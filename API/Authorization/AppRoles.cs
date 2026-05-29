namespace API.Authorization
{
    public static class AppRoles
    {
        public const string Admin = "admin";
        public const string OrganizationAdmin = "organizationAdmin";
        public const string Instructor = "instructor";
        public const string Student = "student";

        public const string All = Admin + "," + OrganizationAdmin + "," + Instructor + "," + Student;
        public const string AdminOrInstructor = Admin + "," + Instructor;
        public const string AdminOrOrganizationAdmin = Admin + "," + OrganizationAdmin;
        public const string AdminOrganizationAdminOrInstructor = Admin + "," + OrganizationAdmin + "," + Instructor;
    }
}
