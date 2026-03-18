using Domain.Entities;
using Domain.Interfaces;
using EntityFramework.Exceptions.SqlServer;
using InfraStructure.Data;
using InfraStructure.Repositories;
using InfraStructure.Repositries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraStructure.DependencyInjection
{
    public static class ServiceContainer
    {
        public static IServiceCollection AddInfraStructureServices
            (this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
            sqloptions =>
            {
                sqloptions.EnableRetryOnFailure();
                sqloptions.MigrationsAssembly(typeof(ServiceContainer).Assembly.FullName);
            }).UseExceptionProcessor(),
            ServiceLifetime.Scoped);
            services.AddScoped<IUserManagment, UserManagement>();
            services.AddScoped<ITokenManagment, TokenManagement>();
            services.AddScoped<IRoleManagment, RoleMangment>();
            services.AddScoped<IConfirmation,ConfirmationManagment>();
            services.AddScoped<IGeneric<Category>, GenericRepository<Category>>();
            services.AddScoped<IGeneric<Course>, GenericRepository<Course>>();
            services.AddScoped<IGeneric<CourseCategory>, GenericRepository<CourseCategory>>();
            services.AddScoped<IGeneric<Rating>, GenericRepository<Rating>>();


            services.AddDefaultIdentity<AppUser>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = true;
            }).AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>();
            services.AddAuthentication(op =>
            {
                op.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                op.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                op.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).
            AddJwtBearer(op =>
            {
                op.SaveToken = true;
                op.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = configuration["JWT:ValidAudience"],
                    ValidIssuer = configuration["JWT:ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]))
                };
            });
            
            return services;
        }
        public static IApplicationBuilder UseInfraStructureService(this IApplicationBuilder app)
        {
            return app;
        }
    }
   
}
