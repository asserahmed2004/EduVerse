
using Application.Dependencyinjection;
using InfraStructure.Data;

using InfraStructure.Data.Seed;

using InfraStructure.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace API
{
    public class Program
    {

        public static async Task Main(string[] args)

        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:3000",
                            "https://localhost:3000",
                            "http://localhost:3001",
                            "https://localhost:3001")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });


            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Enter a JWT access token."
                    };

                    return Task.CompletedTask;
                });

                options.AddOperationTransformer((operation, context, cancellationToken) =>
                {
                    var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                    var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any();
                    var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();

                    if (requiresAuthorization && !allowsAnonymous)
                    {
                        operation.Security ??= new List<OpenApiSecurityRequirement>();
                        operation.Security.Add(new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            }] = Array.Empty<string>()
                        });
                    }

                    return Task.CompletedTask;
                });
            });

            builder.Services.AddInfraStructureServices(builder.Configuration);
            builder.Services.AddApplicationServices();



            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

            var app = builder.Build();
            //await SeedData.SeedAsync(app.Services);

            // Configure the HTTP request pipeline.


            app.MapScalarApiReference();
                app.MapOpenApi();
            
            app.UseInfraStructureService();
            
            app.UseCors();
            

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            //await SeedDatabaseAsync(app);

            app.Run();
        }

        //private static async Task SeedDatabaseAsync(WebApplication app)
        //{
        //    var seedOptions = app.Configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();
        //    if (!seedOptions.Enabled || !seedOptions.RunOnStartup)
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        using var scope = app.Services.CreateScope();
        //        var seeder = scope.ServiceProvider.GetRequiredService<RecommendationDataSeeder>();
        //        await seeder.SeedAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        //        logger.LogError(ex, "Recommendation seed data failed to run on startup.");
        //    }
        //}
    }
}
