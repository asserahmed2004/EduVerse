using Bogus;
using Domain.Entities;
using InfraStructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

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
                _logger.LogInformation("Data seeding is disabled via configuration (DataSeeding:Enabled = false).");
                return SeedReport.Disabled("Data seeding is disabled.");
            }

            if (!_options.RunOnStartup)
            {
                _logger.LogInformation("Data seeding is enabled but RunOnStartup is false. Skipping startup seed.");
                return SeedReport.Disabled("RunOnStartup is false.");
            }

            if (string.IsNullOrWhiteSpace(_options.SeedPassword))
            {
                const string message = "Data seeding is enabled but SeedPassword is empty. Demo users cannot be created safely.";
                _logger.LogError(message);
                throw new InvalidOperationException(message);
            }

            var demoStatus = await GetDemoSeedStatusAsync(cancellationToken);
            LogStartupContext(demoStatus);

            if (demoStatus.IsFullySeeded)
            {
                _logger.LogInformation(
                    "Existing demo seed markers found (marker user, demo users, and {DemoCourseCount} [SEED] courses). Skipping duplicate demo seeding.",
                    demoStatus.DemoCourseCount);

                var existingReport = await BuildReportAsync(cancellationToken);
                existingReport.Seeded = false;
                existingReport.Message = "Demo seed data already present. Skipped.";
                LogReport(existingReport);
                return existingReport;
            }

            if (demoStatus.HasPartialDemoData)
            {
                _logger.LogWarning(
                    "Partial demo seed data detected (demo users: {DemoUserCount}, [SEED] courses: {DemoCourseCount}, marker: {MarkerExists}). Completing missing demo records only.",
                    demoStatus.DemoUserCount,
                    demoStatus.DemoCourseCount,
                    demoStatus.MarkerExists);
            }
            else if (demoStatus.TotalUsers > 0 || demoStatus.TotalCourses > 0 || demoStatus.TotalCategories > 0)
            {
                _logger.LogInformation(
                    "Existing normal project data detected (users: {TotalUsers}, courses: {TotalCourses}, categories: {TotalCategories}). This does NOT block demo seeding.",
                    demoStatus.TotalUsers,
                    demoStatus.TotalCourses,
                    demoStatus.TotalCategories);
            }

            _logger.LogInformation("RecommendationDataSeeder started.");

            var counters = new SeedCreationCounters();

            await EnsureRolesAsync();
            var categories = await SeedCategoriesAsync(counters, cancellationToken);
            var organizations = await SeedOrganizationsAsync(counters, cancellationToken);
            var admins = await SeedAdminsAsync(counters, cancellationToken);
            var instructors = await SeedInstructorsAsync(organizations, counters, cancellationToken);
            var students = await SeedStudentsAsync(counters, cancellationToken);
            var courses = await SeedCoursesAsync(categories, organizations, admins, instructors, counters, cancellationToken);
            var enrollments = await SeedEnrollmentsAsync(students, courses, counters, cancellationToken);
            await EnsureOrganizationAdminDemoLinkAsync(counters, cancellationToken);
            var ratingsCreated = await SeedRatingsAsync(counters, cancellationToken);
            await EnsureMarkerUserAsync(counters, cancellationToken);

            var report = await BuildReportAsync(cancellationToken);
            report.Seeded = true;
            report.Message = demoStatus.HasPartialDemoData
                ? "Partial demo seed completed. Missing demo records were added."
                : "Demo seed completed successfully.";
            report.UsersCreated = counters.UsersCreated;
            report.OrganizationsCreated = counters.OrganizationsCreated;
            report.CategoriesCreated = counters.CategoriesCreated;
            report.CoursesCreated = counters.CoursesCreated;
            report.EnrollmentsCreated = counters.EnrollmentsCreated;
            report.RatingsCreated = counters.RatingsCreated;

            _logger.LogInformation(
                "Seed completed successfully. Created: users={UsersCreated}, organizations={OrganizationsCreated}, categories={CategoriesCreated}, courses={CoursesCreated}, enrollments={EnrollmentsCreated}, ratings={RatingsCreated}, total demo ratings now={RatingCount}",
                counters.UsersCreated,
                counters.OrganizationsCreated,
                counters.CategoriesCreated,
                counters.CoursesCreated,
                counters.EnrollmentsCreated,
                ratingsCreated,
                report.RatingCount);

            LogReport(report);
            return report;
        }

        private void LogStartupContext(SeedDemoStatus demoStatus)
        {
            var connectionString = _context.Database.GetConnectionString();
            _logger.LogInformation("RecommendationDataSeeder using database: {Database}", _context.Database.GetDbConnection().Database);
            _logger.LogInformation("Connection string (masked): {ConnectionString}", MaskConnectionString(connectionString));
            _logger.LogInformation(
                "Database snapshot before seed decision -> total users: {TotalUsers}, total courses: {TotalCourses}, total categories: {TotalCategories}, demo users: {DemoUserCount}, demo courses: {DemoCourseCount}",
                demoStatus.TotalUsers,
                demoStatus.TotalCourses,
                demoStatus.TotalCategories,
                demoStatus.DemoUserCount,
                demoStatus.DemoCourseCount);
        }

        private async Task<SeedDemoStatus> GetDemoSeedStatusAsync(CancellationToken cancellationToken)
        {
            var demoEmailSuffix = $"@{SeedCatalog.EmailDomain}";

            return new SeedDemoStatus
            {
                TotalUsers = await _context.Users.CountAsync(cancellationToken),
                TotalCourses = await _context.Courses.CountAsync(cancellationToken),
                TotalCategories = await _context.Categories.CountAsync(cancellationToken),
                DemoUserCount = await _context.Users.CountAsync(
                    user => user.Email != null && user.Email.EndsWith(demoEmailSuffix),
                    cancellationToken),
                DemoCourseCount = await _context.Courses.CountAsync(
                    course => course.Name.StartsWith("[SEED]"),
                    cancellationToken),
                MarkerExists = await _context.Users.AnyAsync(
                    user => user.Email == SeedCatalog.MarkerEmail,
                    cancellationToken),
                PrimaryDemoStudentExists = await _context.Users.AnyAsync(
                    user => user.Email == $"student.001@{SeedCatalog.EmailDomain}",
                    cancellationToken)
            };
        }

        private async Task EnsureRolesAsync()
        {
            foreach (var role in SeedCatalog.Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                    {
                        _logger.LogError(
                            "Failed to ensure role '{Role}': {Errors}",
                            role,
                            string.Join(", ", result.Errors.Select(error => error.Description)));
                    }
                    else
                    {
                        _logger.LogInformation("Created missing role '{Role}'.", role);
                    }
                }
            }
        }

        private async Task<Dictionary<string, Category>> SeedCategoriesAsync(
            SeedCreationCounters counters,
            CancellationToken cancellationToken)
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
                    counters.CategoriesCreated++;
                }

                map[name] = category;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return map;
        }

        private async Task<List<Organization>> SeedOrganizationsAsync(
            SeedCreationCounters counters,
            CancellationToken cancellationToken)
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
                    counters.OrganizationsCreated++;
                }

                organizations.Add(organization);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return organizations;
        }

        private async Task<List<AppUser>> SeedAdminsAsync(
            SeedCreationCounters counters,
            CancellationToken cancellationToken)
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
                    counters,
                    cancellationToken);
                admins.Add(user);
            }

            return admins;
        }

        private async Task<List<AppUser>> SeedInstructorsAsync(
            IReadOnlyList<Organization> organizations,
            SeedCreationCounters counters,
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
                    counters,
                    cancellationToken);
                instructors.Add(user);
            }

            return instructors;
        }

        private async Task<List<AppUser>> SeedStudentsAsync(
            SeedCreationCounters counters,
            CancellationToken cancellationToken)
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
                    counters,
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
            SeedCreationCounters counters,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
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
                    var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                    _logger.LogError("Identity error creating demo user '{Email}': {Errors}", email, errors);
                    throw new InvalidOperationException($"Failed to create seed user '{email}': {errors}");
                }

                counters.UsersCreated++;
                _logger.LogInformation("Created demo user '{Email}' with role '{Role}'.", email, role);
            }
            else if (organizationId.HasValue && user.OrganizationId != organizationId)
            {
                user.OrganizationId = organizationId;
                await _userManager.UpdateAsync(user);
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(error => error.Description));
                    _logger.LogError("Identity error assigning role '{Role}' to '{Email}': {Errors}", role, email, errors);
                    throw new InvalidOperationException($"Failed to assign role '{role}' to '{email}': {errors}");
                }
            }

            return user;
        }

        private async Task<List<Course>> SeedCoursesAsync(
            IReadOnlyDictionary<string, Category> categories,
            IReadOnlyList<Organization> organizations,
            IReadOnlyList<AppUser> admins,
            IReadOnlyList<AppUser> instructors,
            SeedCreationCounters counters,
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
                    var organizationInstructors = instructors
                        .Where(item => item.OrganizationId == organization.Id)
                        .ToList();
                    if (organizationInstructors.Count == 0)
                    {
                        throw new InvalidOperationException($"No seeded instructor belongs to organization '{organization.Name}'.");
                    }
                    var instructor = organizationInstructors[_faker.Random.Int(0, organizationInstructors.Count - 1)];

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
                    counters.CoursesCreated++;
                }

                var category = categories[template.Category];
                var categoryLinkExists = await _context.CourseCategories.AnyAsync(
                    link => link.CourseId == course.Id && link.CategoryId == category.Id,
                    cancellationToken);

                if (!categoryLinkExists)
                {
                    _context.CourseCategories.Add(new CourseCategory
                    {
                        CourseId = course.Id,
                        CategoryId = category.Id
                    });
                    counters.CourseCategoriesCreated++;
                }

                courses.Add(course);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return courses;
        }

        private async Task<List<Enrollment>> SeedEnrollmentsAsync(
            IReadOnlyList<AppUser> students,
            IReadOnlyList<Course> courses,
            SeedCreationCounters counters,
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
                    counters.EnrollmentsCreated++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (enrollments.Count < SeedCatalog.MinEnrollmentCount)
            {
                enrollments.AddRange(await TopUpEnrollmentsAsync(students, courses, enrollments, counters, cancellationToken));
            }

            return enrollments;
        }

        private async Task EnsureOrganizationAdminDemoLinkAsync(
            SeedCreationCounters counters,
            CancellationToken cancellationToken)
        {
            var organizationAdmin = await _userManager.FindByEmailAsync(SeedCatalog.OrganizationAdminEmail);
            if (organizationAdmin == null)
            {
                _logger.LogInformation(
                    "Organization admin demo link skipped because '{Email}' does not exist.",
                    SeedCatalog.OrganizationAdminEmail);
                return;
            }

            var instructor = await _userManager.FindByEmailAsync($"instructor.001@{SeedCatalog.EmailDomain}");
            var student = await _userManager.FindByEmailAsync($"student.001@{SeedCatalog.EmailDomain}");
            if (instructor == null || student == null)
            {
                _logger.LogWarning(
                    "Organization admin demo link skipped because the primary instructor or student is missing.");
                return;
            }

            var targetCourse = await _context.Courses
                .Where(course =>
                    !course.IsDeleted &&
                    course.InstructorId == instructor.Id &&
                    course.OrganizationId.HasValue)
                .OrderBy(course => course.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (targetCourse?.OrganizationId is not Guid organizationId)
            {
                _logger.LogWarning(
                    "Organization admin demo link skipped because instructor '{Email}' has no organization course.",
                    instructor.Email);
                return;
            }

            var changedUsers = false;

            if (organizationAdmin.OrganizationId != organizationId)
            {
                organizationAdmin.OrganizationId = organizationId;
                changedUsers = true;
            }

            if (instructor.OrganizationId != organizationId)
            {
                instructor.OrganizationId = organizationId;
                changedUsers = true;
            }

            if (changedUsers)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (!await _userManager.IsInRoleAsync(organizationAdmin, "organizationAdmin"))
            {
                var roleResult = await _userManager.AddToRoleAsync(organizationAdmin, "organizationAdmin");
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException(
                        $"Failed to assign organizationAdmin role to '{SeedCatalog.OrganizationAdminEmail}': {errors}");
                }
            }

            var enrollmentExists = await _context.Enrollments.AnyAsync(
                enrollment => enrollment.CourseId == targetCourse.Id && enrollment.StudentId == student.Id,
                cancellationToken);

            if (!enrollmentExists)
            {
                _context.Enrollments.Add(new Enrollment
                {
                    CourseId = targetCourse.Id,
                    StudentId = student.Id,
                    EnrollmentDate = DateTime.UtcNow,
                    Progression = 0,
                    ProgressPercentage = 0,
                    IsCompleted = false
                });
                counters.EnrollmentsCreated++;
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Linked organization admin '{OrganizationAdminEmail}' to organization {OrganizationId}; instructor '{InstructorEmail}' teaches course {CourseId}, and student '{StudentEmail}' is enrolled.",
                organizationAdmin.Email,
                organizationId,
                instructor.Email,
                targetCourse.Id,
                student.Email);
        }

        private async Task<List<Enrollment>> TopUpEnrollmentsAsync(
    IReadOnlyList<AppUser> students,
    IReadOnlyList<Course> courses,
    IReadOnlyList<Enrollment> currentEnrollments,
    SeedCreationCounters counters,
    CancellationToken cancellationToken)
        {
            var added = new List<Enrollment>();

            var existingPairsFromDb = await _context.Enrollments
                .AsNoTracking()
                .Select(e => new { e.StudentId, e.CourseId })
                .ToListAsync(cancellationToken);

            var existingPairs = existingPairsFromDb
                .Select(e => (e.StudentId, e.CourseId))
                .ToHashSet();

            foreach (var student in students)
            {
                var studentEnrollmentCount = existingPairs
                    .Count(pair => pair.StudentId == student.Id);

                var attempts = 0;

                while (studentEnrollmentCount < 8
                       && existingPairs.Count < SeedCatalog.MinEnrollmentCount + 200
                       && attempts < courses.Count * 2)
                {
                    attempts++;

                    var course = courses[_faker.Random.Int(0, courses.Count - 1)];

                    if (!existingPairs.Add((student.Id, course.Id)))
                        continue;

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
                    counters.EnrollmentsCreated++;
                    studentEnrollmentCount++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return added;
        }

        private async Task<int> SeedRatingsAsync(
            SeedCreationCounters counters,
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
                counters.RatingsCreated++;
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
                    counters.RatingsCreated++;
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

        private async Task EnsureMarkerUserAsync(
            SeedCreationCounters counters,
            CancellationToken cancellationToken)
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
                var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                _logger.LogError("Identity error creating marker user: {Errors}", errors);
                throw new InvalidOperationException($"Failed to create seed marker user: {errors}");
            }

            await _userManager.AddToRoleAsync(marker, "admin");
            counters.UsersCreated++;
            _logger.LogInformation("Created demo marker user '{Email}'.", SeedCatalog.MarkerEmail);
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
                StudentCount = seededStudentIds.Count,
                InstructorCount = await _context.Users.CountAsync(
                    user => user.Email!.StartsWith("instructor.") && user.Email.EndsWith($"@{SeedCatalog.EmailDomain}"),
                    cancellationToken),
                AdminCount = await _context.Users.CountAsync(
                    user => user.Email!.StartsWith("admin.") && user.Email.EndsWith($"@{SeedCatalog.EmailDomain}"),
                    cancellationToken),
                OrganizationCount = await _context.Organizations.CountAsync(
                    organization => organization.Email != null && organization.Email.EndsWith($"@{SeedCatalog.EmailDomain}"),
                    cancellationToken),
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

        private void LogReport(SeedReport report)
        {
            _logger.LogInformation("Recommendation seed summary: {Message}", report.Message);
            _logger.LogInformation("Demo users -> students: {StudentCount}, instructors: {InstructorCount}, admins: {AdminCount}, organizations: {OrganizationCount}",
                report.StudentCount, report.InstructorCount, report.AdminCount, report.OrganizationCount);
            _logger.LogInformation("Demo courses: {CourseCount} (catalog tech: {TechCount}, non-tech: {NonTechCount})",
                report.CourseCount, report.TechnologyCourseCount, report.NonTechnologyCourseCount);
            _logger.LogInformation("Demo enrollments: {EnrollmentCount}, completed: {CompletedEnrollmentCount}, demo ratings: {RatingCount}",
                report.EnrollmentCount, report.CompletedEnrollmentCount, report.RatingCount);
        }

        private static string MaskConnectionString(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return "(not configured)";
            }

            var masked = Regex.Replace(connectionString, "Password=[^;]+", "Password=***", RegexOptions.IgnoreCase);
            masked = Regex.Replace(masked, "User ID=[^;]+", "User ID=***", RegexOptions.IgnoreCase);
            return masked;
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
        public int UsersCreated { get; set; }
        public int OrganizationsCreated { get; set; }
        public int CategoriesCreated { get; set; }
        public int CoursesCreated { get; set; }
        public int EnrollmentsCreated { get; set; }
        public int RatingsCreated { get; set; }
        public string SeedPassword { get; set; } = string.Empty;
        public List<string> SampleStudentEmails { get; set; } = [];
        public List<string> SampleCourses { get; set; } = [];
        public Dictionary<string, int> StudentInterestDistribution { get; set; } = new();

        public static SeedReport Disabled(string message) => new() { Message = message };
    }
}
