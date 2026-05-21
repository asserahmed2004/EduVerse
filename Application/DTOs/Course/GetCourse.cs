using Application.DTOs.Category;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Course
{
    public class GetCourse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public double Price { get; set; }
        public double Duration { get; set; }
        public float Rating { get; set; }
        public float UserRating { get; set; }
        public string OrgId { get; set; }
        public string ImageUrl { get; set; }
        public List<GetCategory> Categories { get; set; }



    }
}
