namespace API.Authorization
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Instructor = "Instructor";
        public const string Student = "Student";

        public const string All = Admin + "," + Instructor + "," + Student;
        public const string AdminOrInstructor = Admin + "," + Instructor;
    }
}
