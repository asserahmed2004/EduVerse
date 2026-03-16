using Application.DTOs.Cloud;
using Application.DTOs.Course;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implementitions
{
    public  class CourseService(IGeneric<Course> CoursesManagment ,
        IGeneric<CourseCategory> CoursesCatManagment,IGeneric<Category> CategoryManagment,
        IMapper mapper ,ICloudService cloud ) : ICourseService
    {
        public  async Task<ServiceResponse> CreateCourse(CreateCourse Course)
        {
            if (Course == null)
                return new ServiceResponse { success = false, message = "Course data is null" };
            var mapping = mapper.Map<Course>(Course);

            mapping.ImageUrl = $"{mapping.Id}-Thumbnail{Path.GetExtension(Course.Image.FileName)}";
            mapping.OrgId = "00000000-0000-0000-0000-000000000001";
            var details =new FileDetails { FileName = mapping.ImageUrl, Folder = "courses" };
            var AddCloudFile = new AddCloudFile { Details = details, File = Course.Image};
            var uploadResult = await cloud.UploadFileAsync(AddCloudFile);
            if (!uploadResult.success)
                return new ServiceResponse { success = false, message = "Failed to upload course image" };
            var result = await CoursesManagment.AddAsync(mapping);
            if (result == null)
                return new ServiceResponse { success = false, message = "Failed to create Course" };
            
            var categoryIds = Course.Categories.Split(',').ToList();
            foreach (var category in categoryIds)
            {
                Guid categoryId;
                if (!Guid.TryParse(category, out categoryId))
                    continue;
                var test=await CategoryManagment.GetByIdAsync(categoryId);
                if(test == null)
                    continue;

                var courseCategory = new CourseCategory { CourseId =result.Id , CategoryId = test.Id };
                await CoursesCatManagment.AddAsync(courseCategory);
            }
            return new ServiceResponse { success = true, message = "Course created successfully" };


        }

        public async Task<ServiceResponse> DeleteCourse(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<GetCourse>> GetAllCourses()
        {
            throw new NotImplementedException();
        }

        public async Task<GetCourse> GetCourseById(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<GetCourse> GetCourseByName(string name)
        {
            throw new NotImplementedException();
        }

        public async Task<ServiceResponse> UpdateCourse(UpdateCourse Course)
        {
            throw new NotImplementedException();
        }
    }
}
