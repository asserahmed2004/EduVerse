namespace InfraStructure.Data.Seed
{
    internal sealed class SeedDemoStatus
    {
        public int TotalUsers { get; init; }
        public int TotalCourses { get; init; }
        public int TotalCategories { get; init; }
        public int DemoUserCount { get; init; }
        public int DemoCourseCount { get; init; }
        public bool MarkerExists { get; init; }
        public bool PrimaryDemoStudentExists { get; init; }

        public bool IsFullySeeded =>
            MarkerExists
            && PrimaryDemoStudentExists
            && DemoCourseCount >= SeedCatalog.CourseCount
            && DemoUserCount >= SeedCatalog.StudentCount + SeedCatalog.InstructorCount + SeedCatalog.AdminCount;

        public bool HasPartialDemoData =>
            MarkerExists
            || PrimaryDemoStudentExists
            || DemoCourseCount > 0
            || DemoUserCount > 0;
    }

    internal sealed class SeedCreationCounters
    {
        public int UsersCreated { get; set; }
        public int OrganizationsCreated { get; set; }
        public int CategoriesCreated { get; set; }
        public int CoursesCreated { get; set; }
        public int CourseCategoriesCreated { get; set; }
        public int EnrollmentsCreated { get; set; }
        public int RatingsCreated { get; set; }
        public int ImagesFixed { get; set; }
    }
}
