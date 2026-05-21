namespace API.Authorization
{
    public static class AppRoles
    {
        public const string Admin = "admin";
        public const string Instructor = "instructor";
        public const string Student = "student";

        public const string All = Admin + "," + Instructor + "," + Student;
        public const string AdminOrInstructor = Admin + "," + Instructor;
    }
}
