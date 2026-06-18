using Application.DTOs.Learning;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implementitions
{
    public class InstructorService(
        IGeneric<Course> courses,
        IGeneric<Session> sessions,
        IGeneric<Assignment> assignments,
        IGeneric<AssignmentSubmission> submissions,
        IGeneric<Enrollment> enrollments,
        IGeneric<AttendanceRecord> attendanceRecords,
        IGeneric<Organization> organizations,
        IUserManagment userManagement) : IInstructorService
    {
        public async Task<ServiceResponse> GetMyCoursesAsync(string instructorId)
        {
            var assignedCourses = await GetAssignedCourses(instructorId);
            if (assignedCourses.Count == 0)
                return new ServiceResponse(true, "Instructor courses retrieved successfully", new List<InstructorCourseDto>());

            var courseIds = assignedCourses.Select(c => c.Id).ToList();
            var sessionRows = await sessions.Query()
                .Where(s => courseIds.Contains(s.CourseId))
                .ToListAsync();
            var sessionIds = sessionRows.Select(s => s.Id).ToHashSet();
            var assignmentRows = await assignments.Query()
                .Where(a => sessionIds.Contains(a.SessionId))
                .ToListAsync();
            var enrollmentRows = await enrollments.Query()
                .Where(e => courseIds.Contains(e.CourseId))
                .ToListAsync();
            var organizationIds = assignedCourses
                .Where(c => c.OrganizationId.HasValue)
                .Select(c => c.OrganizationId!.Value)
                .ToHashSet();
            var organizationRows = await organizations.Query()
                .Where(o => organizationIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id);

            var rows = assignedCourses.Select(course =>
            {
                var courseSessions = sessionRows.Where(s => s.CourseId == course.Id).ToList();
                var courseSessionIds = courseSessions.Select(s => s.Id).ToHashSet();
                return new InstructorCourseDto
                {
                    CourseId = course.Id,
                    Name = course.Name,
                    Title = course.Title,
                    OrganizationId = course.OrganizationId!.Value,
                    OrganizationName = organizationRows.TryGetValue(course.OrganizationId.Value, out var organization)
                        ? organization.Name
                        : "EduVerseOrganization",
                    StudentsCount = enrollmentRows.Where(e => e.CourseId == course.Id).Select(e => e.StudentId).Distinct().Count(),
                    SessionsCount = courseSessions.Count,
                    AssignmentsCount = assignmentRows.Count(a => courseSessionIds.Contains(a.SessionId))
                };
            }).ToList();

            return new ServiceResponse(true, "Instructor courses retrieved successfully", rows);
        }

        public async Task<ServiceResponse> GetOverviewAsync(string instructorId)
        {
            var assignedCourses = await GetAssignedCourses(instructorId);
            var courseSessions = (await GetManagedSessions(instructorId)).OrderBy(s => s.Date).ToList();
            var managedSessionIds = courseSessions.Select(s => s.Id).ToList();
            var assignmentRows = await assignments.Query().Where(a => managedSessionIds.Contains(a.SessionId)).ToListAsync();
            var assignmentIds = assignmentRows.Select(a => a.Id).ToList();
            var submissionRows = await submissions.Query().Where(s => assignmentIds.Contains(s.AssignmentId)).ToListAsync();
            var courseIds = assignedCourses.Select(c => c.Id).ToList();
            var studentCount = await enrollments.Query()
                .Where(e => courseIds.Contains(e.CourseId))
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

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
            var courseIds = assignedCourses.Select(c => c.Id).ToList();
            var sessionRows = await GetManagedSessions(instructorId);
            var managedSessionIds = sessionRows.Select(s => s.Id).ToList();
            var assignmentRows = await assignments.Query().Where(a => managedSessionIds.Contains(a.SessionId)).ToListAsync();
            var assignmentIds = assignmentRows.Select(a => a.Id).ToList();
            var enrollmentRows = await enrollments.Query().Where(e => courseIds.Contains(e.CourseId)).ToListAsync();
            var studentIds = enrollmentRows.Select(e => e.StudentId).Distinct().ToList();
            var studentsById = await userManagement.QueryUsers()
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
            var submissionRows = await submissions.Query()
                .Where(s => studentIds.Contains(s.StudentId) && assignmentIds.Contains(s.AssignmentId))
                .Select(s => new { s.StudentId, s.AssignmentId })
                .ToListAsync();
            var courseById = assignedCourses.ToDictionary(c => c.Id);
            var sessionCourseById = sessionRows.ToDictionary(s => s.Id, s => s.CourseId);
            var assignmentCourseById = assignmentRows.ToDictionary(a => a.Id, a => sessionCourseById[a.SessionId]);
            var assignmentCountByCourse = assignmentCourseById.Values.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
            var submittedByStudentCourse = submissionRows
                .GroupBy(s => new { s.StudentId, CourseId = assignmentCourseById[s.AssignmentId] })
                .ToDictionary(g => (g.Key.StudentId, g.Key.CourseId), g => g.Select(x => x.AssignmentId).Distinct().Count());

            var rows = enrollmentRows.Select(enrollment =>
                {
                    studentsById.TryGetValue(enrollment.StudentId, out var student);
                    courseById.TryGetValue(enrollment.CourseId, out var course);
                    assignmentCountByCourse.TryGetValue(enrollment.CourseId, out var assignmentCount);
                    submittedByStudentCourse.TryGetValue((enrollment.StudentId, enrollment.CourseId), out var submitted);
                    return new InstructorStudentDto
                {
                    StudentId = enrollment.StudentId,
                    StudentName = student?.FullName ?? string.Empty,
                    StudentEmail = student?.Email ?? string.Empty,
                    CourseId = enrollment.CourseId,
                    CourseName = course?.Name ?? string.Empty,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    ProgressPercentage = enrollment.ProgressPercentage > 0 ? enrollment.ProgressPercentage : enrollment.Progression,
                    SubmissionSummary = $"{submitted}/{assignmentCount} submitted"
                };
            }).ToList();

            return new ServiceResponse(true, "Instructor students retrieved successfully", rows);
        }

        public async Task<ServiceResponse> GetSubmissionsAsync(string instructorId)
        {
            var assignedCourses = await GetAssignedCourses(instructorId);
            var courseSessions = await GetManagedSessions(instructorId);
            var managedSessionIds = courseSessions.Select(s => s.Id).ToList();
            var assignmentRows = await assignments.Query().Where(a => managedSessionIds.Contains(a.SessionId)).ToListAsync();
            var assignmentIds = assignmentRows.Select(a => a.Id).ToList();
            var submissionRows = await submissions.Query().Where(s => assignmentIds.Contains(s.AssignmentId)).ToListAsync();
            return new ServiceResponse(true, "Instructor submissions retrieved successfully", await BuildSubmissionDtos(submissionRows, assignmentRows, courseSessions, assignedCourses));
        }

        public async Task<ServiceResponse> GetSubmissionAsync(Guid assignmentId, string studentId, string instructorId)
        {
            if (!await CanAccessAssignment(assignmentId, instructorId))
                return new ServiceResponse(false, "You cannot access this submission");

            var submission = await submissions.Query().FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
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

            var submission = await submissions.Query(true).FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
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
            var attendance = await attendanceRecords.Query().Where(a => a.SessionId == sessionId).ToListAsync();
            var studentIds = attendance.Select(a => a.StudentId).Distinct().ToList();
            var students = await userManagement.QueryUsers()
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
            foreach (var record in attendance)
            {
                students.TryGetValue(record.StudentId, out var student);
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
            var attendance = await attendanceRecords.Query(true).FirstOrDefaultAsync(a => a.SessionId == sessionId && a.StudentId == userId);
            if (attendance == null)
                return new ServiceResponse(false, "Attendance record not found.");
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
            return await attendanceRecords.Query().Where(a => a.SessionId == sessionId).ToListAsync();
        }
        private async Task<List<Course>> GetAssignedCourses(string instructorId)
        {
            var instructor = await userManagement.GetUserById(instructorId);
            if (instructor?.OrganizationId.HasValue != true)
                return [];

            var fallbackCourseIds = sessions.Query()
                .Where(s => s.TrainerId == instructorId)
                .Select(s => s.CourseId);
            return await courses.Query()
                .Where(c => !c.IsDeleted &&
                    c.OrganizationId == instructor.OrganizationId &&
                    (c.InstructorId == instructorId || fallbackCourseIds.Contains(c.Id)))
                .ToListAsync();
        }

        private async Task<List<Session>> GetManagedSessions(string instructorId)
        {
            var assignedCourses = await GetAssignedCourses(instructorId);
            var ownedCourseIds = assignedCourses.Where(c => c.InstructorId == instructorId).Select(c => c.Id).ToList();
            var assignedCourseIds = assignedCourses.Select(c => c.Id).ToList();
            return await sessions.Query()
                .Where(s => assignedCourseIds.Contains(s.CourseId) && (ownedCourseIds.Contains(s.CourseId) || s.TrainerId == instructorId))
                .ToListAsync();
        }

        private async Task<List<Assignment>> GetAssignmentsForCourses(HashSet<Guid> courseIds)
        {
            var sessionIds = sessions.Query().Where(s => courseIds.Contains(s.CourseId)).Select(s => s.Id);
            return await assignments.Query().Where(a => sessionIds.Contains(a.SessionId)).ToListAsync();
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

            return await sessions.Query().AnyAsync(s => s.CourseId == courseId && s.TrainerId == instructorId);
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
            var submissionsList = submissionRows.ToList();
            var studentIds = submissionsList.Select(s => s.StudentId).Distinct().ToList();
            var students = await userManagement.QueryUsers()
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
            var rows = new List<InstructorSubmissionDto>();
            foreach (var submission in submissionsList)
            {
                var assignment = assignmentRows.FirstOrDefault(a => a.Id == submission.AssignmentId);
                var session = assignment == null ? null : courseSessions.FirstOrDefault(s => s.Id == assignment.SessionId);
                var course = session == null ? null : assignedCourses.FirstOrDefault(c => c.Id == session.CourseId);
                students.TryGetValue(submission.StudentId, out var student);
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
