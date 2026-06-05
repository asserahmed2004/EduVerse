namespace API.Authorization
{
    public static class AppRoles
    {
        public const string Admin = "admin";
        public const string OrganizationAdmin = "organizationAdmin";
        public const string Instructor = "instructor";
        public const string Student = "student";

        public const string AdminAccess = Admin + ",Admin";
        public const string OrganizationAdminAccess = OrganizationAdmin + ",OrganizationAdmin";
        public const string InstructorAccess = Instructor + ",Instructor";
        public const string StudentAccess = Student + ",Student";

        public const string All = AdminAccess + "," + OrganizationAdminAccess + "," + InstructorAccess + "," + StudentAccess;
        public const string AdminOrInstructor = AdminAccess + "," + InstructorAccess;
        public const string AdminOrOrganizationAdmin = AdminAccess + "," + OrganizationAdminAccess;
        public const string AdminOrganizationAdminOrInstructor = AdminAccess + "," + OrganizationAdminAccess + "," + InstructorAccess;
    }
}
