using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Learning
{
    public class StudentAssignmentDto
    {
        public Guid AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public Guid SessionId { get; set; }
        public string SessionTitle { get; set; } = string.Empty;
        public int SessionNumber { get; set; }
        public DateTime? DueDate { get; set; }
        public string SubmissionStatus { get; set; } = "Not Submitted";
        public DateTime? SubmittedAt { get; set; }
        public double? Grade { get; set; }
        public string? Feedback { get; set; }
        public string? AssignmentFileUrl { get; set; }
        public string? FileUrl { get; set; }
    }

    public class SubmitAssignmentRequest
    {
        public Guid AssignmentId { get; set; }
        public string? TextAnswer { get; set; }
        public IFormFile? File { get; set; }
    }

    public class SessionProgressDto
    {
        public Guid SessionId { get; set; }
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int SessionNumber { get; set; }
        public string? FileUrl { get; set; }
        public string? Description { get; set; }
        public string? VideoUrl { get; set; }
        public string? ExternalLink { get; set; }
        public bool IsDone { get; set; }
        public DateTime? DoneAt { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public IEnumerable<SessionMaterialDto> Materials { get; set; } = [];
        public IEnumerable<StudentAssignmentDto> Assignments { get; set; } = [];
    }

    public class CourseProgressDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public int DoneSessions { get; set; }
        public double ProgressPercentage { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public IEnumerable<SessionProgressDto> Sessions { get; set; } = [];
    }

    public class ToggleSessionDoneResultDto
    {
        public Guid SessionId { get; set; }
        public Guid CourseId { get; set; }
        public bool IsDone { get; set; }
        public DateTime? DoneAt { get; set; }
        public int DoneSessions { get; set; }
        public int TotalSessions { get; set; }
        public double ProgressPercentage { get; set; }
    }

    public class AssignmentProgressDto
    {
        public Guid CourseId { get; set; }
        public int TotalAssignments { get; set; }
        public int SubmittedAssignments { get; set; }
        public double AssignmentProgressPercentage { get; set; }
        public int RequiredPercentage { get; set; } = 80;
        public bool HasRequiredAssignmentProgress { get; set; }
    }

    public class CertificateEligibilityDto
    {
        public Guid CourseId { get; set; }
        public double AssignmentProgressPercentage { get; set; }
        public int RequiredPercentage { get; set; } = 80;
        public bool HasRequiredAssignmentProgress { get; set; }
        public bool IsCourseCompleted { get; set; }
        public bool IsCourseDurationFinished { get; set; }
        public bool CanReceiveCertificate { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CertificateDto
    {
        public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string CertificateCode { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string? FileUrl { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "Valid";
        public string VerificationUrl { get; set; } = string.Empty;
    }

    public class CertificateDownloadDto
    {
        public byte[] Content { get; set; } = [];
        public string FileName { get; set; } = "EduVerse-Certificate.pdf";
    }

    public class CertificateVerificationDto
    {
        public string CertificateCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public string Status { get; set; } = "Valid";
    }

    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class InstructorOverviewDto
    {
        public int AssignedCourses { get; set; }
        public int MyStudents { get; set; }
        public int PendingSubmissions { get; set; }
        public int TotalAssignments { get; set; }
        public IEnumerable<InstructorSessionDto> UpcomingSessions { get; set; } = [];
        public IEnumerable<InstructorSubmissionDto> RecentSubmissions { get; set; } = [];
    }

    public class InstructorCourseDto
    {
        public Guid CourseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public int StudentsCount { get; set; }
        public int SessionsCount { get; set; }
        public int AssignmentsCount { get; set; }
    }

    public class InstructorSessionDto
    {
        public Guid SessionId { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int SessionNumber { get; set; }
        public DateTime Date { get; set; }
    }

    public class InstructorStudentDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public double ProgressPercentage { get; set; }
        public string SubmissionSummary { get; set; } = string.Empty;
    }

    public class InstructorSubmissionDto
    {
        public string SubmissionId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public Guid AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public Guid SessionId { get; set; }
        public string SessionTitle { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string? TextAnswer { get; set; }
        public string? FilePath { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public bool IsLate { get; set; }
        public double? Grade { get; set; }
        public string? Feedback { get; set; }
        public string? FileUrl { get; set; }
    }

    public class GradeSubmissionRequest
    {
        public double Grade { get; set; }
        public string? Feedback { get; set; }
    }

    public class AssignInstructorRequest
    {
        public Guid CourseId { get; set; }
        public string InstructorId { get; set; } = string.Empty;
    }

    public class SessionMaterialDto
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "Link";
        public string? Url { get; set; }
        public string? FilePath { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AttendanceQrDto
    {
        public Guid SessionId { get; set; }
        public string AttendanceCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class MarkAttendanceRequest
    {
        public string AttendanceCode { get; set; } = string.Empty;
    }

    public class AttendanceRecordDto
    {
        public Guid SessionId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public bool Attended { get; set; }
    }
}
