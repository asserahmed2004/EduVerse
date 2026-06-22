
using Application.Dependencyinjection;
using InfraStructure.Data.Seed;
using InfraStructure.DependencyInjection;
using API.Middleware;
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
                options.AddPolicy("Front",policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:3000",
                            "https://localhost:3000",
                            "http://localhost:3001",
                            "https://localhost:3001",
                            "https://eduverseweb.azurewebsites.net")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();
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
            builder.Services.AddApplicationServices(builder.Configuration);

            var app = builder.Build();

            app.MapScalarApiReference();
            app.MapOpenApi();

            app.UseInfraStructureService();
            
            app.UseMiddleware<ApiExceptionMiddleware>();
            app.UseHttpsRedirection();
            app.UseCors("Front");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<RequestPerformanceMiddleware>();
            app.MapControllers();

            await SeedDatabaseAsync(app);

            app.Run();
        }

        private static async Task SeedDatabaseAsync(WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var environmentName = app.Environment.EnvironmentName;
            var seedOptions = app.Configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();

            logger.LogInformation("Application environment: {EnvironmentName}", environmentName);
            logger.LogInformation(
                "Data seeding configuration -> Enabled: {Enabled}, RunOnStartup: {RunOnStartup}",
                seedOptions.Enabled,
                seedOptions.RunOnStartup);

            if (!seedOptions.Enabled || !seedOptions.RunOnStartup)
            {
                logger.LogInformation("RecommendationDataSeeder skipped because seeding is disabled in configuration.");
                return;
            }

            try
            {
                using var scope = app.Services.CreateScope();
                var seeder = scope.ServiceProvider.GetRequiredService<RecommendationDataSeeder>();
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RecommendationDataSeeder failed during startup.");
                throw;
            }
        }
    }
}
