using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InfraStructure.Data;

public static class SeedData
{
    private const string DefaultPassword = "P@ssw0rd123!";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        string[] roles = ["Admin", "Instructor", "Student"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Prevent duplicate seeding.
        if (await context.Categories.AnyAsync() && await context.Courses.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        var organization = await context.Organizations.FirstOrDefaultAsync(o => o.Email == "info@eduverse.local");
        if (organization is null)
        {
            organization = new Organization
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "EduVerse Academy",
                Description = "Fake academy used for development and testing.",
                Email = "info@eduverse.local",
                PhoneNumber = "+201000000000",
                LogoUrl = "/images/orgs/eduverse.png",
                WebsiteUrl = "https://eduverse.local",
                Status = "Active",
                CreatedAt = now,
                CreatedByName = "SeedData"
            };

            context.Organizations.Add(organization);
            await context.SaveChangesAsync();
        }

        var admin = await EnsureUserAsync(userManager, roleManager, new AppUser
        {
            Id = "seed-admin-0001",
            UserName = "admin@eduverse.local",
            Email = "admin@eduverse.local",
            EmailConfirmed = true,
            FullName = "Mohamed Magdy Admin",
            Birthdate = new DateOnly(2000, 1, 1),
            OrganizationId = organization.Id,
            ProfilePicture = "/images/users/admin.png"
        }, "Admin");

        var instructors = new List<AppUser>();
        string[] instructorNames =
        [
            "Ahmed Hassan", "Mona Adel", "Youssef Karim", "Sara Mostafa", "Omar Tarek",
            "Nour Ali", "Hana Samir", "Khaled Nabil", "Mai Ibrahim", "Mostafa Gamal",
            "Dina Ashraf", "Hossam Farid", "Rana Sherif", "Amr Salah", "Heba Fathy",
            "Tamer Wagdy", "Aya Essam", "Mahmoud Reda", "Lina Omar", "Sherif Adel"
        ];

        for (int i = 0; i < instructorNames.Length; i++)
        {
            var instructor = await EnsureUserAsync(userManager, roleManager, new AppUser
            {
                Id = $"seed-instructor-{i + 1:0000}",
                UserName = $"instructor{i + 1}@eduverse.local",
                Email = $"instructor{i + 1}@eduverse.local",
                EmailConfirmed = true,
                FullName = instructorNames[i],
                Birthdate = new DateOnly(1988 + (i % 10), (i % 12) + 1, (i % 25) + 1),
                OrganizationId = organization.Id,
                ProfilePicture = $"/images/users/instructor-{i + 1}.png"
            }, "Instructor");

            instructors.Add(instructor);
        }

        var students = new List<AppUser>();
        string[] firstNames = ["Ali", "Omar", "Youssef", "Mariam", "Nour", "Hana", "Salma", "Nada", "Karim", "Adel", "Farah", "Malak", "Ziad", "Seif", "Laila", "Jana", "Talia", "Mazen", "Adam", "Yara"];
        string[] lastNames = ["Ahmed", "Mohamed", "Hassan", "Ibrahim", "Mahmoud", "Mostafa", "Samir", "Tarek", "Nabil", "Gamal"];

        for (int i = 1; i <= 300; i++)
        {
            var fullName = $"{firstNames[(i - 1) % firstNames.Length]} {lastNames[(i - 1) % lastNames.Length]}";
            var student = await EnsureUserAsync(userManager, roleManager, new AppUser
            {
                Id = $"seed-student-{i:0000}",
                UserName = $"student{i}@eduverse.local",
                Email = $"student{i}@eduverse.local",
                EmailConfirmed = true,
                FullName = fullName,
                Birthdate = new DateOnly(2000 + (i % 7), ((i + 2) % 12) + 1, ((i + 4) % 25) + 1),
                OrganizationId = organization.Id,
                ProfilePicture = $"/images/users/student-{i}.png"
            }, "Student");

            students.Add(student);
        }

        var categories = await SeedCategoriesAsync(context);
        var courses = await SeedCoursesAsync(context, organization, admin, instructors, categories);
        var sessions = await SeedSessionsAndAssignmentsAsync(context, courses, instructors);
        await SeedEnrollmentsPaymentsRatingsAsync(context, students, courses);
    }

