using Backend.Common;
using Backend.Common.Options;
using Backend.Constants;
using Backend.Jobs;
using Backend.Models;
using Backend.Repositories.Implements;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using FluentValidation;
using Hangfire;
using Hangfire.Redis.StackExchange;
using StackExchange.Redis;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.Text;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // LOAD .env FILE
            DotNetEnv.Env.Load();

            // Map and Validate Environment Variables
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

            void MapOptionalEnv(string configKey, string envKey)
            {
                var envValue = Environment.GetEnvironmentVariable(envKey);
                if (envValue != null)
                {
                    builder.Configuration[configKey] = envValue;
                }
            }

            MapRequiredEnv("ConnectionStrings:MyCnn", "DB_CONNECTION_STRING");
            MapRequiredEnv("Jwt:Key", "JWT_KEY");
            MapRequiredEnv("Jwt:Issuer", "JWT_ISSUER");
            MapRequiredEnv("Jwt:Audience", "JWT_AUDIENCE");
            MapRequiredEnv("Jwt:AccessTokenMinutes", "JWT_ACCESS_TOKEN_MINUTES");
            MapRequiredEnv("Jwt:RefreshTokenDays", "JWT_REFRESH_TOKEN_DAYS");
            MapRequiredEnv("AuthCookie:Secure", "AUTH_COOKIE_SECURE");
            MapRequiredEnv("AuthCookie:SameSite", "AUTH_COOKIE_SAMESITE");
            MapOptionalEnv("AuthCookie:Domain", "AUTH_COOKIE_DOMAIN");
            MapRequiredEnv("EmailSettings:SmtpServer", "EMAIL_SMTP_SERVER");
            MapRequiredEnv("EmailSettings:Port", "EMAIL_PORT");
            MapRequiredEnv("EmailSettings:SenderEmail", "EMAIL_SENDER");
            MapRequiredEnv("EmailSettings:SenderPassword", "EMAIL_PASSWORD");
            MapRequiredEnv("Google:ClientId", "GOOGLE_CLIENT_ID");
            MapRequiredEnv("FrontendSettings:BaseUrl", "FRONTEND_BASE_URL");
            MapRequiredEnv("Redis:Connection", "REDIS_CONNECTION");

            // Kiểm tra tổng quát trước khi nổ app
            if (missingKeys.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Failed to start application. The following environment variables are missing in your .env file:\n" +
                    $"- {string.Join("\n- ", missingKeys)}"
                );
            }

            // database
            builder.Services.AddDbContext<MtcaSep490G26Context>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("MyCnn")
                )
            );

            builder.Services.AddHttpContextAccessor();

            // Register Repositories
            builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IExamBlueprintRepository, ExamBlueprintRepository>();
            builder.Services.AddScoped<ICourseRepository, CourseRepository>();
            builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
            builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
            builder.Services.AddScoped<IStudentExamRepository, StudentExamRepository>();
            builder.Services.AddScoped<IExamRepository, ExamRepository>();
            builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
            builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
            builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
            builder.Services.AddScoped<IAssignExamRepository, AssignExamRepository>();

            // get OPTIONS
            builder.Services.AddOptions<JwtOptions>()
                .Bind(builder.Configuration.GetSection(JwtOptions.SectionName));

            builder.Services.AddOptions<AuthCookieOptions>()
                .Bind(builder.Configuration.GetSection(AuthCookieOptions.SectionName));

            builder.Services.AddSingleton(TimeProvider.System);

            // Honor X-Forwarded-* từ cloudflared (loopback proxy) để Request.Scheme/RemoteIpAddress phản ánh client thật.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            // Register Services
            builder.Services.AddScoped<IAdminUserService, AdminUserService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
            builder.Services.AddSingleton<IOtpStore, RedisOtpStore>();
            builder.Services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
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
            // Add PracticeExam
            builder.Services.AddScoped<IPracticeExamRepository, PracticeExamRepository>();
            builder.Services.AddScoped<IPracticeExamService, PracticeExamService>();

            // Hangfire
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(
                    sp.GetRequiredService<IConfiguration>()["Redis:Connection"]!));
            builder.Services.AddSingleton<IExamStatusScheduler, ExamStatusScheduler>();
            builder.Services.AddTransient<ExamStatusTransitionJob>();
            var hangfireInvisibilityTimeout = TimeSpan.FromMinutes(5);
            builder.Services.AddHangfire((sp, config) =>
            {
                var mux = sp.GetRequiredService<IConnectionMultiplexer>();
                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseRedisStorage(mux, new RedisStorageOptions
                    {
                        Prefix = RedisKeys.HangfirePrefix,
                        Db = RedisKeys.HangfireDb,
                        InvisibilityTimeout = hangfireInvisibilityTimeout
                    });
            });
            builder.Services.AddHangfireServer();
            builder.Services.AddHostedService<ExamScheduleRehydrator>();

            // JWT AUTHENTICATION
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (!string.IsNullOrEmpty(context.Request.Headers.Authorization))
                        {
                            return Task.CompletedTask;
                        }

                        var token = context.Request.Cookies[AuthCookieDefaults.Name];
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            // OTHER SERVICES
            builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<Backend.Common.Validation.ValidationFilter>();
            })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                });

            var frontendOrigin = builder.Configuration["FrontendSettings:BaseUrl"]
                ?? throw new InvalidOperationException("FrontendSettings:BaseUrl is required for CORS.");
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontendOrigin", policy =>
                {
                    policy.WithOrigins(frontendOrigin.TrimEnd('/'))
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
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

            app.UseForwardedHeaders();

            app.UseMiddleware<Backend.Middleware.ExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("FrontendOrigin");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<Backend.Middleware.FirstLoginPasswordMiddleware>();

            // Hangfire Dashboard
            if (app.Environment.IsDevelopment())
            {
                app.UseHangfireDashboard("/hangfire");
            }

            app.MapControllers();

            app.Run();
        }
    }
}
