using Application.DTOs.Category;
using System;
using System.Collections.Generic;

namespace Application.DTOs.Recommendation
{
    public class RecommendedCourseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string ImageUrl { get; set; }
        public string? Tags { get; set; }
        public string? Level { get; set; }
        public float Rating { get; set; }
        public int RatingCount { get; set; }
        public int StudentsCount { get; set; }
        public string? Category { get; set; }
        public List<GetCategory> Categories { get; set; } = new();
        public double RecommendationScore { get; set; }
    }
}
