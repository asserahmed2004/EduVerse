using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Sessions
{
    public class GetSession
    {
        public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public string FileUrl { get; set; }
        public string TrainerId { get; set; }
        public DateTime Date { get; set; }
        public double Duration { get; set; }
        public int SessionNumber { get; set; }
        public string? Description { get; set; }
        public string? VideoUrl { get; set; }
        public string? ExternalLink { get; set; }
        public bool IsCompleted { get; set; }
        public string? AttendanceCode { get; set; }
        public DateTime? AttendanceCodeCreatedAt { get; set; }
    }
}
