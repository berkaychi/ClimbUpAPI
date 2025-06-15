using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using ClimbUpAPI.Data;
using ClimbUpAPI.Services;
using ClimbUpAPI.Services.Implementations;
using NSwag;
using NSwag.Generation.Processors.Security;
using ClimbUpAPI.Models;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using ClimbUpAPI.Mappings;
using Serilog;
using ClimbUpAPI.Middleware;
using ClimbUpAPI.Helpers;
using ClimbUpAPI.Services.HostedServices;
using ClimbUpAPI.Services.Strategies.Badge;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration.Sources.Clear();

builder.Configuration
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();


if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Configuration.AddCommandLine(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Debug);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
         .ReadFrom.Configuration(context.Configuration)
         .ReadFrom.Services(services)
         .Enrich.FromLogContext()
         .Enrich.WithMachineName()
         .Enrich.WithThreadId()
         .Enrich.WithProcessId()
         .Enrich.WithProperty("ApplicationName", "ClimbUpAPI")
         .Enrich.WithProperty("EnvironmentName", context.HostingEnvironment.EnvironmentName);
});

builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new IsoUtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableIsoUtcDateTimeConverter());
    });

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.AllowedForNewUsers = true;
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration.GetSection("AppSettings:Secret").Value ?? "")),
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                var user = await userManager.FindByIdAsync(userId);
                var securityStamp = context.Principal?.FindFirst("SecurityStamp")?.Value;

                if (user == null || user.SecurityStamp != securityStamp)
                {
                    context.Fail("Token is no longer valid");
                }
            }
        }
    };
});

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>(i => new SmtpEmailSender(
    builder.Configuration["EmailSender:Host"],
    builder.Configuration.GetValue<int>("EmailSender:Port"),
    builder.Configuration.GetValue<bool>("EmailSender:EnableSSL"),
    builder.Configuration["EmailSender:Username"],
    builder.Configuration["EmailSender:Password"])
    );



builder.Services.AddScoped<IFocusSessionService, FocusSessionService>();
builder.Services.AddScoped<ISessionTypeService, SessionTypeService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IUserProfileService, UserService>();
builder.Services.AddScoped<IAccountManagementService, UserService>();
builder.Services.AddScoped<IUserSessionService, UserService>();
builder.Services.AddScoped<IAdminUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IToDoService, ToDoService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IBadgeService, BadgeService>();
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<IUserFocusAnalyticsService, UserFocusAnalyticsService>();
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddHostedService<AbandonedSessionCleanupService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddHostedService<AssignTasksBackgroundService>();
builder.Services.AddHostedService<ToDoOverdueService>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddScoped<IBadgeStrategy, CompletedSessionsStrategy>();
builder.Services.AddScoped<IBadgeStrategy, FocusDurationStrategy>();
builder.Services.AddScoped<IBadgeStrategy, ToDosCompletedStrategy>();
var MyDevelopmentCorsPolicy = "_myDevelopmentCorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyDevelopmentCorsPolicy,
                      policy =>
                      {
                          policy.WithOrigins(
                                    "http://localhost:3000",
                                    "http://localhost:5173",
                                    "http://127.0.0.1:3000",
                                    "http://127.0.0.1:5173",
                                    "https://climbup-app.vercel.app"
                                 )
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                      });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ClimbUpAPI", Version = "v1" });
    c.EnableAnnotations();

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token'ınızı giriniz. Örnek: Bearer 12345abcdef"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddEndpointsApiExplorer();


builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseCors(MyDevelopmentCorsPolicy);
/*if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
*/

app.UseSwagger();
app.UseSwaggerUI();
app.UseDeveloperExceptionPage();

app.UseRouting();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RequestLogContextMiddleware>();

app.MapControllers();

app.Run();
