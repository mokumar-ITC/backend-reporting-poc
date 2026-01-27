using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using BIEmbedSystem.API.Controllers;
using BIEmbedSystem.API.Jobs;
using BIEmbedSystem.Core;
using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Core.Interfaces;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Infrastrucure.Repository;
using BIEmbedSystem.Infrastrucure.UnitOfWork;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Quartz;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory());
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// DB
builder.Services.AddDbContext<MDMDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection")));
builder.Services.AddScoped<SendGripEmail>();
builder.Services.AddScoped<EmailServiceGraph>();

// Azure AD Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Data_Plexus_Read.Read"));
});

// Serilog
builder.Host.UseSerilog((context, services, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext();
});


// ---------------------------------------------------------
// ✅ CORS (CORRECT WAY) — Supports localhost + production
// ---------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost",
                "https://localhost",
                "https://cms-365-ebsacc.itciss.com", // Add production frontend
                "http://localhost:5173"       // Local Dev Frontend
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
            //.AllowCredentials();
    });
});

// Controllers
builder.Services.AddControllers();

// DI Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<PBIManagementService>();
builder.Services.AddScoped<HomeServices>();
builder.Services.AddScoped<ReportPbiEmbedService>();
builder.Services.AddScoped<AzureGraphService>();
builder.Services.AddScoped<FabricCapacityService>();
builder.Services.AddScoped<SchedulerService>();
builder.Services.AddTransient<CapacityOperationJob>();
builder.Services.AddScoped<AadService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddMemoryCache();
builder.Services.Configure<AzureAdSettings>(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddScoped<IPlanFeatureService, PlanFeatureService>();
builder.Services.AddScoped<IOrganizationSubscriptionService, OrganizationSubscriptionService>();
builder.Services.AddScoped<IUserSubscriptionService, UserSubscriptionService>();
builder.Services.AddHostedService<SubscriptionExpiryService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<UserTrackingService>();
builder.Services.AddScoped<SubscriptionPlanService>();
builder.Services.Configure<IISServerOptions>(o => o.MaxRequestBodySize = long.MaxValue);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = long.MaxValue);
builder.Services.AddHttpClient();
builder.Services.AddScoped<ITranslationService, TranslationService>();
builder.Services.AddHttpClient<IPowerBiService, PowerBiService>();
builder.Services.AddScoped<IPowerBiService, PowerBiService>();
builder.Services.AddScoped<ISemanticSchedulerService, SemanticSchedulerService>();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddHostedService<CapacityScheduleMonitor>();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ---------------------------------------------------------
// ⭐ SWAGGER — ENABLED FOR BOTH DEVELOPMENT + PRODUCTION
// ---------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BIEmbedSystem API", Version = "v1" });

    // Add bearer token
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    options.OperationFilter<FileUploadOperationFilter>();
        
});


// Build App
var app = builder.Build();

// Start Quartz Scheduled Jobs
//using (var scope = app.Services.CreateScope())
//{
//    var schedulerService = scope.ServiceProvider.GetRequiredService<SchedulerService>();
//    var db = scope.ServiceProvider.GetRequiredService<MDMDbContext>();
//    var schedules = db.Capacity_Scheduler.Where(s => s.Status == "Active").ToList();
//    await schedulerService.ScheduleCapacityJobsAsync(schedules);
//}

// Logging
app.UseSerilogRequestLogging();

// ---------------------------------------------------------
// ⭐ SWAGGER AVAILABLE IN PROD + DEV
// ---------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "BIEmbedSystem API v1");
    options.RoutePrefix = "swagger"; // https://localhost:7242/swagger
});
app.UseStaticFiles(); // For wwwroot

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logos")),
    RequestPath = "/logos"
});


// ---------------------------------------------------------
// Pipeline Order (Correct for IIS + JWT + HTTPS)
// ---------------------------------------------------------
app.UseCors("DefaultCors");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

app.Run();
