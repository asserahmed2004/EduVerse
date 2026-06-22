namespace InfraStructure.Data.Seed
{
    internal static class SeedImageCatalog
    {
        public const string ProfileAvatar = "seed:profile-avatar";
        public const string Organization = "seed:organization";

        public static string GetCourseImage(string? title, string? tags)
        {
            var context = $"{title} {tags}".ToLowerInvariant();

            if (ContainsAny(context, "angular"))
                return "seed:course-angular";
            if (ContainsAny(context, "react native", "react-native", "flutter", "mobile"))
                return "seed:course-mobile";
            if (ContainsAny(context, "react", "javascript", "frontend", "typescript"))
                return "seed:course-react";
            if (ContainsAny(context, "sql server", "database", "sql"))
                return "seed:course-sql";
            if (ContainsAny(context, "java", "spring"))
                return "seed:course-java";
            if (ContainsAny(context, "asp.net", "aspnet", ".net", "dotnet", "c#", "csharp", "entity framework"))
                return "seed:course-dotnet";
            if (ContainsAny(context, "python", "machine learning", "data science", "analytics", "pandas"))
                return "seed:course-data-ai";
            if (ContainsAny(context, "cyber", "security", "ethical hacking", "networking"))
                return "seed:course-cybersecurity";
            if (ContainsAny(context, "devops", "docker", "kubernetes", "cloud", "aws", "ci/cd", "cicd"))
                return "seed:course-cloud-devops";
            if (ContainsAny(context, "testing", "selenium", "quality assurance", "qa"))
                return "seed:course-testing";
            if (ContainsAny(context, "photography", "photoshop", "photo editing"))
                return "seed:course-photography";
            if (ContainsAny(context, "design", "figma", "ui", "ux", "graphic"))
                return "seed:course-design";
            if (ContainsAny(context, "health", "fitness", "nutrition", "wellness", "personal development"))
                return "seed:course-wellness";
            if (ContainsAny(context, "language", "english", "german", "french", "spanish", "leadership", "public speaking", "communication"))
                return "seed:course-communication";
            if (ContainsAny(context, "marketing", "sales", "business", "entrepreneurship", "finance", "accounting", "project management"))
                return "seed:course-business-marketing";

            return "seed:course-dotnet";
        }

        public static bool NeedsFallback(string? value, Guid? seededCourseId = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            var normalized = value.Trim();
            if (normalized.StartsWith("seed:", StringComparison.OrdinalIgnoreCase))
                return false;

            if (normalized.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("placehold.co", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("via.placeholder.com", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return seededCourseId.HasValue
                && string.Equals(normalized, $"{seededCourseId.Value}-Thumbnail", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }
}
