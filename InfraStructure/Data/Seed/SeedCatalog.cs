using System;
using System.Collections.Generic;
using System.Linq;

namespace InfraStructure.Data.Seed
{
    internal static class SeedCatalog
    {
        public const string MarkerEmail = "seed.marker@demo.eduverse.app";
        public const string EmailDomain = "demo.eduverse.app";
        public const string OrganizationAdminEmail = "OrgAdminMigo@eduverse.com";
        public const int StudentCount = 100;
        public const int InstructorCount = 20;
        public const int AdminCount = 5;
        public const int OrganizationCount = 10;
        public const int CourseCount = 100;
        public const int TargetRatingCount = 1000;
        public const int MinEnrollmentCount = 500;

        public static readonly string[] Roles =
        [
            "admin",
            "student",
            "instructor",
            "organizationAdmin"
        ];

        public static readonly (string Name, string Description)[] Categories =
        [
            ("Programming", "Software development and programming languages"),
            ("Web Development", "Frontend and backend web technologies"),
            ("Mobile Development", "iOS, Android, and cross-platform apps"),
            ("Data Science", "Analytics, machine learning, and AI foundations"),
            ("Cyber Security", "Security, networking, and ethical hacking"),
            ("DevOps & Cloud", "CI/CD, Docker, Kubernetes, and cloud platforms"),
            ("Database", "SQL, database design, and data management"),
            ("Business", "Business strategy, operations, and entrepreneurship"),
            ("Marketing", "Digital marketing, branding, and sales"),
            ("Design", "Graphic design, UI, and UX"),
            ("Languages", "Language learning and communication"),
            ("Leadership", "Leadership, management, and soft skills"),
            ("Finance", "Personal finance, investing, and accounting"),
            ("Health & Fitness", "Nutrition, fitness, and wellness"),
            ("Photography", "Photography techniques and editing"),
            ("Project Management", "Agile, Scrum, and project delivery"),
            ("Personal Development", "Productivity, habits, and growth"),
            ("Software Testing", "QA, automation, and test strategy")
        ];

        public static readonly string[] Levels =
        [
            "Beginner Level",
            "Intermediate Level",
            "Advanced Level",
            "All Levels"
        ];

        public static readonly (string Name, string Description)[] Organizations =
        [
            ("TechNova Academy", "Technology and software training provider"),
            ("CloudBridge Institute", "Cloud, DevOps, and modern infrastructure courses"),
            ("CodeCraft Labs", "Hands-on programming bootcamps"),
            ("DataPulse School", "Data science and analytics education"),
            ("SecureNet Training", "Cybersecurity and networking programs"),
            ("BizGrowth Hub", "Business, marketing, and entrepreneurship"),
            ("CreativeStudio Academy", "Design, UX, and visual storytelling"),
            ("GlobalLang Center", "Language and communication courses"),
            ("WellnessPath Institute", "Health, fitness, and lifestyle coaching"),
            ("LensMaster Academy", "Photography and creative media")
        ];

        public static readonly StudentInterestProfile[] StudentProfiles = BuildStudentProfiles();

        public static readonly CourseTemplate[] Courses = BuildCourses();

        private static StudentInterestProfile[] BuildStudentProfiles()
        {
            var profiles = new List<StudentInterestProfile>();

            AddProfiles(profiles, 1, 12, StudentInterestGroup.BackendDeveloper, "Backend Developer");
            AddProfiles(profiles, 13, 24, StudentInterestGroup.MobileDeveloper, "Mobile Developer");
            AddProfiles(profiles, 25, 36, StudentInterestGroup.FrontendDeveloper, "Frontend Developer");
            AddProfiles(profiles, 37, 48, StudentInterestGroup.CyberSecurityLearner, "Cyber Security Learner");
            AddProfiles(profiles, 49, 60, StudentInterestGroup.DataScientist, "Data Scientist");
            AddProfiles(profiles, 61, 67, StudentInterestGroup.MarketingSpecialist, "Marketing Specialist");
            AddProfiles(profiles, 68, 74, StudentInterestGroup.BusinessOwner, "Business Owner");
            AddProfiles(profiles, 75, 81, StudentInterestGroup.GraphicDesigner, "Graphic Designer");
            AddProfiles(profiles, 82, 88, StudentInterestGroup.LanguageLearner, "Language Learner");
            AddProfiles(profiles, 89, 94, StudentInterestGroup.FitnessEnthusiast, "Fitness Enthusiast");
            AddProfiles(profiles, 95, 100, StudentInterestGroup.Photographer, "Photographer");

            return profiles.ToArray();
        }

