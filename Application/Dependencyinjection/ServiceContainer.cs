using Application.Mapping;
using Application.Services.Implementitions;
using Application.Services.Implementitions.Auth;
using Application.Services.Interfaces;
using Application.Services.Interfaces.Auth;
using Application.Validations;
using Application.Validations.Auth;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dependencyinjection
{
    public static class ServiceContainer
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingConfig>());
            services.AddValidatorsFromAssemblyContaining<RegisterValidation>();
            services.AddValidatorsFromAssemblyContaining<LoginValidation>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<ICloudService, CloudService>();

            services.AddFluentValidationAutoValidation();
           services.AddScoped<IAuthServices, AuthService>();



            return services;
        }
    }
}
