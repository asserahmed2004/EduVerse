using Application.DTOs.Auth;
using Application.DTOs.Category;
using Application.DTOs.Course;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Application.Mapping
{
    public class MappingConfig:Profile
    {
        public MappingConfig()

        {
            CreateMap<AppUser, RegisterUser>().ForMember(u => u.Password, opt => opt.MapFrom(r => r.PasswordHash)).ReverseMap();
            CreateMap<AppUser, LoginUser>().ForMember(u => u.Password, opt => opt.MapFrom(r => r.PasswordHash)).ReverseMap();
            CreateMap<AppUser, GetUser>().ReverseMap();
            CreateMap<EmailConfirmation,ConfirmEmail>().ReverseMap();
            CreateMap<GetCategory, Category>().ReverseMap();
            CreateMap<CreateCategory, Category>().ReverseMap();
            CreateMap<UpdateCategory, Category>().ReverseMap();
            CreateMap<Course, GetCourse>().ReverseMap();
            CreateMap<Course, CreateCourse>().ReverseMap();
            CreateMap<Course, UpdateCourse>().ReverseMap();
        }
    }
}
