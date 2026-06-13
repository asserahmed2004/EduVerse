using Application.DTOs.Learning;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services.Implementitions
{
    public class InstructorService(
        IGeneric<Course> courses,
        IGeneric<Session> sessions,
        IGeneric<Assignment> assignments,
        IGeneric<AssignmentSubmission> submissions,
        IGeneric<Enrollment> enrollments,
        IGeneric<AttendanceRecord> attendanceRecords,
        IUserManagment userManagement) : IInstructorService
    {
        public async Task<ServiceResponse> GetOverviewAsync(string instructorId)
        {
            var assignedCourses = await GetAssignedCourses(instructorId);
            var courseSessions = (await GetManagedSessions(instructorId)).OrderBy(s => s.Date).ToList();
            var managedSessionIds = courseSessions.Select(s => s.Id).ToHashSet();
            var assignmentRows = (await assignments.GetAllAsync()).Where(a => managedSessionIds.Contains(a.SessionId)).ToList();
            var submissionRows = (await submissions.GetAllAsync()).Where(s => assignmentRows.Any(a => a.Id == s.AssignmentId)).ToList();
            var courseIds = assignedCourses.Select(c => c.Id).ToHashSet();
            var studentCount = (await enrollments.GetAllAsync()).Where(e => courseIds.Contains(e.CourseId)).Select(e => e.StudentId).Distinct().Count();

            var overview = new InstructorOverviewDto
            {
                AssignedCourses = assignedCourses.Count,
                MyStudents = studentCount,
                TotalAssignments = assignmentRows.Count,
                PendingSubmissions = submissionRows.Count(s => !s.Grade.HasValue),
                UpcomingSessions = courseSessions.Where(s => s.Date >= DateTime.Today).Take(5).Select(s => ToSessionDto(s, assignedCourses)).ToList(),
                RecentSubmissions = await BuildSubmissionDtos(submissionRows.OrderByDescending(s => s.SubmittedAt ?? DateTime.MinValue).Take(5), assignmentRows, courseSessions, assignedCourses)
            };

            return new ServiceResponse(true, "Instructor overview retrieved successfully", overview);
        }

        public async Task<ServiceResponse> GetSessionsAsync(string instructorId)
        {
            var assignedCourses = await GetAssignedCourses(instructorId);
            var rows = (await GetManagedSessions(instructorId)).OrderBy(s => s.Date).Select(s => ToSessionDto(s, assignedCourses)).ToList();
            return new ServiceResponse(true, "Instructor sessions retrieved successfully", rows);
        }

        public async Task<ServiceResponse> GetStudentsAsync(string instructorId)
        {
            var assignedCourses = await GetAssignedCourses(instructorId);
            var courseIds = assignedCourses.Select(c => c.Id).ToHashSet();
            var submissionRows = (await submissions.GetAllAsync()).ToList();
            var sessionRows = await GetManagedSessions(instructorId);
            var managedSessionIds = sessionRows.Select(s => s.Id).ToHashSet();
            var assignmentRows = (await assignments.GetAllAsync()).Where(a => managedSessionIds.Contains(a.SessionId)).ToList();
            var rows = new List<InstructorStudentDto>();

            foreach (var enrollment in (await enrollments.GetAllAsync()).Where(e => courseIds.Contains(e.CourseId)))
            {
                var student = await userManagement.GetUserById(enrollment.StudentId);
                var course = assignedCourses.FirstOrDefault(c => c.Id == enrollment.CourseId);
                var courseAssignments = assignmentRows.Where(a => sessionRows.Any(s => s.Id == a.SessionId && s.CourseId == enrollment.CourseId)).ToList();
                var submitted = submissionRows.Count(s => s.StudentId == enrollment.StudentId && courseAssignments.Any(a => a.Id == s.AssignmentId));
                rows.Add(new InstructorStudentDto
                {
                    StudentId = enrollment.StudentId,
                    StudentName = student?.FullName ?? string.Empty,
                    StudentEmail = student?.Email ?? string.Empty,
                    CourseId = enrollment.CourseId,
                    CourseName = course?.Name ?? string.Empty,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    ProgressPercentage = enrollment.ProgressPercentage > 0 ? enrollment.ProgressPercentage : enrollment.Progression,
                    SubmissionSummary = $"{submitted}/{courseAssignments.Count} submitted"
                });
            }

            return new ServiceResponse(true, "Instructor students retrieved successfully", rows);
        }

        public async Task<ServiceResponse> GetSubmissionsAsync(string instructorId)
        {
            var assignedCourses = await GetAssignedCourses(instructorId);
            var courseSessions = await GetManagedSessions(instructorId);
            var managedSessionIds = courseSessions.Select(s => s.Id).ToHashSet();
            var assignmentRows = (await assignments.GetAllAsync()).Where(a => managedSessionIds.Contains(a.SessionId)).ToList();
            var submissionRows = (await submissions.GetAllAsync()).Where(s => assignmentRows.Any(a => a.Id == s.AssignmentId)).ToList();
            return new ServiceResponse(true, "Instructor submissions retrieved successfully", await BuildSubmissionDtos(submissionRows, assignmentRows, courseSessions, assignedCourses));
        }

        public async Task<ServiceResponse> GetSubmissionAsync(Guid assignmentId, string studentId, string instructorId)
        {
            if (!await CanAccessAssignment(assignmentId, instructorId))
                return new ServiceResponse(false, "You cannot access this submission");

            var submission = (await submissions.GetAllAsync()).FirstOrDefault(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
            if (submission == null)
                return new ServiceResponse(false, "Submission not found");

            var assignment = await assignments.GetByIdAsync(assignmentId);
            var session = assignment == null ? null : await sessions.GetByIdAsync(assignment.SessionId);
            var course = session == null ? null : await courses.GetByIdAsync(session.CourseId);
            var student = await userManagement.GetUserById(studentId);

            return new ServiceResponse(true, "Submission retrieved successfully", new InstructorSubmissionDto
            {
                SubmissionId = $"{assignmentId}:{studentId}",
                StudentId = studentId,
                StudentName = student?.FullName ?? string.Empty,
                AssignmentId = assignmentId,
                AssignmentTitle = assignment?.Subject ?? string.Empty,
                SessionId = session?.Id ?? Guid.Empty,
                SessionTitle = session?.Title ?? string.Empty,
                CourseId = course?.Id ?? Guid.Empty,
                CourseName = course?.Name ?? string.Empty,
                TextAnswer = submission.TextAnswer,
                FilePath = submission.FileUrl,
                SubmittedAt = submission.SubmittedAt,
                IsLate = submission.IsLate,
                Grade = submission.Grade,
                Feedback = submission.Feedback,
                FileUrl = submission.FileUrl
            });
        }

        public async Task<ServiceResponse> GradeSubmissionAsync(Guid assignmentId, string studentId, GradeSubmissionRequest request, string instructorId)
        {
            if (!await CanAccessAssignment(assignmentId, instructorId))
                return new ServiceResponse(false, "You can grade only submissions for assigned courses");
            if (request == null || double.IsNaN(request.Grade) || double.IsInfinity(request.Grade) || request.Grade < 0 || request.Grade > 100)
                return new ServiceResponse(false, "Grade must be between 0 and 100");

            var submission = (await submissions.GetAllAsync()).FirstOrDefault(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
            if (submission == null)
                return new ServiceResponse(false, "Submission not found");

            submission.Grade = request.Grade;
            submission.Feedback = request.Feedback;
            var result = await submissions.UpdateAsync(submission);
            return result == null
                ? new ServiceResponse(false, "Failed to save submission grade")
                : new ServiceResponse(true, "Submission graded successfully", new { grade = submission.Grade, feedback = submission.Feedback });
        }

        public async Task<ServiceResponse> CreateSessionQrAsync(Guid sessionId, string userId, bool isAdminOrOrganizationAdmin)
        {
            var session = await sessions.GetByIdAsync(sessionId);
            if (session == null)
                return new ServiceResponse(false, "Session not found");

            if (!isAdminOrOrganizationAdmin && !await CanAccessCourse(session.CourseId, userId))
                return new ServiceResponse(false, "You cannot generate attendance for this session");

            session.AttendanceCode = Random.Shared.Next(100000, 999999).ToString();
            session.AttendanceCodeCreatedAt = DateTime.UtcNow;
            await sessions.UpdateAsync(session);
            return new ServiceResponse(true, "Attendance QR created successfully", new AttendanceQrDto
            {
                SessionId = session.Id,
                AttendanceCode = session.AttendanceCode,
                CreatedAt = session.AttendanceCodeCreatedAt.Value
            });
        }

        public async Task<ServiceResponse> GetSessionAttendanceAsync(Guid sessionId, string userId, bool isAdminOrOrganizationAdmin)
        {
            var session = await sessions.GetByIdAsync(sessionId);
            if (session == null)
                return new ServiceResponse(false, "Session not found");

            if (!isAdminOrOrganizationAdmin && !await CanAccessCourse(session.CourseId, userId))
                return new ServiceResponse(false, "You cannot view attendance for this session");

            var rows = new List<AttendanceRecordDto>();
            foreach (var record in (await attendanceRecords.GetAllAsync()).Where(a => a.SessionId == sessionId))
            {
                var student = await userManagement.GetUserById(record.StudentId);
                rows.Add(new AttendanceRecordDto
                {
                    SessionId = record.SessionId,
                    StudentId = record.StudentId,
                    StudentName = student?.FullName ?? student?.Email ?? string.Empty,
                    Attended = record.Attended
                    
                });
            }

            return new ServiceResponse(true, "Session attendance retrieved successfully", rows);
        }
        public async Task<ServiceResponse> MarkAttendance(Guid sessionId, string userId)
        {
            var attendance = (await attendanceRecords.GetAllAsync()).FirstOrDefault(a => a.SessionId == sessionId && a.StudentId == userId);
            if (attendance.Attended)
            {
                attendance.Attended = false;
                var result = await attendanceRecords.UpdateAsync(attendance);

            }
            else
            {
                attendance.Attended = true;
                var result = await attendanceRecords.UpdateAsync(attendance);
            }
            return new ServiceResponse(true, "Attendance status updated.");


        }
        public async Task<IEnumerable<AttendanceRecord>> GetAttendanceRecords(Guid sessionId)
        {
            var result = await attendanceRecords.GetAllAsync();
            return result;
        }
        private async Task<List<Course>> GetAssignedCourses(string instructorId)
        {
            var activeCourses = (await courses.GetAllAsync()).Where(c => !c.IsDeleted).ToList();
            var fallbackCourseIds = (await sessions.GetAllAsync()).Where(s => s.TrainerId == instructorId).Select(s => s.CourseId).ToHashSet();
            return activeCourses.Where(c => c.InstructorId == instructorId || fallbackCourseIds.Contains(c.Id)).ToList();
        }

        private async Task<List<Session>> GetManagedSessions(string instructorId)
        {
            var activeCourses = (await courses.GetAllAsync()).Where(c => !c.IsDeleted).ToList();
            var ownedCourseIds = activeCourses.Where(c => c.InstructorId == instructorId).Select(c => c.Id).ToHashSet();
            var activeCourseIds = activeCourses.Select(c => c.Id).ToHashSet();
            return (await sessions.GetAllAsync())
                .Where(s => activeCourseIds.Contains(s.CourseId) && (ownedCourseIds.Contains(s.CourseId) || s.TrainerId == instructorId))
                .ToList();
        }

        private async Task<List<Assignment>> GetAssignmentsForCourses(HashSet<Guid> courseIds)
        {
            var sessionIds = (await sessions.GetAllAsync()).Where(s => courseIds.Contains(s.CourseId)).Select(s => s.Id).ToHashSet();
            return (await assignments.GetAllAsync()).Where(a => sessionIds.Contains(a.SessionId)).ToList();
        }

        private async Task<bool> CanAccessAssignment(Guid assignmentId, string instructorId)
        {
            var assignment = await assignments.GetByIdAsync(assignmentId);
            var session = assignment == null ? null : await sessions.GetByIdAsync(assignment.SessionId);
            var course = session == null ? null : await courses.GetByIdAsync(session.CourseId);
            return session != null && course != null && !course.IsDeleted &&
                (course.InstructorId == instructorId || session.TrainerId == instructorId);
        }

        private async Task<bool> CanAccessCourse(Guid courseId, string instructorId)
        {
            var course = await courses.GetByIdAsync(courseId);
            if (course?.InstructorId == instructorId)
                return true;

            return (await sessions.GetAllAsync()).Any(s => s.CourseId == courseId && s.TrainerId == instructorId);
        }

        private static InstructorSessionDto ToSessionDto(Session session, IEnumerable<Course> assignedCourses)
        {
            return new InstructorSessionDto
            {
                SessionId = session.Id,
                CourseId = session.CourseId,
                CourseName = assignedCourses.FirstOrDefault(c => c.Id == session.CourseId)?.Name ?? string.Empty,
                Title = session.Title,
                SessionNumber = session.SessionNumber,
                Date = session.Date
            };
        }

        private async Task<List<InstructorSubmissionDto>> BuildSubmissionDtos(IEnumerable<AssignmentSubmission> submissionRows, List<Assignment> assignmentRows, List<Session> courseSessions, List<Course> assignedCourses)
        {
            var rows = new List<InstructorSubmissionDto>();
            foreach (var submission in submissionRows)
            {
                var assignment = assignmentRows.FirstOrDefault(a => a.Id == submission.AssignmentId);
                var session = assignment == null ? null : courseSessions.FirstOrDefault(s => s.Id == assignment.SessionId);
                var course = session == null ? null : assignedCourses.FirstOrDefault(c => c.Id == session.CourseId);
                var student = await userManagement.GetUserById(submission.StudentId);
                rows.Add(new InstructorSubmissionDto
                {
                    SubmissionId = $"{submission.AssignmentId}:{submission.StudentId}",
                    StudentId = submission.StudentId,
                    StudentName = student?.FullName ?? student?.Email ?? string.Empty,
                    AssignmentId = submission.AssignmentId,
                    AssignmentTitle = assignment?.Subject ?? string.Empty,
                    SessionId = session?.Id ?? Guid.Empty,
                    SessionTitle = session?.Title ?? string.Empty,
                    CourseId = course?.Id ?? Guid.Empty,
                    CourseName = course?.Name ?? string.Empty,
                    TextAnswer = submission.TextAnswer,
                    FilePath = submission.FileUrl,
                    SubmittedAt = submission.SubmittedAt,
                    IsLate = submission.IsLate,
                    Grade = submission.Grade,
                    Feedback = submission.Feedback,
                    FileUrl = submission.FileUrl
                });
            }

            return rows;
        }
    }
}
