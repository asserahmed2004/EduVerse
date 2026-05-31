namespace Application.DTOs.Dashboard
{
    public class TrendPointDto
    {
        public string Label { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public double Value { get; set; }
    }

    public class RoleCountDto
    {
        public string Role { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TopCourseChartDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int Enrollments { get; set; }
        public double Revenue { get; set; }
        public double AverageRating { get; set; }
    }
}
