namespace Application.DTOs.Dashboard
{
    public class OrganizationStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalOrganizations { get; set; }
        public int TotalCourses { get; set; }
        public int DeletedCourses { get; set; }
        public int TotalInstructors { get; set; }
        public int TotalStudents { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalSessions { get; set; }
        public int TotalAssignments { get; set; }
        public int TotalPayments { get; set; }
        public int PendingPayments { get; set; }
        public double TotalRevenue { get; set; }
        public double AverageRating { get; set; }
    }
}
