using Bogus;
using Domain.Entities;
using InfraStructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfraStructure.Data.Seed
{
    public class RecommendationDataSeeder
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SeedOptions _options;
        private readonly ILogger<RecommendationDataSeeder> _logger;
        private readonly Faker _faker = new(locale: "en");

        public RecommendationDataSeeder(
            AppDbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<SeedOptions> options,
            ILogger<RecommendationDataSeeder> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<SeedReport> SeedAsync(CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Data seeding is disabled.");
                return SeedReport.Disabled();
            }

            if (await IsAlreadySeededAsync(cancellationToken))
            {
                _logger.LogInformation("Recommendation seed data already exists. Skipping seeding.");
                return await BuildExistingReportAsync(cancellationToken);
            }

            _logger.LogInformation("Starting recommendation seed data generation...");

            await EnsureRolesAsync();
            var categories = await SeedCategoriesAsync(cancellationToken);
            var organizations = await SeedOrganizationsAsync(cancellationToken);
            var admins = await SeedAdminsAsync(cancellationToken);
            var instructors = await SeedInstructorsAsync(organizations, cancellationToken);
            var students = await SeedStudentsAsync(cancellationToken);
            var courses = await SeedCoursesAsync(categories, organizations, admins, instructors, cancellationToken);
            var enrollments = await SeedEnrollmentsAsync(students, courses, cancellationToken);
            var ratings = await SeedRatingsAsync(enrollments, cancellationToken);
            await EnsureMarkerUserAsync(admins.First().Id, cancellationToken);

            var report = await BuildReportAsync(cancellationToken);
            LogReport(report);
            return report;
        }

        private async Task<bool> IsAlreadySeededAsync(CancellationToken cancellationToken)
        {
            var markerExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(user => user.Email == SeedCatalog.MarkerEmail, cancellationToken);

            var seededCourseCount = await _context.Courses
                .AsNoTracking()
                .CountAsync(course => course.Name.StartsWith("[SEED]"), cancellationToken);

            return markerExists && seededCourseCount >= SeedCatalog.CourseCount;
        }

        private async Task EnsureRolesAsync()
        {
            foreach (var role in SeedCatalog.Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private async Task<Dictionary<string, Category>> SeedCategoriesAsync(CancellationToken cancellationToken)
        {
            var map = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
            var existing = await _context.Categories.ToListAsync(cancellationToken);

            foreach (var (name, description) in SeedCatalog.Categories)
            {
                var category = existing.FirstOrDefault(item =>
                    string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));

                if (category == null)
                {
                    category = new Category
                    {
                        Id = SeedCatalog.CreateDeterministicGuid($"category-{name}"),
                        Name = name,
                        Description = description
                    };
                    _context.Categories.Add(category);
                }

                map[name] = category;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return map;
        }

        private async Task<List<Organization>> SeedOrganizationsAsync(CancellationToken cancellationToken)
        {
            var organizations = new List<Organization>();

            for (var index = 0; index < SeedCatalog.OrganizationCount; index++)
            {
                var (name, description) = SeedCatalog.Organizations[index];
                var organizationId = SeedCatalog.CreateDeterministicGuid($"organization-{index + 1:D2}");
                var organization = await _context.Organizations
                    .FirstOrDefaultAsync(item => item.Id == organizationId, cancellationToken);

                if (organization == null)
                {
                    organization = new Organization
                    {
                        Id = organizationId,
                        Name = name,
                        Description = description,
                        Email = $"org.{index + 1:D2}@{SeedCatalog.EmailDomain}",
                        PhoneNumber = _faker.Phone.PhoneNumber(),
                        WebsiteUrl = $"https://www.{Slugify(name)}.demo",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow.AddDays(-_faker.Random.Int(30, 400)),
                        CreatedByName = "Seed System"
                    };
                    _context.Organizations.Add(organization);
                }

                organizations.Add(organization);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return organizations;
        }

        private async Task<List<AppUser>> SeedAdminsAsync(CancellationToken cancellationToken)
        {
            var admins = new List<AppUser>();

            for (var index = 1; index <= SeedCatalog.AdminCount; index++)
            {
                var email = $"admin.{index:D3}@{SeedCatalog.EmailDomain}";
                var user = await EnsureUserAsync(
                    email,
                    $"admin.{index:D3}",
                    _faker.Name.FullName(),
                    "admin",
                    null,
                    cancellationToken);
                admins.Add(user);
            }

            return admins;
        }

        private async Task<List<AppUser>> SeedInstructorsAsync(
            IReadOnlyList<Organization> organizations,
            CancellationToken cancellationToken)
        {
            var instructors = new List<AppUser>();

            for (var index = 1; index <= SeedCatalog.InstructorCount; index++)
            {
                var email = $"instructor.{index:D3}@{SeedCatalog.EmailDomain}";
                var organization = organizations[(index - 1) % organizations.Count];
                var user = await EnsureUserAsync(
                    email,
                    $"instructor.{index:D3}",
                    _faker.Name.FullName(),
                    "instructor",
                    organization.Id,
                    cancellationToken);
                instructors.Add(user);
            }

            return instructors;
        }

        private async Task<List<AppUser>> SeedStudentsAsync(CancellationToken cancellationToken)
        {
            var students = new List<AppUser>();

            for (var index = 1; index <= SeedCatalog.StudentCount; index++)
            {
                var email = $"student.{index:D3}@{SeedCatalog.EmailDomain}";
                var user = await EnsureUserAsync(
                    email,
                    $"student.{index:D3}",
                    _faker.Name.FullName(),
                    "student",
                    null,
                    cancellationToken);
                students.Add(user);
            }

            return students;
        }

        private async Task<AppUser> EnsureUserAsync(
            string email,
            string userName,
            string fullName,
            string role,
            Guid? organizationId,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                return user;
            }

            user = new AppUser
            {
                Id = SeedCatalog.CreateDeterministicGuid($"user-{email}").ToString(),
                Email = email,
                UserName = userName,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = userName.ToUpperInvariant(),
                FullName = fullName,
                Birthdate = DateOnly.FromDateTime(_faker.Date.Between(DateTime.UtcNow.AddYears(-45), DateTime.UtcNow.AddYears(-18))),
                EmailConfirmed = true,
                PhoneNumber = _faker.Phone.PhoneNumber(),
                PhoneNumberConfirmed = false,
                OrganizationId = organizationId,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, _options.SeedPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create seed user '{email}': {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }

            await _userManager.AddToRoleAsync(user, role);
            return user;
        }

        private async Task<List<Course>> SeedCoursesAsync(
            IReadOnlyDictionary<string, Category> categories,
            IReadOnlyList<Organization> organizations,
            IReadOnlyList<AppUser> admins,
            IReadOnlyList<AppUser> instructors,
            CancellationToken cancellationToken)
        {
            var courses = new List<Course>();
            var primaryAdmin = admins.First();

            foreach (var template in SeedCatalog.Courses)
            {
                var courseId = SeedCatalog.CreateDeterministicGuid($"course-{template.Key}");
                var course = await _context.Courses
                    .FirstOrDefaultAsync(item => item.Id == courseId, cancellationToken);

                if (course == null)
                {
                    var organization = organizations[_faker.Random.Int(0, organizations.Count - 1)];
                    var instructor = instructors[_faker.Random.Int(0, instructors.Count - 1)];

                    course = new Course
                    {
                        Id = courseId,
                        Name = template.Name,
                        Title = template.Title,
                        Description = template.Description,
                        Price = template.Price,
                        Duration = _faker.Random.Double(4, 40),
                        ImageUrl = $"{courseId}-Thumbnail",
                        Tags = template.Tags,
                        Level = template.Level,
                        IsDeleted = false,
                        OrgId = primaryAdmin.Id,
                        OrganizationId = organization.Id,
                        InstructorId = instructor.Id
                    };

                    _context.Courses.Add(course);

                    var category = categories[template.Category];
                    _context.CourseCategories.Add(new CourseCategory
                    {
                        CourseId = course.Id,
                        CategoryId = category.Id
                    });
                }

                courses.Add(course);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return courses;
        }

        private async Task<List<Enrollment>> SeedEnrollmentsAsync(
            IReadOnlyList<AppUser> students,
            IReadOnlyList<Course> courses,
            CancellationToken cancellationToken)
        {
            var enrollments = new List<Enrollment>();
            var courseTemplates = SeedCatalog.Courses.ToDictionary(
                template => SeedCatalog.CreateDeterministicGuid($"course-{template.Key}"),
                template => template);

            foreach (var profile in SeedCatalog.StudentProfiles)
            {
                var student = students[profile.StudentIndex - 1];
                var enrollmentCount = _faker.Random.Int(3, 10);
                var primaryCount = (int)Math.Round(enrollmentCount * _faker.Random.Double(0.7, 0.9));
                primaryCount = Math.Clamp(primaryCount, 1, enrollmentCount);

                var primaryCourses = courses
                    .Where(course => courseTemplates.TryGetValue(course.Id, out var template)
                                     && SeedCatalog.MatchesInterest(template, profile.Group))
                    .OrderBy(_ => _faker.Random.Int())
                    .ToList();

                var secondaryCourses = courses
                    .Where(course => !primaryCourses.Contains(course))
                    .OrderBy(_ => _faker.Random.Int())
                    .ToList();

                var selectedCourses = new List<Course>();
                selectedCourses.AddRange(primaryCourses.Take(primaryCount));

                var remaining = enrollmentCount - selectedCourses.Count;
                if (remaining > 0)
                {
                    selectedCourses.AddRange(secondaryCourses.Take(remaining));
                }

                if (selectedCourses.Count < enrollmentCount)
                {
                    selectedCourses.AddRange(
                        courses
                            .Except(selectedCourses)
                            .OrderBy(_ => _faker.Random.Int())
                            .Take(enrollmentCount - selectedCourses.Count));
                }

                foreach (var course in selectedCourses.DistinctBy(course => course.Id))
                {
                    var exists = await _context.Enrollments.AnyAsync(
                        enrollment => enrollment.StudentId == student.Id && enrollment.CourseId == course.Id,
                        cancellationToken);

                    if (exists)
                    {
                        continue;
                    }

                    var shouldComplete = _faker.Random.Bool(0.45f);
                    var enrollment = new Enrollment
                    {
                        CourseId = course.Id,
                        StudentId = student.Id,
                        EnrollmentDate = DateTime.UtcNow.AddDays(-_faker.Random.Int(10, 300)),
                        Progression = shouldComplete ? 100 : _faker.Random.Double(5, 95),
                        ProgressPercentage = shouldComplete ? 100 : _faker.Random.Double(5, 95),
                        IsCompleted = shouldComplete,
                        CompletedAt = shouldComplete ? DateTime.UtcNow.AddDays(-_faker.Random.Int(1, 60)) : null
                    };

                    if (enrollment.ProgressPercentage >= 100)
                    {
                        enrollment.ProgressPercentage = 100;
                        enrollment.Progression = 100;
                        enrollment.IsCompleted = true;
                        enrollment.CompletedAt ??= DateTime.UtcNow.AddDays(-_faker.Random.Int(1, 30));
                    }

                    _context.Enrollments.Add(enrollment);
                    enrollments.Add(enrollment);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (enrollments.Count < SeedCatalog.MinEnrollmentCount)
            {
                enrollments.AddRange(await TopUpEnrollmentsAsync(students, courses, enrollments, cancellationToken));
            }

            return enrollments;
        }

        private async Task<List<Enrollment>> TopUpEnrollmentsAsync(
            IReadOnlyList<AppUser> students,
            IReadOnlyList<Course> courses,
            IReadOnlyList<Enrollment> currentEnrollments,
            CancellationToken cancellationToken)
        {
            var added = new List<Enrollment>();
            var existingPairs = currentEnrollments
                .Select(enrollment => (enrollment.StudentId, enrollment.CourseId))
                .ToHashSet();

            foreach (var student in students)
            {
                var studentEnrollmentCount = currentEnrollments.Count(enrollment => enrollment.StudentId == student.Id)
                    + added.Count(enrollment => enrollment.StudentId == student.Id);

                while (studentEnrollmentCount < 8
                       && currentEnrollments.Count + added.Count < SeedCatalog.MinEnrollmentCount + 200)
                {
                    var course = courses[_faker.Random.Int(0, courses.Count - 1)];
                    if (!existingPairs.Add((student.Id, course.Id)))
                    {
                        continue;
                    }

                    var enrollment = new Enrollment
                    {
                        CourseId = course.Id,
                        StudentId = student.Id,
                        EnrollmentDate = DateTime.UtcNow.AddDays(-_faker.Random.Int(10, 200)),
                        Progression = _faker.Random.Double(10, 80),
                        ProgressPercentage = _faker.Random.Double(10, 80),
                        IsCompleted = false
                    };

                    _context.Enrollments.Add(enrollment);
                    added.Add(enrollment);
                    studentEnrollmentCount++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return added;
        }

        private async Task<int> SeedRatingsAsync(
            IReadOnlyList<Enrollment> newEnrollments,
            CancellationToken cancellationToken)
        {
            var seededStudentIds = (await _context.Users
                    .Where(user => user.Email!.StartsWith("student.") && user.Email.EndsWith($"@{SeedCatalog.EmailDomain}"))
                    .Select(user => user.Id)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            var targetEnrollments = (await _context.Enrollments
                .Where(enrollment => seededStudentIds.Contains(enrollment.StudentId))
                .ToListAsync(cancellationToken))
                .OrderByDescending(enrollment => enrollment.IsCompleted)
                .ThenBy(_ => _faker.Random.Int())
                .ToList();

            var ratingCount = await _context.Ratings
                .CountAsync(rating => seededStudentIds.Contains(rating.StudentId), cancellationToken);

            foreach (var enrollment in targetEnrollments)
            {
                if (ratingCount >= SeedCatalog.TargetRatingCount)
                {
                    break;
                }

                var exists = await _context.Ratings.AnyAsync(
                    rating => rating.StudentId == enrollment.StudentId && rating.CourseId == enrollment.CourseId,
                    cancellationToken);

                if (exists)
                {
                    continue;
                }

                var shouldRate = enrollment.IsCompleted
                    ? _faker.Random.Bool(0.95f)
                    : _faker.Random.Bool(0.65f);

                if (!shouldRate)
                {
                    continue;
                }

                _context.Ratings.Add(new Rating
                {
                    StudentId = enrollment.StudentId,
                    CourseId = enrollment.CourseId,
                    RatingValue = PickRatingValue(enrollment.IsCompleted)
                });

                ratingCount++;
            }

            if (ratingCount < SeedCatalog.TargetRatingCount)
            {
                foreach (var enrollment in targetEnrollments)
                {
                    if (ratingCount >= SeedCatalog.TargetRatingCount)
                    {
                        break;
                    }

                    var exists = await _context.Ratings.AnyAsync(
                        rating => rating.StudentId == enrollment.StudentId && rating.CourseId == enrollment.CourseId,
                        cancellationToken);

                    if (exists)
                    {
                        continue;
                    }

                    _context.Ratings.Add(new Rating
                    {
                        StudentId = enrollment.StudentId,
                        CourseId = enrollment.CourseId,
                        RatingValue = PickRatingValue(enrollment.IsCompleted)
                    });

                    ratingCount++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return ratingCount;
        }

        private float PickRatingValue(bool completed)
        {
            var roll = _faker.Random.Double(0, 1);
            if (completed)
            {
                roll = Math.Min(1, roll + 0.12);
            }

            if (roll < 0.45) return 5;
            if (roll < 0.80) return 4;
            if (roll < 0.95) return 3;
            if (roll < 0.99) return 2;
            return 1;
        }

        private async Task EnsureMarkerUserAsync(string adminId, CancellationToken cancellationToken)
        {
            if (await _context.Users.AnyAsync(user => user.Email == SeedCatalog.MarkerEmail, cancellationToken))
            {
                return;
            }

            var marker = new AppUser
            {
                Id = SeedCatalog.CreateDeterministicGuid("seed-marker-user").ToString(),
                Email = SeedCatalog.MarkerEmail,
                UserName = "seed.marker",
                NormalizedEmail = SeedCatalog.MarkerEmail.ToUpperInvariant(),
                NormalizedUserName = "SEED.MARKER",
                FullName = "Seed Marker",
                Birthdate = new DateOnly(1990, 1, 1),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(marker, _options.SeedPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create seed marker user: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }

            await _userManager.AddToRoleAsync(marker, "admin");
        }

        private async Task<SeedReport> BuildReportAsync(CancellationToken cancellationToken)
        {
            var seededStudentEmails = await _context.Users
                .Where(user => user.Email!.StartsWith("student.") && user.Email.EndsWith($"@{SeedCatalog.EmailDomain}"))
                .Select(user => user.Email!)
                .OrderBy(email => email)
                .ToListAsync(cancellationToken);

            var seededCourses = await _context.Courses
                .AsNoTracking()
                .Where(course => course.Name.StartsWith("[SEED]"))
                .OrderBy(course => course.Name)
                .Select(course => new { course.Title, course.Tags, course.Level })
                .Take(5)
                .ToListAsync(cancellationToken);

            var techCourseCount = SeedCatalog.Courses.Count(course => course.IsTechnology);
            var nonTechCourseCount = SeedCatalog.Courses.Length - techCourseCount;

            var seededStudentIds = await _context.Users
                .Where(user => user.Email!.EndsWith($"@{SeedCatalog.EmailDomain}") && user.Email.StartsWith("student."))
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);

            var enrollmentCount = await _context.Enrollments
                .CountAsync(enrollment => seededStudentIds.Contains(enrollment.StudentId), cancellationToken);

            var ratingCount = await _context.Ratings
                .CountAsync(rating => seededStudentIds.Contains(rating.StudentId), cancellationToken);

            var completedCount = await _context.Enrollments
                .CountAsync(enrollment => seededStudentIds.Contains(enrollment.StudentId) && enrollment.IsCompleted, cancellationToken);

            var interestCounts = SeedCatalog.StudentProfiles
                .GroupBy(profile => profile.Label)
                .ToDictionary(group => group.Key, group => group.Count());

            return new SeedReport
            {
                Seeded = true,
                StudentCount = seededStudentIds.Count,
                InstructorCount = await _context.Users.CountAsync(
                    user => user.Email!.StartsWith("instructor.") && user.Email.EndsWith($"@{SeedCatalog.EmailDomain}"),
                    cancellationToken),
                AdminCount = await _context.Users.CountAsync(
                    user => user.Email!.StartsWith("admin.") && user.Email.EndsWith($"@{SeedCatalog.EmailDomain}"),
                    cancellationToken),
                OrganizationCount = SeedCatalog.OrganizationCount,
                CourseCount = await _context.Courses.CountAsync(course => course.Name.StartsWith("[SEED]"), cancellationToken),
                TechnologyCourseCount = techCourseCount,
                NonTechnologyCourseCount = nonTechCourseCount,
                EnrollmentCount = enrollmentCount,
                CompletedEnrollmentCount = completedCount,
                RatingCount = ratingCount,
                SampleStudentEmails = seededStudentEmails.Take(5).ToList(),
                SampleCourses = seededCourses.Select(course => $"{course.Title} ({course.Level}) [{course.Tags}]").ToList(),
                StudentInterestDistribution = interestCounts,
                SeedPassword = _options.SeedPassword
            };
        }

        private async Task<SeedReport> BuildExistingReportAsync(CancellationToken cancellationToken)
        {
            var report = await BuildReportAsync(cancellationToken);
            report.Seeded = false;
            report.Message = "Seed data already present.";
            return report;
        }

        private void LogReport(SeedReport report)
        {
            _logger.LogInformation("Recommendation seed completed.");
            _logger.LogInformation("Students: {StudentCount}, Instructors: {InstructorCount}, Admins: {AdminCount}, Organizations: {OrganizationCount}",
                report.StudentCount, report.InstructorCount, report.AdminCount, report.OrganizationCount);
            _logger.LogInformation("Courses: {CourseCount} (Tech: {TechCount}, Non-Tech: {NonTechCount})",
                report.CourseCount, report.TechnologyCourseCount, report.NonTechnologyCourseCount);
            _logger.LogInformation("Enrollments: {EnrollmentCount}, Completed: {CompletedEnrollmentCount}, Ratings: {RatingCount}",
                report.EnrollmentCount, report.CompletedEnrollmentCount, report.RatingCount);
        }

        private static string Slugify(string value)
        {
            return new string(value
                .ToLowerInvariant()
                .Where(character => char.IsLetterOrDigit(character) || character == ' ')
                .ToArray())
                .Replace(' ', '-');
        }
    }

    public sealed class SeedReport
    {
        public bool Seeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int InstructorCount { get; set; }
        public int AdminCount { get; set; }
        public int OrganizationCount { get; set; }
        public int CourseCount { get; set; }
        public int TechnologyCourseCount { get; set; }
        public int NonTechnologyCourseCount { get; set; }
        public int EnrollmentCount { get; set; }
        public int CompletedEnrollmentCount { get; set; }
        public int RatingCount { get; set; }
        public string SeedPassword { get; set; } = string.Empty;
        public List<string> SampleStudentEmails { get; set; } = [];
        public List<string> SampleCourses { get; set; } = [];
        public Dictionary<string, int> StudentInterestDistribution { get; set; } = new();

        public static SeedReport Disabled() => new() { Message = "Seeding disabled." };
    }
}
