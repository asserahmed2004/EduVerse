using Application.DTOs.Category;
using Application.DTOs.Recommendation;
using Application.DTOs.Responses;
using Application.Helpers;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Implementitions
{
    public class RecommendationService(
        IGeneric<Course> courseRepository,
        IGeneric<CourseCategory> courseCategoryRepository,
        IGeneric<Category> categoryRepository,
        IGeneric<Enrollment> enrollmentRepository,
        IGeneric<Rating> ratingRepository,
        IMapper mapper) : IRecommendationService
    {
        private const int RecommendationLimit = 5;

        public async Task<ServiceResponse> GetPersonalizedRecommendationsAsync(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return new ServiceResponse(false, "Student id is required.");
            }

            var context = await BuildContextAsync();
            var enrolledCourseIds = context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId)
                .ToHashSet();

            if (enrolledCourseIds.Count == 0)
            {
                return new ServiceResponse(
                    true,
                    "No enrolled courses found. Showing trending courses instead.",
                    await BuildTrendingRecommendationsAsync(context));
            }

            var profile = BuildStudentProfile(studentId, context, enrolledCourseIds);
            var recommendations = context.ActiveCourses
                .Where(course => !enrolledCourseIds.Contains(course.Id))
                .Select(course => CreateRecommendation(
                    course,
                    context,
                    CourseSimilarityCalculator.CalculateContentScore(
                        profile.CategoryIds,
                        profile.Tags,
                        profile.Level,
                        context.GetCategoryIds(course.Id),
                        context.GetTags(course),
                        course.Level)))
                .Where(recommendation => recommendation.RecommendationScore > 0)
                .OrderByDescending(recommendation => recommendation.RecommendationScore)
                .ThenByDescending(recommendation => recommendation.Rating)
                .Take(RecommendationLimit)
                .ToList();

            if (recommendations.Count == 0)
            {
                return new ServiceResponse(
                    true,
                    "No personalized matches found. Showing trending courses instead.",
                    await BuildTrendingRecommendationsAsync(context));
            }

            return new ServiceResponse(true, "Personalized recommendations retrieved successfully.", recommendations);
        }

        public async Task<ServiceResponse> GetSimilarCoursesAsync(Guid courseId)
        {
            if (courseId == Guid.Empty)
            {
                return new ServiceResponse(false, "Invalid course id.");
            }

            var context = await BuildContextAsync();
            var sourceCourse = context.ActiveCourses.FirstOrDefault(course => course.Id == courseId);
            if (sourceCourse == null)
            {
                return new ServiceResponse(false, "Course not found.");
            }

            var sourceCategoryIds = context.GetCategoryIds(sourceCourse.Id);
            var sourceTags = context.GetTags(sourceCourse);

            var recommendations = context.ActiveCourses
                .Where(course => course.Id != courseId)
                .Select(course => CreateRecommendation(
                    course,
                    context,
                    CourseSimilarityCalculator.CalculateContentScore(
                        sourceCategoryIds,
                        sourceTags,
                        sourceCourse.Level,
                        context.GetCategoryIds(course.Id),
                        context.GetTags(course),
                        course.Level)))
                .OrderByDescending(recommendation => recommendation.RecommendationScore)
                .ThenByDescending(recommendation => recommendation.Rating)
                .Take(RecommendationLimit)
                .ToList();

            return new ServiceResponse(true, "Similar courses retrieved successfully.", recommendations);
        }

        public async Task<ServiceResponse> GetTrendingCoursesAsync()
        {
            var context = await BuildContextAsync();
            var recommendations = await BuildTrendingRecommendationsAsync(context);
            return new ServiceResponse(true, "Trending courses retrieved successfully.", recommendations);
        }

        private async Task<List<RecommendedCourseDto>> BuildTrendingRecommendationsAsync(RecommendationContext context)
        {
            var maxEnrollmentCount = context.ActiveCourses
                .Select(course => context.GetEnrollmentCount(course.Id))
                .DefaultIfEmpty(0)
                .Max();

            var maxRatingCount = context.ActiveCourses
                .Select(course => context.GetRatingCount(course.Id))
                .DefaultIfEmpty(0)
                .Max();

            return context.ActiveCourses
                .Select(course =>
                {
                    var ratingSummary = context.GetRatingSummary(course.Id);
                    var score = CourseSimilarityCalculator.CalculateTrendingScore(
                        context.GetEnrollmentCount(course.Id),
                        ratingSummary.AverageRating,
                        ratingSummary.RatingCount,
                        maxEnrollmentCount,
                        maxRatingCount);

                    return CreateRecommendation(course, context, score);
                })
                .OrderByDescending(recommendation => recommendation.RecommendationScore)
                .ThenByDescending(recommendation => recommendation.StudentsCount)
                .Take(RecommendationLimit)
                .ToList();
        }

        private static StudentProfile BuildStudentProfile(
            string studentId,
            RecommendationContext context,
            HashSet<Guid> enrolledCourseIds)
        {
            var profile = new StudentProfile();
            var studentRatings = context.Ratings
                .Where(rating => rating.StudentId == studentId)
                .ToDictionary(rating => rating.CourseId, rating => rating.RatingValue);

            foreach (var enrolledCourseId in enrolledCourseIds)
            {
                var course = context.ActiveCourses.FirstOrDefault(item => item.Id == enrolledCourseId);
                if (course == null)
                {
                    continue;
                }

                var enrollment = context.Enrollments.FirstOrDefault(item =>
                    item.StudentId == studentId && item.CourseId == enrolledCourseId);

                var weight = 1;
                if (enrollment?.IsCompleted == true)
                {
                    weight += 1;
                }

                if (studentRatings.TryGetValue(enrolledCourseId, out var userRating) && userRating >= 4)
                {
                    weight += 1;
                }

                for (var i = 0; i < weight; i++)
                {
                    profile.CategoryIds.AddRange(context.GetCategoryIds(course.Id));
                }

                foreach (var tag in context.GetTags(course))
                {
                    for (var i = 0; i < weight; i++)
                    {
                        profile.Tags.Add(tag);
                    }
                }

                if (!string.IsNullOrWhiteSpace(course.Level))
                {
                    profile.Levels.Add(course.Level);
                }
            }

            profile.Level = profile.Levels
                .GroupBy(level => level, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .FirstOrDefault();

            return profile;
        }

        private RecommendedCourseDto CreateRecommendation(
            Course course,
            RecommendationContext context,
            double score)
        {
            var ratingSummary = context.GetRatingSummary(course.Id);
            var categories = context.GetCategories(course.Id);

            return new RecommendedCourseDto
            {
                Id = course.Id,
                Name = course.Name,
                Title = course.Title,
                Description = course.Description,
                Price = course.Price,
                ImageUrl = course.ImageUrl,
                Tags = course.Tags,
                Level = course.Level,
                Rating = ratingSummary.AverageRating,
                RatingCount = ratingSummary.RatingCount,
                StudentsCount = context.GetEnrollmentCount(course.Id),
                Category = categories.FirstOrDefault()?.Name,
                Categories = categories,
                RecommendationScore = Math.Round(score, 4)
            };
        }

        private async Task<RecommendationContext> BuildContextAsync()
        {
            var courses = (await courseRepository.GetAllAsync())
                .Where(course => !course.IsDeleted)
                .ToList();

            var courseCategories = (await courseCategoryRepository.GetAllAsync()).ToList();
            var categories = (await categoryRepository.GetAllAsync()).ToDictionary(category => category.Id);
            var enrollments = (await enrollmentRepository.GetAllAsync()).ToList();
            var ratings = (await ratingRepository.GetAllAsync()).ToList();

            return new RecommendationContext(courses, courseCategories, categories, enrollments, ratings, mapper);
        }

        private sealed class StudentProfile
        {
            public List<Guid> CategoryIds { get; } = new();
            public List<string> Tags { get; } = new();
            public HashSet<string> Levels { get; } = new(StringComparer.OrdinalIgnoreCase);
            public string? Level { get; set; }
        }

        private sealed class RecommendationContext
        {
            public RecommendationContext(
                List<Course> courses,
                List<CourseCategory> courseCategories,
                Dictionary<Guid, Category> categories,
                List<Enrollment> enrollments,
                List<Rating> ratings,
                IMapper mapper)
            {
                ActiveCourses = courses;
                CourseCategories = courseCategories;
                Categories = categories;
                Enrollments = enrollments;
                Ratings = ratings;
                Mapper = mapper;
            }

            public List<Course> ActiveCourses { get; }
            public List<CourseCategory> CourseCategories { get; }
            public Dictionary<Guid, Category> Categories { get; }
            public List<Enrollment> Enrollments { get; }
            public List<Rating> Ratings { get; }
            public IMapper Mapper { get; }

            public HashSet<Guid> GetCategoryIds(Guid courseId)
            {
                return CourseCategories
                    .Where(link => link.CourseId == courseId)
                    .Select(link => link.CategoryId)
                    .ToHashSet();
            }

            public List<GetCategory> GetCategories(Guid courseId)
            {
                return CourseCategories
                    .Where(link => link.CourseId == courseId)
                    .Select(link => Categories.GetValueOrDefault(link.CategoryId))
                    .Where(category => category != null)
                    .Select(category => Mapper.Map<GetCategory>(category!))
                    .ToList();
            }

            public HashSet<string> GetTags(Course course)
            {
                return CourseSimilarityCalculator.ExtractTags(course.Tags, course.Title);
            }

            public int GetEnrollmentCount(Guid courseId)
            {
                return Enrollments
                    .Where(enrollment => enrollment.CourseId == courseId)
                    .Select(enrollment => enrollment.StudentId)
                    .Distinct()
                    .Count();
            }

            public int GetRatingCount(Guid courseId)
            {
                return Ratings.Count(rating => rating.CourseId == courseId);
            }

            public (float AverageRating, int RatingCount) GetRatingSummary(Guid courseId)
            {
                var courseRatings = Ratings.Where(rating => rating.CourseId == courseId).ToList();
                if (courseRatings.Count == 0)
                {
                    return (0, 0);
                }

                return ((float)courseRatings.Average(rating => rating.RatingValue), courseRatings.Count);
            }
        }
    }
}