        private static void AddProfiles(
            List<StudentInterestProfile> profiles,
            int from,
            int to,
            StudentInterestGroup group,
            string label)
        {
            for (var index = from; index <= to; index++)
            {
                profiles.Add(new StudentInterestProfile(index, group, label));
            }
        }

        private static CourseTemplate[] BuildCourses()
        {
            var courses = new List<CourseTemplate>();
            var techTemplates = GetTechnologyTemplates();
            var nonTechTemplates = GetNonTechnologyTemplates();

            for (var i = 0; i < 60; i++)
            {
                var template = techTemplates[i % techTemplates.Length];
                var variant = i / techTemplates.Length + 1;
                courses.Add(CreateCourseTemplate(
                    $"tech-{i + 1:D3}",
                    $"{template.Title}{(variant > 1 ? $" {variant}" : string.Empty)}",
                    template.Name,
                    template.Description,
                    template.Category,
                    template.Tags,
                    template.Level,
                    template.PriceBase + (variant * 3),
                    isTechnology: true,
                    template.Topics));
            }

            for (var i = 0; i < 40; i++)
            {
                var template = nonTechTemplates[i % nonTechTemplates.Length];
                var variant = i / nonTechTemplates.Length + 1;
                courses.Add(CreateCourseTemplate(
                    $"nontech-{i + 1:D3}",
                    $"{template.Title}{(variant > 1 ? $" {variant}" : string.Empty)}",
                    template.Name,
                    template.Description,
                    template.Category,
                    template.Tags,
                    template.Level,
                    template.PriceBase + (variant * 2),
                    isTechnology: false,
                    template.Topics));
            }

            return courses.ToArray();
        }

        private static CourseTemplate CreateCourseTemplate(
            string key,
            string title,
            string name,
            string description,
            string category,
            string tags,
            string level,
            double priceBase,
            bool isTechnology,
            string[] topics)
        {
            return new CourseTemplate(
                key,
                $"[SEED] {name}",
                title,
                description,
                category,
                tags,
                level,
                priceBase,
                isTechnology,
                topics);
        }

