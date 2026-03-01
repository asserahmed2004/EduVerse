using Application.DTOs.Auth;
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
        }
    }
}
