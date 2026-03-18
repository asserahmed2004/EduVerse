using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Rating
{
    public class CreateRating
    {
        
        public Guid CourseId { get; set; }
        public float RatingValue { get; set; }
    }
}