        private static RawCourseTemplate[] GetTechnologyTemplates()
        {
            return
            [
                new("C# Programming Masterclass", "csharp-fundamentals", "Learn C# from scratch with practical exercises and real-world examples.", "Programming", "csharp,dotnet,programming,backend", "Beginner Level", 39.99, ["csharp", "dotnet", "backend"]),
                new("ASP.NET Core Web API Development", "aspnet-webapi", "Build secure REST APIs with ASP.NET Core, JWT, and Entity Framework.", "Web Development", "aspnet,dotnet,csharp,backend,webapi", "Intermediate Level", 54.99, ["aspnet", "dotnet", "webapi"]),
                new(".NET Clean Architecture in Practice", "dotnet-clean-architecture", "Apply Clean Architecture, CQRS, and SOLID principles in .NET projects.", "Programming", "dotnet,csharp,clean-architecture,backend", "Advanced Level", 64.99, ["dotnet", "architecture", "csharp"]),
                new("Entity Framework Core Deep Dive", "entity-framework-core", "Master EF Core migrations, relationships, performance tuning, and best practices.", "Database", "entity-framework,dotnet,sql,database,backend", "Intermediate Level", 49.99, ["entity-framework", "dotnet", "sql"]),
                new("SQL Server for Backend Developers", "sql-server-backend", "Design schemas, write optimized queries, and manage SQL Server databases.", "Database", "sql,database,sql-server,backend", "Intermediate Level", 44.99, ["sql", "database", "backend"]),
                new("Flutter Mobile App Development", "flutter-mobile", "Create beautiful cross-platform mobile apps with Flutter and Dart.", "Mobile Development", "flutter,mobile,dart,cross-platform", "Beginner Level", 59.99, ["flutter", "mobile", "dart"]),
                new("React Frontend Development Bootcamp", "react-frontend", "Build modern SPAs with React, hooks, routing, and state management.", "Web Development", "react,javascript,frontend,spa", "Beginner Level", 49.99, ["react", "javascript", "frontend"]),
                new("Angular Enterprise Applications", "angular-enterprise", "Develop scalable enterprise frontends with Angular and TypeScript.", "Web Development", "angular,typescript,frontend,enterprise", "Intermediate Level", 54.99, ["angular", "frontend", "typescript"]),
                new("Java Programming for Developers", "java-programming", "Core Java, OOP, collections, streams, and application structure.", "Programming", "java,programming,backend,oop", "Beginner Level", 42.99, ["java", "backend", "programming"]),
                new("Spring Boot Microservices", "spring-boot-microservices", "Design and deploy microservices with Spring Boot and REST APIs.", "Web Development", "java,spring-boot,microservices,backend", "Advanced Level", 69.99, ["spring-boot", "java", "microservices"]),
                new("Python for Data Analysis", "python-data-analysis", "Analyze datasets with Python, Pandas, NumPy, and visualization tools.", "Data Science", "python,data-science,analytics,pandas", "Beginner Level", 47.99, ["python", "data-science", "analytics"]),
                new("Machine Learning Foundations with Python", "machine-learning-python", "Understand supervised learning, model evaluation, and ML workflows.", "Data Science", "python,machine-learning,data-science,ml", "Intermediate Level", 74.99, ["machine-learning", "python", "data-science"]),
                new("Cyber Security Fundamentals", "cyber-security-fundamentals", "Learn security principles, threats, vulnerabilities, and mitigation strategies.", "Cyber Security", "cybersecurity,security,networking,infosec", "Beginner Level", 59.99, ["cybersecurity", "security", "networking"]),
                new("Ethical Hacking and Penetration Testing", "ethical-hacking", "Hands-on penetration testing methodology and defensive security practices.", "Cyber Security", "cybersecurity,ethical-hacking,security,networking", "Advanced Level", 79.99, ["cybersecurity", "ethical-hacking", "security"]),
                new("Computer Networking Essentials", "networking-essentials", "Understand TCP/IP, routing, switching, DNS, and network troubleshooting.", "Cyber Security", "networking,cybersecurity,infrastructure,security", "Intermediate Level", 52.99, ["networking", "cybersecurity", "security"]),
                new("DevOps CI/CD Pipeline Mastery", "devops-cicd", "Automate build, test, and deployment pipelines with modern DevOps tools.", "DevOps & Cloud", "devops,cicd,automation,deployment", "Intermediate Level", 64.99, ["devops", "cicd", "automation"]),
                new("Docker and Kubernetes for Developers", "docker-kubernetes", "Containerize applications and orchestrate workloads with Docker and Kubernetes.", "DevOps & Cloud", "docker,kubernetes,devops,cloud,containers", "Advanced Level", 72.99, ["docker", "kubernetes", "devops"]),
                new("AWS Cloud Practitioner to Architect", "aws-cloud", "Cloud fundamentals, core AWS services, and solution architecture patterns.", "DevOps & Cloud", "cloud,aws,devops,architecture", "All Levels", 68.99, ["cloud", "aws", "devops"]),
                new("Automated Software Testing with Selenium", "software-testing-selenium", "Build reliable automated test suites for web applications.", "Software Testing", "testing,selenium,qa,automation", "Intermediate Level", 46.99, ["testing", "qa", "automation"]),
                new("Mobile Development with React Native", "react-native-mobile", "Build iOS and Android apps using React Native and JavaScript.", "Mobile Development", "react-native,mobile,javascript,cross-platform", "Intermediate Level", 57.99, ["react-native", "mobile", "javascript"])
            ];
        }

