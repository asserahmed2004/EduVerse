namespace Application.DTOs.Dashboard
{
    public class OrganizationStatsDto
    {
        public int TotalCourses { get; set; }
        public int DeletedCourses { get; set; }
        public int TotalInstructors { get; set; }
        public int TotalStudents { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalSessions { get; set; }
        public int TotalAssignments { get; set; }
        public int TotalPayments { get; set; }
        public double TotalRevenue { get; set; }
        public double AverageRating { get; set; }
    }
}
