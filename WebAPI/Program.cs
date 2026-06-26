using Application;
using Application.Services.AppleAuth;
using Application.Services.AudioStorage;
using Application.Services.CurrentUser;
using Application.Services.SmsService;
using Application.Services.StoryGeneration;
using Application.Services.Tts;
using WebAPI.Security;
using Core.CrossCuttingConcerns.Exception.WebAPI.Extensions;
using Core.CrossCuttingConcerns.Logging.Configurations;
using Core.Security.Encryption;
using Core.Security.JWT;
using Core.Security.WebApi.Extensions;
using Infrastructure;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables();

var configuration = configurationBuilder.Build();

builder.Services.AddControllers();

builder.Services.AddApplicationServices(
    seqLogConfiguration: builder
        .Configuration.GetSection("SeriLogConfigurations:SeqLogConfiguration")
        .Get<SeqLogConfiguration>());
builder.Services.AddInfrastructureServices();
builder.Services.Configure<TokenOptions>(configuration.GetSection("TokenOptions"));

builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<TokenOptions>>().Value);
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("OtpSettings"));
// builder.Configuration includes user-secrets in Development, so Anthropic:ApiKey resolves there.
builder.Services.Configure<AnthropicOptions>(builder.Configuration.GetSection("Anthropic"));
builder.Services.Configure<StorySettings>(builder.Configuration.GetSection("StorySettings"));
builder.Services.Configure<GoogleTtsOptions>(builder.Configuration.GetSection("GoogleTts"));
// CloudflareR2 credentials resolve from user-secrets (dev) / env CloudflareR2__* (prod).
builder.Services.Configure<CloudflareR2Options>(builder.Configuration.GetSection("CloudflareR2"));
// Apple Sign-In: Audience must match the iOS bundle id (default com.masal.app).
builder.Services.Configure<AppleAuthOptions>(builder.Configuration.GetSection("AppleAuth"));

builder.Host.UseSerilog((_, lc) => lc.ReadFrom.Configuration(configuration));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddPersistenceServices(builder.Configuration);

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            RequireExpirationTime = false,
            ValidIssuer = configuration["TokenOptions:Issuer"],
            ValidAudience = configuration["TokenOptions:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(configuration["TokenOptions:SecurityKey"])
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
    {
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader();
    })
);

builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition(
        name: "Bearer",
        securityScheme: new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer YOUR_TOKEN\". \r\n\r\n"
                + "`Enter your token in the text input below.`"
        }
    );
    opt.OperationFilter<BearerSecurityRequirementOperationFilter>();
});

// Background story generation: in-memory queue + a worker so `tonight` returns immediately.
builder.Services.AddSingleton<WebAPI.BackgroundServices.StoryGenerationQueue>();
builder.Services.AddSingleton<Application.Services.StoryPipeline.IStoryGenerationQueue>(
    sp => sp.GetRequiredService<WebAPI.BackgroundServices.StoryGenerationQueue>());
builder.Services.AddHostedService<WebAPI.BackgroundServices.StoryGenerationBackgroundService>();

var app = builder.Build();

// Apply EF migrations on startup so a fresh hosted database (e.g. Neon on first deploy) gets its
// schema - nothing runs `dotnet ef database update` in that environment.
using (IServiceScope migrationScope = app.Services.CreateScope())
{
    BaseDbContext db = migrationScope.ServiceProvider.GetRequiredService<BaseDbContext>();
    db.Database.Migrate();
}

if (environment == "Development")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.ConfigureCustomExceptionMiddleware();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseCors();

app.Run();