        private static RawCourseTemplate[] GetNonTechnologyTemplates()
        {
            return
            [
                new("Project Management Professional Prep", "project-management", "Plan, execute, and deliver projects using proven PM frameworks.", "Project Management", "project-management,business,planning,agile", "All Levels", 44.99, ["project-management", "business", "planning"]),
                new("Digital Marketing Strategy", "digital-marketing", "Build campaigns across SEO, social media, email, and paid ads.", "Marketing", "marketing,digital-marketing,business,sales", "Beginner Level", 39.99, ["marketing", "business", "sales"]),
                new("Sales Mastery for Growth Teams", "sales-mastery", "Prospecting, negotiation, closing, and customer relationship skills.", "Marketing", "sales,marketing,business,communication", "Intermediate Level", 42.99, ["sales", "marketing", "business"]),
                new("Entrepreneurship and Startup Launch", "entrepreneurship", "Validate ideas, build business models, and launch your startup.", "Business", "entrepreneurship,business,startup,strategy", "All Levels", 49.99, ["entrepreneurship", "business", "strategy"]),
                new("Accounting Essentials for Managers", "accounting-essentials", "Understand financial statements, budgeting, and business accounting.", "Finance", "accounting,finance,business,management", "Beginner Level", 37.99, ["accounting", "finance", "business"]),
                new("Graphic Design Fundamentals", "graphic-design", "Typography, color theory, layout, and visual communication basics.", "Design", "graphic-design,design,creativity,visual", "Beginner Level", 34.99, ["graphic-design", "design", "creativity"]),
                new("UI Design with Figma", "ui-design-figma", "Design modern interfaces, components, and prototypes in Figma.", "Design", "ui-design,design,figma,ux", "Intermediate Level", 41.99, ["ui-design", "design", "figma"]),
                new("UX Research and Product Design", "ux-research", "Conduct user research and design intuitive product experiences.", "Design", "ux-design,design,research,product", "Intermediate Level", 46.99, ["ux-design", "design", "research"]),
                new("Adobe Photoshop for Creatives", "photoshop-editing", "Photo editing, retouching, and creative compositing in Photoshop.", "Design", "photoshop,design,editing,creativity", "Beginner Level", 36.99, ["photoshop", "design", "editing"]),
                new("Business English Communication", "business-english", "Professional English for meetings, emails, and presentations.", "Languages", "english,language,business,communication", "All Levels", 29.99, ["english", "language", "communication"]),
                new("German for Beginners", "german-language", "Build conversational German skills for travel and work.", "Languages", "german,language,speaking,communication", "Beginner Level", 27.99, ["german", "language", "speaking"]),
                new("French Conversation Skills", "french-language", "Improve French speaking, listening, and everyday communication.", "Languages", "french,language,speaking,communication", "Beginner Level", 27.99, ["french", "language", "speaking"]),
                new("Spanish for Professionals", "spanish-language", "Workplace Spanish for meetings, travel, and client communication.", "Languages", "spanish,language,speaking,business", "All Levels", 31.99, ["spanish", "language", "speaking"]),
                new("Leadership and Team Management", "leadership-management", "Lead teams effectively with communication, feedback, and motivation.", "Leadership", "leadership,management,communication,teams", "Intermediate Level", 43.99, ["leadership", "communication", "management"]),
                new("Public Speaking Confidence", "public-speaking", "Overcome stage fear and deliver compelling presentations.", "Personal Development", "public-speaking,communication,leadership,presentation", "All Levels", 33.99, ["public-speaking", "communication", "leadership"]),
                new("Productivity and Time Management", "productivity-time-management", "Prioritize tasks, build habits, and manage time effectively.", "Personal Development", "productivity,time-management,personal-development,habits", "All Levels", 28.99, ["productivity", "time-management", "personal-development"]),
                new("Nutrition and Healthy Eating", "nutrition-health", "Balanced nutrition, meal planning, and sustainable healthy habits.", "Health & Fitness", "nutrition,health,wellness,fitness", "All Levels", 32.99, ["nutrition", "health", "fitness"]),
                new("Home Workout and Weight Loss", "home-workout", "Fat loss routines, strength training, and home fitness programming.", "Health & Fitness", "fitness,health,weight-loss,workout", "Beginner Level", 29.99, ["fitness", "health", "weight-loss"]),
                new("Personal Finance and Investing", "personal-finance", "Budgeting, saving, investing, and long-term financial planning.", "Finance", "finance,investment,money,planning", "All Levels", 38.99, ["finance", "investment", "money"]),
                new("Photography Basics for Beginners", "photography-basics", "Camera settings, composition, lighting, and photo storytelling.", "Photography", "photography,camera,composition,editing", "Beginner Level", 35.99, ["photography", "camera", "editing"])
            ];
        }

