using Backend.Models;
using Backend.Repositories.Implements;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using System.Text;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ============================
            // LOAD .env FILE
            // ============================
            DotNetEnv.Env.Load();

            // ============================
            // Map and Validate Environment Variables
            // ============================
            var missingKeys = new List<string>();

            // Local helper function to validate and map (reads from .env first, then falls back to existing appsettings.json config)
            void MapRequiredEnv(string configKey, string envKey)
            {
                var envValue = Environment.GetEnvironmentVariable(envKey);
                var existingConfigValue = builder.Configuration[configKey];
                
                // Lỗi khi cả .env VÀ appsettings.json đều rỗng
                if (string.IsNullOrWhiteSpace(envValue) && string.IsNullOrWhiteSpace(existingConfigValue))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[CRITICAL] Missing required config: '{configKey}'. Add as {envKey} in .env or straight to appsettings.json!");
                    Console.ResetColor();
                    missingKeys.Add(envKey);
                    return;
                }

                // Ưu tiên nạp đè cấu hình nếu có tham số môi trường
                if (!string.IsNullOrWhiteSpace(envValue))
                {
                    builder.Configuration[configKey] = envValue;
                }
            }

            MapRequiredEnv("ConnectionStrings:MyCnn", "DB_CONNECTION_STRING");
            MapRequiredEnv("Jwt:Key", "JWT_KEY");
            MapRequiredEnv("Jwt:Issuer", "JWT_ISSUER");
            MapRequiredEnv("Jwt:Audience", "JWT_AUDIENCE");
            MapRequiredEnv("EmailSettings:SmtpServer", "EMAIL_SMTP_SERVER");
            MapRequiredEnv("EmailSettings:Port", "EMAIL_PORT");
            MapRequiredEnv("EmailSettings:SenderEmail", "EMAIL_SENDER");
            MapRequiredEnv("EmailSettings:SenderPassword", "EMAIL_PASSWORD");
            MapRequiredEnv("Google:ClientId", "GOOGLE_CLIENT_ID");
            MapRequiredEnv("FrontendSettings:BaseUrl", "FRONTEND_BASE_URL");

            // Kiểm tra tổng quát trước khi nổ app
            if (missingKeys.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Failed to start application. The following environment variables are missing in your .env file:\n" +
                    $"- {string.Join("\n- ", missingKeys)}"
                );
            }

            // =========================
            // DATABASE
            // =========================
            builder.Services.AddDbContext<MtcaSep490G26Context>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("MyCnn")
                )
            );

            // Register Repositories
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IExamBlueprintRepository, ExamBlueprintRepository>();
            builder.Services.AddScoped<ICourseRepo, CourseRepo>();
            builder.Services.AddScoped<IChapterRepo, ChapterRepo>();
            builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IStudentExamRepository, StudentExamRepository>();
            builder.Services.AddScoped<IExamRepository, ExamRepository>();
            builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
            builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
            builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
            builder.Services.AddScoped<IAssignExamRepository, AssignExamRepository>();

            // Register Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IExamBlueprintService, ExamBlueprintService>();
            builder.Services.AddScoped<IAssignExamService, AssignExamService>();
            builder.Services.AddScoped<ICourseService, CourseService>();
            builder.Services.AddScoped<IChapterService, ChapterService>();
            builder.Services.AddScoped<IQuestionService, QuestionService>();
            builder.Services.AddScoped<IStudentExamService, StudentExamService>();
            builder.Services.AddScoped<ISubmissionService, SubmissionService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            // Add AnalyticsService
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();


            // =========================
            // JWT AUTHENTICATION
            // =========================
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "")
                    )
                };
            });

            // =========================
            // OTHER SERVICES
            // =========================
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                });
            builder.Services.AddSignalR();
            builder.Services.AddMemoryCache();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new Microsoft.OpenApi.Models.OpenApiInfo
                    {
                        Title = "MTCA API",
                        Version = "v1"
                    });

                c.AddSecurityDefinition("Bearer",
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Description = "Authorization: Bearer {token}",
                        Name = "Authorization",
                        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                        Scheme = "Bearer"
                    });

                c.AddSecurityRequirement(
                    new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                    {
                        {
                            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                            {
                                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                                {
                                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new List<string>()
                        }
                    });
            });

            var app = builder.Build();

            // =========================
            // PIPELINE
            // =========================
            app.UseMiddleware<Backend.Helper.ExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<Backend.Hubs.ExamHub>("/examHub");

            app.Run();
        }
    }
}