    private static async Task<AppUser> EnsureUserAsync(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, AppUser user, string role)
    {
        var existing = await userManager.FindByEmailAsync(user.Email!);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
                await userManager.AddToRoleAsync(existing, role);
            return existing;
        }

        var result = await userManager.CreateAsync(user, DefaultPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create seed user {user.Email}: {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static async Task<List<Category>> SeedCategoriesAsync(AppDbContext context)
    {
        var requiredCategories = new List<Category>
        {
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Name = "Software Development", Description = "Web, backend, mobile, APIs, and software engineering." },
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Name = "Data Science", Description = "Data analysis, machine learning, AI, and visualization." },
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), Name = "Cloud Computing", Description = "Azure, DevOps, deployment, containers, and cloud architecture." },
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), Name = "Cyber Security", Description = "Security fundamentals, network security, and ethical hacking." },
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000005"), Name = "Business", Description = "Business strategy, management, and entrepreneurship." },
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000006"), Name = "Marketing", Description = "Digital marketing, SEO, ads, and content strategy." },
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000007"), Name = "Design", Description = "UI UX, Figma, product design, and visual design." },
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000008"), Name = "Finance", Description = "Accounting, investment, financial analysis, and Excel." },
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000009"), Name = "Languages", Description = "English, German, communication, and presentation skills." },
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000010"), Name = "Personal Development", Description = "Productivity, leadership, and career growth." }
        };

        var existingCategories = await context.Categories.ToListAsync();

        foreach (var required in requiredCategories)
        {
            var exists = existingCategories.Any(c =>
                c.Name.Trim().ToLower() == required.Name.Trim().ToLower());

            if (!exists)
                context.Categories.Add(required);
        }

        await context.SaveChangesAsync();
        return await context.Categories.ToListAsync();
    }

    private static async Task<List<Course>> SeedCoursesAsync(AppDbContext context, Organization organization, AppUser admin, List<AppUser> instructors, List<Category> categories)
    {
        if (await context.Courses.AnyAsync())
            return await context.Courses.ToListAsync();

        var softwareCourses = new[]
        {
            ("ASP.NET Core Web API", "Build secure REST APIs with EF Core and JWT", "dotnet,csharp,asp.net,api,ef core,jwt,backend", "Intermediate", 1200d, 28d, "Software Development"),
            ("Full Stack React and .NET", "Create a complete LMS-style web application", "react,dotnet,web,api,sql,jwt,frontend,backend", "Intermediate", 1500d, 36d, "Software Development"),
            ("Flutter Mobile Development", "Build Android and iOS apps using Flutter", "flutter,dart,mobile,ui,api,firebase", "Beginner", 1000d, 30d, "Software Development"),
            ("SQL Server for Developers", "Database design, queries, indexes, and stored procedures", "sql,sql server,database,queries,indexes,stored procedures", "Beginner", 700d, 18d, "Software Development"),
            ("Clean Architecture in .NET", "Structure enterprise applications using clean architecture", "clean architecture,dotnet,solid,repository,cqrs,mediatr", "Advanced", 1400d, 24d, "Software Development"),
            ("JavaScript Essentials", "Core JavaScript, DOM, async programming, and APIs", "javascript,dom,async,frontend,web", "Beginner", 500d, 14d, "Software Development"),
            ("Next.js App Router", "Build modern server-rendered React applications", "next.js,react,app router,typescript,tailwind", "Intermediate", 900d, 20d, "Software Development"),
            ("Software Testing Fundamentals", "Manual testing, test cases, Selenium basics", "testing,qa,selenium,test cases,automation", "Beginner", 600d, 16d, "Software Development"),
            ("Microservices with .NET", "Design distributed services with messaging and Docker", "microservices,dotnet,docker,rabbitmq,api", "Advanced", 1800d, 32d, "Software Development"),
            ("Git and GitHub Masterclass", "Version control, branches, pull requests, and workflows", "git,github,version control,branches,pull requests", "Beginner", 300d, 8d, "Software Development"),
            ("Entity Framework Core Deep Dive", "Migrations, relationships, LINQ, performance", "ef core,dotnet,linq,migrations,relationships,sql", "Intermediate", 850d, 18d, "Software Development"),
            ("C# OOP and LINQ", "Master OOP principles and LINQ queries in C#", "csharp,oop,linq,classes,interfaces", "Beginner", 650d, 20d, "Software Development"),
            ("Docker for Developers", "Containerize web APIs and databases", "docker,containers,devops,api,sql server", "Intermediate", 800d, 14d, "Cloud Computing"),
            ("Azure App Service Deployment", "Publish .NET and Next.js apps to Azure", "azure,app service,deployment,dotnet,next.js,cloud", "Intermediate", 950d, 16d, "Cloud Computing"),
            ("REST API Design", "Design clean APIs with authentication and documentation", "rest,api,swagger,scalar,jwt,backend", "Beginner", 550d, 12d, "Software Development"),
            ("Blazor Desktop Apps", "Build desktop apps using Blazor Hybrid", "blazor,desktop,winui,dotnet,local storage", "Intermediate", 1000d, 22d, "Software Development"),
            ("Python for Automation", "Use Python scripts to automate daily tasks", "python,automation,scripts,files,apis", "Beginner", 500d, 12d, "Software Development"),
            ("Data Structures in C#", "Arrays, lists, stacks, queues, trees, and hashing", "csharp,data structures,algorithms,problem solving", "Intermediate", 700d, 20d, "Software Development"),
            ("Algorithms and Problem Solving", "Sorting, searching, recursion, and complexity", "algorithms,complexity,recursion,sorting,searching", "Intermediate", 750d, 22d, "Software Development"),
            ("Secure Authentication with JWT", "Implement login, roles, claims, refresh tokens", "jwt,identity,roles,claims,security,dotnet", "Intermediate", 900d, 14d, "Cyber Security"),
            ("Machine Learning Basics", "Regression, classification, evaluation, and recommendation", "machine learning,regression,classification,recommendation,python", "Beginner", 1100d, 26d, "Data Science"),
            ("Content Based Recommendation Systems", "Build tag-based course recommendation engines", "recommendation system,content based,tags,similarity,ml", "Intermediate", 1300d, 18d, "Data Science"),
            ("Data Analysis with Python", "Pandas, NumPy, charts, and real datasets", "python,pandas,numpy,data analysis,visualization", "Beginner", 900d, 20d, "Data Science"),
            ("Parallel Computing Basics", "OpenMP, MPI, speedup, scalability, and GPU concepts", "parallel computing,openmp,mpi,cpu,gpu,scalability", "Intermediate", 1000d, 18d, "Software Development")
        };

        var otherCourses = new[]
        {
            ("Digital Marketing Fundamentals", "SEO, social media, ads, and funnels", "marketing,seo,social media,ads,content", "Beginner", 600d, 14d, "Marketing"),
            ("UI UX Design with Figma", "Wireframes, prototypes, components, and design systems", "ui ux,figma,wireframe,prototype,design", "Beginner", 800d, 18d, "Design"),
            ("Business Analysis Basics", "Requirements, stakeholders, diagrams, and documentation", "business analysis,requirements,stakeholders,uml", "Beginner", 700d, 16d, "Business"),
            ("Project Management Essentials", "Plan, track, and deliver software and business projects", "project management,agile,scrum,planning", "Beginner", 750d, 15d, "Business"),
            ("Financial Accounting", "Understand statements, journals, and business finance", "accounting,finance,statements,journal", "Beginner", 650d, 20d, "Finance"),
            ("Excel for Business", "Formulas, charts, dashboards, and data cleaning", "excel,dashboard,formulas,pivot table,data", "Beginner", 500d, 12d, "Finance"),
            ("English Conversation", "Improve speaking, listening, and interview communication", "english,conversation,speaking,listening,communication", "Beginner", 400d, 16d, "Languages"),
            ("German A1", "Start German grammar, vocabulary, and daily phrases", "german,language,a1,vocabulary,grammar", "Beginner", 600d, 24d, "Languages"),
            ("Career Development", "CV writing, interviews, LinkedIn, and job search", "career,cv,interview,linkedin,jobs", "Beginner", 350d, 8d, "Personal Development"),
            ("Leadership Skills", "Communication, delegation, decision making, and teamwork", "leadership,teamwork,communication,management", "Intermediate", 600d, 10d, "Personal Development"),
            ("Entrepreneurship Basics", "Idea validation, MVP, market research, and pitching", "entrepreneurship,startup,mvp,pitching,business", "Beginner", 900d, 18d, "Business"),
            ("Graphic Design Basics", "Color, typography, layouts, and brand visuals", "graphic design,typography,color,branding", "Beginner", 650d, 16d, "Design"),
            ("Content Creation", "Plan, script, record, and publish content", "content creation,video,scripting,social media", "Beginner", 550d, 12d, "Marketing"),
            ("Investment Fundamentals", "Stocks, risk, diversification, and long-term planning", "investment,stocks,finance,risk,portfolio", "Beginner", 700d, 14d, "Finance"),
            ("Presentation Skills", "Create confident presentations and clear storytelling", "presentation,public speaking,storytelling,communication", "Beginner", 450d, 10d, "Personal Development"),
            ("Product Management", "Roadmaps, user stories, metrics, and discovery", "product management,roadmap,user stories,metrics", "Intermediate", 1000d, 20d, "Business")
        };

        var baseCourseTemplates = softwareCourses.Concat(otherCourses).ToList();

        // Expand the base list into a larger realistic catalog.
        // Final result: 120 courses. Around 60% are software/AI related and 40% are mixed fields.
        var allCourseTemplates = new List<(string Name, string Description, string Tags, string Level, double Price, double Duration, string Category)>();
        allCourseTemplates.AddRange(baseCourseTemplates);

        string[] variants = ["Bootcamp", "Practical Projects", "Advanced Lab", "Career Track", "Crash Course"];
        string[] variantTags = ["bootcamp,hands on,practice", "projects,portfolio,real world", "advanced,lab,case study", "career,interview,portfolio", "crash course,fast track,essentials"];

        var targetCourseCount = 120;
        var expansionIndex = 0;
        while (allCourseTemplates.Count < targetCourseCount)
        {
            var source = baseCourseTemplates[expansionIndex % baseCourseTemplates.Count];
            var variantIndex = expansionIndex % variants.Length;
            var level = variantIndex switch
            {
                0 => source.Item4,
                1 => "Intermediate",
                2 => "Advanced",
                3 => source.Item4,
                _ => "Beginner"
            };

            allCourseTemplates.Add((
                $"{source.Item1} - {variants[variantIndex]}",
                $"{source.Item2}. Includes extra exercises, quizzes, and portfolio tasks.",
                $"{source.Item3},{variantTags[variantIndex]}",
                level,
                Math.Round(source.Item5 * (1 + (variantIndex * 0.12)), 0),
                source.Item6 + (variantIndex * 4),
                source.Item7
            ));

            expansionIndex++;
        }

        var courses = new List<Course>();
        var courseCategories = new List<CourseCategory>();

        for (int i = 0; i < allCourseTemplates.Count; i++)
        {
            var item = allCourseTemplates[i];
            var instructor = instructors[i % instructors.Count];
            var courseId = Guid.Parse($"30000000-0000-0000-0000-{i + 1:000000000000}");

            var course = new Course
            {
                Id = courseId,
                Name = item.Item1,
                Title = item.Item1,
                Description = item.Item2,
                Price = item.Item5,
                Duration = item.Item6,
                ImageUrl = $"/images/courses/course-{i + 1}.jpg",
                IsDeleted = false,
                OrganizationId = organization.Id,
                OrgId = admin.Id,
                InstructorId = instructor.Id,
                Tags = item.Item3,
                Level = item.Item4
            };

            courses.Add(course);

            var category = categories.FirstOrDefault(c => string.Equals(c.Name.Trim(), item.Item7.Trim(), StringComparison.OrdinalIgnoreCase));
            if (category is null)
                throw new InvalidOperationException($"Missing category in database: {item.Item7}. Check SeedCategoriesAsync.");

            courseCategories.Add(new CourseCategory { CourseId = courseId, CategoryId = category.Id });

            // Add a second category for hybrid courses.
            if (item.Item3.Contains("azure") || item.Item3.Contains("docker"))
                courseCategories.Add(new CourseCategory { CourseId = courseId, CategoryId = categories.First(c => string.Equals(c.Name.Trim(), "Cloud Computing", StringComparison.OrdinalIgnoreCase)).Id });
            if (item.Item3.Contains("jwt") || item.Item3.Contains("security"))
                courseCategories.Add(new CourseCategory { CourseId = courseId, CategoryId = categories.First(c => string.Equals(c.Name.Trim(), "Cyber Security", StringComparison.OrdinalIgnoreCase)).Id });
            if (item.Item3.Contains("machine learning") || item.Item3.Contains("data"))
                courseCategories.Add(new CourseCategory { CourseId = courseId, CategoryId = categories.First(c => string.Equals(c.Name.Trim(), "Data Science", StringComparison.OrdinalIgnoreCase)).Id });
        }

        context.Courses.AddRange(courses);
        context.CourseCategories.AddRange(courseCategories.DistinctBy(cc => new { cc.CourseId, cc.CategoryId }));
        await context.SaveChangesAsync();
        return courses;
    }

    private static async Task<List<Session>> SeedSessionsAndAssignmentsAsync(AppDbContext context, List<Course> courses, List<AppUser> instructors)
    {
        if (await context.Sessions.AnyAsync())
            return await context.Sessions.ToListAsync();

        var sessions = new List<Session>();
        var assignments = new List<Assignment>();
        var now = DateTime.UtcNow;

        for (int c = 0; c < courses.Count; c++)
        {
            var course = courses[c];
            var trainerId = course.InstructorId ?? instructors[c % instructors.Count].Id;

            for (int s = 1; s <= 8; s++)
            {
                var sessionId = Guid.Parse($"40000000-0000-{c + 1:0000}-0000-{s:000000000000}");
                sessions.Add(new Session
                {
                    Id = sessionId,
                    CourseId = course.Id,
                    Title = $"{course.Name} - Session {s}",
                    Description = s switch
                    {
                        1 => "Introduction and setup",
                        2 => "Core concepts and guided examples",
                        3 => "Hands-on practice",
                        4 => "Mini project implementation",
                        _ => "Review and final assessment"
                    },
                    FileUrl = $"/files/courses/{course.Id}/session-{s}.pdf",
                    VideoUrl = $"https://videos.eduverse.local/{course.Id}/session-{s}",
                    ExternalLink = $"https://resources.eduverse.local/{course.Id}/session-{s}",
                    TrainerId = trainerId,
                    Date = now.AddDays(c + s),
                    Duration = 2,
                    SessionNumber = s,
                    AttendanceCode = $"EV{c + 1:00}{s:00}",
                    AttendanceCodeCreatedAt = now
                });

                if (s is 2 or 4 or 6 or 8)
                {
                    assignments.Add(new Assignment
                    {
                        Id = Guid.Parse($"50000000-0000-{c + 1:0000}-0000-{s:000000000000}"),
                        SessionId = sessionId,
                        Subject = $"Assignment {s / 2}: {course.Name}",
                        Description = "Solve the practical task based on this session.",
                        Content = "Submit your solution as a PDF or project file.",
                        DueDate = now.AddDays(c + s + 7)
                    });
                }
            }
        }

        context.Sessions.AddRange(sessions);
        context.Assignments.AddRange(assignments);
        await context.SaveChangesAsync();
        return sessions;
    }

    private static async Task SeedEnrollmentsPaymentsRatingsAsync(AppDbContext context, List<AppUser> students, List<Course> courses)
    {
        if (await context.Enrollments.AnyAsync())
            return;

        var enrollments = new List<Enrollment>();
        var payments = new List<Payment>();
        var ratings = new List<Rating>();
        var now = DateTime.UtcNow;

        for (int i = 0; i < students.Count; i++)
        {
            var student = students[i];
            var preferredCourses = i < 60
                ? courses.Where(c => (c.Tags ?? "").Contains("dotnet") || (c.Tags ?? "").Contains("react") || (c.Tags ?? "").Contains("api") || (c.Tags ?? "").Contains("python") || (c.Tags ?? "").Contains("data")).ToList()
                : courses.Where(c => !(c.Tags ?? "").Contains("dotnet") && !(c.Tags ?? "").Contains("react") && !(c.Tags ?? "").Contains("api")).ToList();

            var selectedCourses = preferredCourses
                .OrderBy(c => Math.Abs(c.Name.GetHashCode() + i) % 1000)
                .Take(8)
                .ToList();

            foreach (var course in selectedCourses)
            {
                var progress = ((i * 13 + course.Name.Length) % 101);
                var isCompleted = progress >= 90;

                enrollments.Add(new Enrollment
                {
                    CourseId = course.Id,
                    StudentId = student.Id,
                    EnrollmentDate = now.AddDays(-((i % 30) + 1)),
                    Progression = progress,
                    ProgressPercentage = progress,
                    IsCompleted = isCompleted,
                    CompletedAt = isCompleted ? now.AddDays(-(i % 5)) : null,
                    GraduationDate = isCompleted ? now.AddDays(-(i % 3)) : null,
                    CertificateCode = isCompleted ? $"EV-CERT-{student.Id[^4..]}-{course.Id.ToString()[..8]}" : null,
                    FileUrl = isCompleted ? $"/certificates/{student.Id}/{course.Id}.pdf" : null
                });

                payments.Add(new Payment
                {
                    CourseId = course.Id,
                    StudentId = student.Id,
                    SubmittingDate = now.AddDays(-((i % 30) + 1)),
                    TotalPrice = course.Price,
                    PaymentMethod = i % 3 == 0 ? "Card" : i % 3 == 1 ? "Wallet" : "Cash",
                    PaymentStatus = "Paid",
                    PaymentProvider = i % 3 == 2 ? "Manual" : "Paymob",
                    SpecialReference = $"SEED-{student.Id[^4..]}-{course.Id.ToString()[..8]}",
                    MerchantOrderId = $"MO-{Guid.NewGuid():N}"[..20],
                    ProviderStatusCode = 200,
                    ProviderResponse = "Seed payment approved"
                });

                ratings.Add(new Rating
                {
                    CourseId = course.Id,
                    StudentId = student.Id,
                    RatingValue = 3 + ((i + course.Name.Length) % 3) // 3, 4, or 5
                });
            }
        }

        context.Enrollments.AddRange(enrollments.DistinctBy(e => new { e.CourseId, e.StudentId }));
        context.Payments.AddRange(payments.DistinctBy(p => new { p.CourseId, p.StudentId }));
        context.Ratings.AddRange(ratings.DistinctBy(r => new { r.CourseId, r.StudentId }));
        await context.SaveChangesAsync();
    }
}
