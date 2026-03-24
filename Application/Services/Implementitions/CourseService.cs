using Application.DTOs.Assignment;
using Application.DTOs.Category;
using Application.DTOs.Cloud;
using Application.DTOs.Course;
using Application.DTOs.Rating;
using Application.DTOs.Responses;
using Application.DTOs.Sessions;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediaToolkit;
using MediaToolkit.Model;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implementitions
{
    public  class CourseService(IGeneric<Course> CoursesManagment ,
        IGeneric<CourseCategory> CoursesCatManagment,IGeneric<Category> CategoryManagment,
        IMapper mapper ,ICloudService cloud,IGeneric<Rating> RatingManagment ,IGeneric<Session> SessionManagment,IGeneric<Assignment> AssignmentManagment) : ICourseService
    {
        public async Task<ServiceResponse> AddRating(CreateRating rating, string userid)
        {
            if(rating == null || rating.CourseId == Guid.Empty || string.IsNullOrEmpty(userid) || rating.RatingValue < 0 || rating.RatingValue > 5)
                return new ServiceResponse { success = false, message = "Invalid rating data" };
            var mappedRating = mapper.Map<Rating>(rating);
            mappedRating.StudentId = userid;
            var existingRating = await RatingManagment.GetAllAsync();
            var userCourseRating = existingRating.FirstOrDefault(r => r.CourseId == rating.CourseId && r.StudentId == userid);
            if (userCourseRating != null)
                await RatingManagment.DeleteAsync(userCourseRating);
            var result = await RatingManagment.AddAsync(mappedRating);
            if (result == null)
                return new ServiceResponse { success = false, message = "Failed to add rating" };
            return new ServiceResponse { success = true, message = "Rating added successfully" };
        }

        

        public  async Task<ServiceResponse> CreateCourse(CreateCourse Course)
        {
            if (Course == null)
                return new ServiceResponse { success = false, message = "Course data is null" };
            var mapping = mapper.Map<Course>(Course);
            mapping.Duration = 0;
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
            if (id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid Course ID" };
            var course = await CoursesManagment.GetByIdAsync(id);
            if (course == null)
                return new ServiceResponse { success = false, message = "Course not found" };
            var result = await CoursesManagment.DeleteAsync(course);
            if (result == 0)
                return new ServiceResponse { success = false, message = "Failed to delete Course" };
            var fileDetails = new FileDetails { FileName = course.ImageUrl, Folder = "courses" };
            var cloudResult = await cloud.DeleteFileAsync(fileDetails);
            if (!cloudResult.success)
                return new ServiceResponse { success = false, message = "Failed to delete course image from cloud" };
            var categoryLinks = await CoursesCatManagment.GetAllAsync();
            var courseCategories = categoryLinks.Where(cc => cc.CourseId == id).ToList();
            foreach (var courseCategory in courseCategories)
            {
                await CoursesCatManagment.DeleteAsync(courseCategory);
            }
            return new ServiceResponse { success = true, message = "Course deleted successfully" };
        }

        public async Task<List<GetCourse>> GetAllCourses(string? userid)
        {
            var courses = await CoursesManagment.GetAllAsync();
            if (courses == null || !courses.Any())
                return new List<GetCourse>();
            var mappedCourses = mapper.Map<List<GetCourse>>(courses);

            var categoryLinks = await CoursesCatManagment.GetAllAsync();
            var ratings = await RatingManagment.GetAllAsync();
            
            foreach (var course in mappedCourses)
            {
                
                var courseCategories = categoryLinks.Where(cc => cc.CourseId == course.Id).ToList();
                var categories = new List<GetCategory>();
                foreach (var courseCategory in courseCategories)
                {
                    var category = await CategoryManagment.GetByIdAsync(courseCategory.CategoryId);
                    if (category != null)
                    {
                        categories.Add(mapper.Map<GetCategory>(category));
                    }
                }
                course.Categories = categories;
            }
          
                foreach (var course in mappedCourses)
                {
                    var courseRatings = ratings.Where(r => r.CourseId == course.Id).ToList();
                if (courseRatings.Any())
                    course.Rating = (float)courseRatings.Average(r => r.RatingValue);
                else
                    course.Rating = 0;

                if (!string.IsNullOrEmpty(userid))
                {
                    course.UserRating = courseRatings.FirstOrDefault(r => r.StudentId == userid)?.RatingValue ?? 0;
                }
                    
                    
                }
            
            return mappedCourses;
        }

        public async Task<GetCourse> GetCourseById(Guid id, string? userid)
        {
            var ratings = await RatingManagment.GetAllAsync();
            var course = await CoursesManagment.GetByIdAsync(id);
            if (course == null)
                return null;
            var mappedCourse = mapper.Map<GetCourse>(course);
            var categoryLinks = await CoursesCatManagment.GetAllAsync();
            var courseCategories = categoryLinks.Where(cc => cc.CourseId == id).ToList();
            var categories = new List<GetCategory>();
            foreach (var courseCategory in courseCategories)
            {
                var category = await CategoryManagment.GetByIdAsync(courseCategory.CategoryId);
                if (category != null)
                {
                    categories.Add(mapper.Map<GetCategory>(category));
                }
            }
            mappedCourse.Categories = categories;

            var courseRatings = ratings.Where(r => r.CourseId == course.Id).ToList();
            if (courseRatings.Any())
            {
                mappedCourse.Rating = (float)courseRatings.Average(r => r.RatingValue);
            }
            else
            {
                mappedCourse.Rating = 0;
            }
            if (!string.IsNullOrEmpty(userid))
            {
                mappedCourse.UserRating = courseRatings.FirstOrDefault(r => r.StudentId == userid)?.RatingValue ?? 0;
            }
            return mappedCourse;

        }

        public async Task<GetCourse> GetCourseByName(string name, string? userid)
        {
            var courses = await CoursesManagment.GetAllAsync();
            var course = courses.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (course == null )
                return null;
            var mappedCourse = mapper.Map<GetCourse>(course);
            var categoryLinks = await CoursesCatManagment.GetAllAsync();
            var courseCategories = categoryLinks.Where(cc => cc.CourseId == course.Id).ToList();
            var categories = new List<GetCategory>();
            foreach (var courseCategory in courseCategories)
            {
                var category = await CategoryManagment.GetByIdAsync(courseCategory.CategoryId);
                if (category != null)
                {
                    categories.Add(mapper.Map<GetCategory>(category));
                }
            }
            mappedCourse.Categories = categories;
            var ratings = await RatingManagment.GetAllAsync();
            var courseRatings = ratings.Where(r => r.CourseId == course.Id).ToList();
            if (courseRatings.Any())
            {
                mappedCourse.Rating = (float)courseRatings.Average(r => r.RatingValue);
            }
            else
            {
                mappedCourse.Rating = 0;
            }
        
            if (!string.IsNullOrEmpty(userid))
            {
                mappedCourse.UserRating = courseRatings.FirstOrDefault(r => r.StudentId == userid)?.RatingValue ?? 0;
            }


            return mappedCourse;
        }

        private async Task<ServiceResponse> UpdateDuration(Guid id, double duration)
        {
            if (id == Guid.Empty || duration <= 0)
                return new ServiceResponse { success = false, message = "Invalid Course ID or duration" };
            var course= await CoursesManagment.GetByIdAsync(id);
            if (course == null)
                return new ServiceResponse { success = false, message = "Course not found" };
            course.Duration += duration;
            var result = await CoursesManagment.UpdateAsync(course);
            if (result == null)
                return new ServiceResponse { success = false, message = "Failed to update Course duration" };
            return new ServiceResponse { success = true, message = "Course duration updated successfully" };
        }

        public async Task<ServiceResponse> UpdateCourse(UpdateCourse Course)
        {
            if (Course == null || Course.Id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid Course data" };
            var existingCourse = await CoursesManagment.GetByIdAsync(Course.Id);
            if (existingCourse == null)
                return new ServiceResponse { success = false, message = "Course not found" };
            existingCourse.Name = Course.Name;
            existingCourse.Description = Course.Description;
            existingCourse.Title = Course.Title;
            existingCourse.Price = Course.Price;

            if (Course.Image != null)
            {
                var fileDetails = new FileDetails { FileName = existingCourse.ImageUrl, Folder = "courses" };
                var cloudDeleteResult = await cloud.DeleteFileAsync(fileDetails);
                if (!cloudDeleteResult.success)
                    return new ServiceResponse { success = false, message = "Failed to delete old course image from cloud" };
                existingCourse.ImageUrl = $"{existingCourse.Id}-Thumbnail{Path.GetExtension(Course.Image.FileName)}";
                var addCloudFile = new AddCloudFile { Details = fileDetails, File = Course.Image };
                var cloudUploadResult = await cloud.UploadFileAsync(addCloudFile);
                if (!cloudUploadResult.success)
                    return new ServiceResponse { success = false, message = "Failed to upload new course image to cloud" };
            }
            var categoryLinks = await CoursesCatManagment.GetAllAsync();
            var existingCourseCategories = categoryLinks.Where(cc => cc.CourseId == Course.Id).ToList();
            foreach (var courseCategory in existingCourseCategories)
            {
                await CoursesCatManagment.DeleteAsync(courseCategory);
            }
            var categoryIds = Course.Categories.Split(',').ToList();
            foreach (var category in categoryIds)
            {
                Guid categoryId;
                if (!Guid.TryParse(category, out categoryId))
                    continue;
                var test = await CategoryManagment.GetByIdAsync(categoryId);
                if (test == null)
                    continue;
                var courseCategory = new CourseCategory { CourseId = Course.Id, CategoryId = test.Id };
                await CoursesCatManagment.AddAsync(courseCategory);
            }
            var updateResult = await CoursesManagment.UpdateAsync(existingCourse);
            if (updateResult == null)
                return new ServiceResponse { success = false, message = "Failed to update Course" };
            return new ServiceResponse { success = true, message = "Course updated successfully" };
        }

        public async Task<ServiceResponse> AddSession(CreateSession session)
        {
            if (session == null)
                return new ServiceResponse { success = false, message = "Invalid session data" };
            var mappedSession = mapper.Map<Session>(session);
            mappedSession.Date= DateTime .Today;
            var duration = GetVideoDuration(session.File);
            mappedSession.Duration = duration.TotalMinutes;
            await UpdateDuration(mappedSession.CourseId, duration.TotalMinutes);
            var fileDetails = new FileDetails { FileName = $"{mappedSession.Id}-SessionMaterial{Path.GetExtension(session.File.FileName)}", Folder = "sessions" };
            var addCloudFile = new AddCloudFile { Details = fileDetails, File = session.File };
            var uploadResult = await cloud.UploadFileAsync(addCloudFile);
            if (!uploadResult.success)
                return new ServiceResponse { success = false, message = "Failed to upload session material to cloud" };
            mappedSession.FileUrl = fileDetails.FileName;
            var result = await SessionManagment.AddAsync(mappedSession);
            if (result == null)
                return new ServiceResponse { success = false, message = "Failed to add session" };
            return new ServiceResponse { success = true, message = "Session added successfully" };

        }

        public async Task<ServiceResponse> UpdateSession(UpdateSession session)
        {
            if (session == null || session.Id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid session data" };
            var existingSession = await SessionManagment.GetByIdAsync(session.Id);
            if (existingSession == null)
                return new ServiceResponse { success = false, message = "Session not found" };
            
            if(session.File != null)
            {
                var duration = GetVideoDuration(session.File);
                var durationDifference = (double)duration.TotalMinutes - existingSession.Duration;
                await UpdateDuration(existingSession.CourseId, (int)durationDifference);
                existingSession.Duration = duration.TotalMinutes;
                var fileDetails = new FileDetails { FileName = existingSession.FileUrl, Folder = "sessions" };
                var cloudDeleteResult = await cloud.DeleteFileAsync(fileDetails);
                if (!cloudDeleteResult.success)
                    return new ServiceResponse { success = false, message = "Failed to delete old session material from cloud" };
                var newFileDetails = new FileDetails { FileName = $"{existingSession.Id}-SessionMaterial{Path.GetExtension(session.File.FileName)}", Folder = "sessions" };
                var addCloudFile = new AddCloudFile { Details = newFileDetails, File = session.File };
                var cloudUploadResult = await cloud.UploadFileAsync(addCloudFile);
                if (!cloudUploadResult.success)
                    return new ServiceResponse { success = false, message = "Failed to upload new session material to cloud" };
                existingSession.FileUrl = newFileDetails.FileName;
            }
            existingSession.Title = session.Title;
            existingSession.TrainerId = session.TrainerId;
            existingSession.FileUrl = $"{existingSession.Id}-SessionMaterial{Path.GetExtension(session.File.FileName)}";
            existingSession.SessionNumber = session.SessionNumber;

            var updateResult = await SessionManagment.UpdateAsync(existingSession);
            if (updateResult == null)
                return new ServiceResponse { success = false, message = "Failed to update session" };

            return new ServiceResponse { success = true, message = "Session updated successfully" };

        }
        private TimeSpan GetVideoDuration(IFormFile videoFile)
        {
            // Ensure the file is a video and has content
            if (videoFile == null || videoFile.Length == 0)
            {
                return TimeSpan.Zero;
            }

            // Save the IFormFile to a temporary file path
            var tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                videoFile.CopyTo(stream);
            }

            try
            {
                var inputFile = new MediaFile { Filename = tempFilePath };
                using (var engine = new Engine())
                {
                    engine.GetMetadata(inputFile);
                }

                // The duration is available in the metadata
                return inputFile.Metadata.Duration;
            }
            catch (Exception ex)
            {
                
                return TimeSpan.Zero;
            }
            finally
            {
                // Clean up the temporary file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        public async Task<ServiceResponse> DeleteSession(Guid id)
        {
            if (id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid session ID" };
            var existingSession = await SessionManagment.GetByIdAsync(id);
            if (existingSession == null)
                return new ServiceResponse { success = false, message = "Session not found" };
            var result = await SessionManagment.DeleteAsync(existingSession);
            if (result == 0)
                return new ServiceResponse { success = false, message = "Failed to delete session" };
            var fileDetails = new FileDetails { FileName = existingSession.FileUrl, Folder = "sessions" };
            var cloudResult = await cloud.DeleteFileAsync(fileDetails);
            if (!cloudResult.success)
                return new ServiceResponse { success = false, message = "Failed to delete session material from cloud" };
            return new ServiceResponse { success = true, message = "Session deleted successfully" };

        }

        public async Task<List<GetSession>> GetCourseAllSessions(Guid courdeid)
        {
            if (courdeid == Guid.Empty)
                return new List<GetSession>();
            var sessions = await SessionManagment.GetAllAsync();
            if (sessions == null || !sessions.Any())
                return new List<GetSession>();

            var courseSessions = sessions.Where(s => s.CourseId == courdeid).OrderBy(n=>n.SessionNumber).ToList();
            var mappedSessions = mapper.Map<List<GetSession>>(courseSessions);
            return mappedSessions;
        }

        public async Task<GetSession> GetSessionById(Guid id)
        {
            if (id == Guid.Empty)
                return null;
            var session = await SessionManagment.GetAllAsync();

            if (session == null||!session.Any())
                return null;
            var targetSession = session.FirstOrDefault(s => s.Id == id);
            if (targetSession == null)
                return null;
            var mappedSession = mapper.Map<GetSession>(targetSession);
            return mappedSession;
        }

        public async Task<GetSession> GetSessionByNumber(Guid courseid, int sessionnumber)
        {
            if (courseid == Guid.Empty || sessionnumber <= 0)
                return null;
            var sessions = await SessionManagment.GetAllAsync();
            if (sessions == null || !sessions.Any())
                return null;
            var targetSession = sessions.FirstOrDefault(s => s.CourseId == courseid && s.SessionNumber == sessionnumber);
            if (targetSession == null)
                return null;
            var mappedSession = mapper.Map<GetSession>(targetSession);
            return mappedSession;
        }

        public async Task<ServiceResponse> AddAssignment(CreateAssignment assignment)
        {
            if (assignment == null)
                return new ServiceResponse { success = false, message = "Invalid assignment data" };
            var mappedAssignment = mapper.Map<Assignment>(assignment);
            var fileDetails = new FileDetails { FileName = $"{mappedAssignment.Id}-AssignmentMaterial{Path.GetExtension(assignment.File.FileName)}", Folder = "assignments" };
            var addCloudFile = new AddCloudFile { Details = fileDetails, File = assignment.File };
            var uploadResult = await cloud.UploadFileAsync(addCloudFile);
            if (!uploadResult.success)
                return new ServiceResponse { success = false, message = "Failed to upload assignment material to cloud" };
            mappedAssignment.Content = fileDetails.FileName;
            var result = await AssignmentManagment.AddAsync(mappedAssignment);
            if (result == null)
                return new ServiceResponse { success = false, message = "Failed to add assignment" };
            return new ServiceResponse { success = true, message = "Assignment added successfully" };

        }

        public async Task<ServiceResponse> UpdateAssignment(UpdateAssignment assignment)
        {
            if (assignment == null || assignment.Id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid assignment data" };
            var existingAssignment = await AssignmentManagment.GetByIdAsync(assignment.Id);
            if (existingAssignment == null)
                return new ServiceResponse { success = false, message = "Assignment not found" };
            if (assignment.File != null)
            {
                var fileDetails = new FileDetails { FileName = existingAssignment.Content, Folder = "assignments" };
                var cloudDeleteResult = await cloud.DeleteFileAsync(fileDetails);
                if (!cloudDeleteResult.success)
                    return new ServiceResponse { success = false, message = "Failed to delete old assignment material from cloud" };
                var newFileDetails = new FileDetails { FileName = $"{existingAssignment.Id}-AssignmentMaterial{Path.GetExtension(assignment.File.FileName)}", Folder = "assignments" };
                var addCloudFile = new AddCloudFile { Details = newFileDetails, File = assignment.File };
                var cloudUploadResult = await cloud.UploadFileAsync(addCloudFile);
                if (!cloudUploadResult.success)
                    return new ServiceResponse { success = false, message = "Failed to upload new assignment material to cloud" };
                existingAssignment.Content = newFileDetails.FileName;
            }
            existingAssignment.Subject = assignment.Subject;
            existingAssignment.Description = assignment.Description;
            var updateResult = await AssignmentManagment.UpdateAsync(existingAssignment);
            if (updateResult == null)
                return new ServiceResponse { success = false, message = "Failed to update assignment" };
            return new ServiceResponse { success = true, message = "Assignment updated successfully" };

        }

        public async Task<ServiceResponse> DeleteAssignment(Guid id)
        {
            if (id == Guid.Empty)
                return new ServiceResponse { success = false, message = "Invalid assignment ID" };
            var existingAssignment = await AssignmentManagment.GetByIdAsync(id);
            if (existingAssignment == null)
                return new ServiceResponse { success = false, message = "Assignment not found" };
            var result = await AssignmentManagment.DeleteAsync(existingAssignment);
            if (result == 0)
                return new ServiceResponse { success = false, message = "Failed to delete assignment" };
            var fileDetails = new FileDetails { FileName = existingAssignment.Content, Folder = "assignments" };
            var cloudResult = await cloud.DeleteFileAsync(fileDetails);
            if (!cloudResult.success)
                return new ServiceResponse { success = false, message = "Failed to delete assignment material from cloud" };
            return new ServiceResponse { success = true, message = "Assignment deleted successfully" };
        }

        public async Task<List<GetAssignment>> GetCourseAllAssignments(Guid courdeid)
        {
            var assignments = await AssignmentManagment.GetAllAsync();
            if (assignments == null || !assignments.Any())
                return new List<GetAssignment>();
            var sessions = GetCourseAllSessions(courdeid);
            var courseAssignments = assignments.Where(a => sessions.Result.Any(s => s.Id == a.SessionId)).ToList();
            var mappedAssignments = mapper.Map<List<GetAssignment>>(courseAssignments);
            return mappedAssignments;


        }

        public async Task<GetAssignment> GetAssignmentById(Guid id)
        {
            if (id == Guid.Empty)
                return null;
            var assignment = await AssignmentManagment.GetByIdAsync(id);
            if (assignment == null)
                return null;
            var mappedAssignment = mapper.Map<GetAssignment>(assignment);
            return mappedAssignment;
        }

        public async Task<GetAssignment> GetAssignmentBySession(Guid sessionid)
        {
            if (sessionid == Guid.Empty)
                return null;
            var assignments = await AssignmentManagment.GetAllAsync();
            if (assignments == null || !assignments.Any())
                return null;
            var targetAssignment = assignments.FirstOrDefault(a => a.SessionId == sessionid);
            if (targetAssignment == null)
                return null;
            var mappedAssignment = mapper.Map<GetAssignment>(targetAssignment);
            return mappedAssignment;
        }
    }
}