        public static bool MatchesInterest(CourseTemplate course, StudentInterestGroup group)
        {
            return group switch
            {
                StudentInterestGroup.BackendDeveloper => course.IsTechnology &&
                    course.Topics.Any(topic => topic is "csharp" or "dotnet" or "aspnet" or "sql" or "entity-framework" or "backend" or "architecture"),
                StudentInterestGroup.MobileDeveloper => course.IsTechnology &&
                    course.Topics.Any(topic => topic is "flutter" or "mobile" or "dart" or "react-native"),
                StudentInterestGroup.FrontendDeveloper => course.IsTechnology &&
                    course.Topics.Any(topic => topic is "react" or "angular" or "javascript" or "frontend" or "typescript"),
                StudentInterestGroup.CyberSecurityLearner => course.IsTechnology &&
                    course.Topics.Any(topic => topic is "cybersecurity" or "security" or "networking" or "ethical-hacking"),
                StudentInterestGroup.DataScientist => course.IsTechnology &&
                    course.Topics.Any(topic => topic is "python" or "data-science" or "machine-learning" or "analytics" or "ml"),
                StudentInterestGroup.MarketingSpecialist => !course.IsTechnology &&
                    course.Topics.Any(topic => topic is "marketing" or "sales" or "digital-marketing"),
                StudentInterestGroup.BusinessOwner => !course.IsTechnology &&
                    course.Topics.Any(topic => topic is "business" or "entrepreneurship" or "strategy" or "project-management" or "accounting"),
                StudentInterestGroup.GraphicDesigner => !course.IsTechnology &&
                    course.Topics.Any(topic => topic is "design" or "graphic-design" or "ui-design" or "ux-design" or "photoshop" or "figma"),
                StudentInterestGroup.LanguageLearner => !course.IsTechnology &&
                    course.Topics.Any(topic => topic is "english" or "german" or "french" or "spanish" or "language"),
                StudentInterestGroup.FitnessEnthusiast => !course.IsTechnology &&
                    course.Topics.Any(topic => topic is "fitness" or "health" or "nutrition" or "weight-loss" or "workout"),
                StudentInterestGroup.Photographer => !course.IsTechnology &&
                    course.Topics.Any(topic => topic is "photography" or "camera" or "editing"),
                _ => false
            };
        }

        public static Guid CreateDeterministicGuid(string seed)
        {
            var bytes = System.Security.Cryptography.MD5.HashData(
                System.Text.Encoding.UTF8.GetBytes($"EduVerse.Seed.v1.{seed}"));
            return new Guid(bytes);
        }
    }

    internal enum StudentInterestGroup
    {
        BackendDeveloper,
        MobileDeveloper,
        FrontendDeveloper,
        CyberSecurityLearner,
        DataScientist,
        MarketingSpecialist,
        BusinessOwner,
        GraphicDesigner,
        LanguageLearner,
        FitnessEnthusiast,
        Photographer
    }

    internal sealed record StudentInterestProfile(
        int StudentIndex,
        StudentInterestGroup Group,
        string Label);

    internal sealed record CourseTemplate(
        string Key,
        string Name,
        string Title,
        string Description,
        string Category,
        string Tags,
        string Level,
        double Price,
        bool IsTechnology,
        string[] Topics);

    internal sealed record RawCourseTemplate(
        string Title,
        string Name,
        string Description,
        string Category,
        string Tags,
        string Level,
        double PriceBase,
        string[] Topics);
}
