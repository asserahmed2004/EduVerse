using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Helpers
{
    public static class CourseSimilarityCalculator
    {
        public const double CategoryWeight = 0.4;
        public const double TagWeight = 0.5;
        public const double LevelWeight = 0.1;

        public static double CalculateContentScore(
            IEnumerable<Guid> sourceCategoryIds,
            IEnumerable<string> sourceTags,
            string? sourceLevel,
            HashSet<Guid> targetCategoryIds,
            HashSet<string> targetTags,
            string? targetLevel)
        {
            var sourceCategories = sourceCategoryIds.ToHashSet();
            var categoryScore = CalculateWeightedCategoryScore(sourceCategoryIds, targetCategoryIds);
            var tagScore = CalculateWeightedTagScore(sourceTags, targetTags);
            var levelScore = CalculateLevelScore(sourceLevel, targetLevel);

            return (categoryScore * CategoryWeight)
                   + (tagScore * TagWeight)
                   + (levelScore * LevelWeight);
        }

        private static double CalculateWeightedCategoryScore(IEnumerable<Guid> sourceCategoryIds, HashSet<Guid> targetCategoryIds)
        {
            var sourceList = sourceCategoryIds.ToList();
            if (sourceList.Count == 0 && targetCategoryIds.Count == 0)
            {
                return 0;
            }

            var jaccardScore = JaccardSimilarity(sourceList.ToHashSet(), targetCategoryIds);
            var overlapScore = sourceList.Count == 0
                ? 0
                : (double)sourceList.Count(categoryId => targetCategoryIds.Contains(categoryId)) / sourceList.Count;

            return (jaccardScore + overlapScore) / 2;
        }

        private static double CalculateWeightedTagScore(IEnumerable<string> sourceTags, HashSet<string> targetTags)
        {
            var sourceList = sourceTags.ToList();
            if (sourceList.Count == 0 && targetTags.Count == 0)
            {
                return 0;
            }

            var jaccardScore = JaccardSimilarity(
                sourceList.ToHashSet(StringComparer.OrdinalIgnoreCase),
                targetTags);

            var overlapScore = sourceList.Count == 0
                ? 0
                : (double)sourceList.Count(tag => targetTags.Contains(tag)) / sourceList.Count;

            return (jaccardScore + overlapScore) / 2;
        }

        public static double CalculateTrendingScore(
            int enrollmentCount,
            float averageRating,
            int ratingCount,
            int maxEnrollmentCount,
            int maxRatingCount)
        {
            var enrollmentNorm = maxEnrollmentCount > 0
                ? (double)enrollmentCount / maxEnrollmentCount
                : 0;

            var ratingNorm = averageRating / 5f;

            var ratingCountNorm = maxRatingCount > 0
                ? (double)ratingCount / maxRatingCount
                : 0;

            return (enrollmentNorm * 0.5)
                   + (ratingNorm * ratingCountNorm * 0.3)
                   + (ratingCountNorm * 0.2);
        }

        public static HashSet<string> ExtractTags(string? tags, string? title)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(tags))
            {
                foreach (var tag in tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var normalized = NormalizeToken(tag);
                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        result.Add(normalized);
                    }
                }
            }

            foreach (var token in ExtractTitleTokens(title))
            {
                result.Add(token);
            }

            return result;
        }

        public static IEnumerable<string> ExtractTitleTokens(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return Array.Empty<string>();
            }

            return title
                .Split([' ', '-', '_', ':', ',', '.', '(', ')'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeToken)
                .Where(token => token.Length >= 3);
        }

        public static double CalculateLevelScore(string? sourceLevel, string? targetLevel)
        {
            var normalizedSource = NormalizeLevel(sourceLevel);
            var normalizedTarget = NormalizeLevel(targetLevel);

            if (string.IsNullOrEmpty(normalizedSource) || string.IsNullOrEmpty(normalizedTarget))
            {
                return 0.5;
            }

            if (normalizedSource == normalizedTarget)
            {
                return 1;
            }

            if (IsAllLevels(normalizedSource) || IsAllLevels(normalizedTarget))
            {
                return 0.75;
            }

            return 0;
        }

        private static double JaccardSimilarity<T>(HashSet<T> left, HashSet<T> right)
        {
            if (left.Count == 0 && right.Count == 0)
            {
                return 0;
            }

            var intersection = left.Intersect(right).Count();
            var union = left.Union(right).Count();
            return union == 0 ? 0 : (double)intersection / union;
        }

        private static string NormalizeToken(string value)
        {
            return value.Trim().ToLowerInvariant();
        }

        private static string NormalizeLevel(string? level)
        {
            return string.IsNullOrWhiteSpace(level)
                ? string.Empty
                : level.Trim().ToLowerInvariant();
        }

        private static bool IsAllLevels(string level)
        {
            return level.Contains("all level", StringComparison.OrdinalIgnoreCase)
                   || level == "all"
                   || level == "any";
        }
    }
}
