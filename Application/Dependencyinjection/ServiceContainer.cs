using Application.Mapping;
using Application.Services.Implementitions;
using Application.Configuration;
using Application.Services.Implementitions.Auth;
using Application.Services.Interfaces;
using Application.Services.Interfaces.Auth;
using Application.Validations;
using Application.Validations.Auth;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Configuration;
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
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingConfig>());
            services.AddValidatorsFromAssemblyContaining<RegisterValidation>();
            services.AddValidatorsFromAssemblyContaining<LoginValidation>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<ICloudService, CloudService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IActivityLogService, ActivityLogService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IOrganizationService, OrganizationService>();
            services.AddScoped<IInstructorService, InstructorService>();
            services.AddScoped<IRecommendationService, RecommendationService>();

            services.AddFluentValidationAutoValidation();
           services.AddScoped<IAuthServices, AuthService>();
            services.Configure<PaymobOptions>(configuration.GetSection(PaymobOptions.SectionName));
            var paymobBaseUrl = configuration[$"{PaymobOptions.SectionName}:BaseUrl"]
                ?? "https://accept.paymob.com/api/";
            if (!paymobBaseUrl.EndsWith('/'))
            {
                paymobBaseUrl += "/";
            }

            services.AddHttpClient("Paymob", options =>
            {
                options.BaseAddress = new Uri(paymobBaseUrl, UriKind.Absolute);
                options.Timeout = TimeSpan.FromSeconds(30);
                options.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            });



            return services;
        }
    }
}
