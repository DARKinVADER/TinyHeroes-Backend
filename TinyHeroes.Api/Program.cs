using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using System.Security.Claims;
using AspNet.Security.OAuth.Apple;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using TinyHeroes.Api.Middleware;
using TinyHeroes.Api.Services;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure;
using TinyHeroes.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Enable Serilog SelfLog to stderr so sink errors appear in container stdout
Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine($"[SERILOG] {msg}"));

var initialLevel = Enum.TryParse<LogEventLevel>(
    builder.Configuration["Serilog:InitialMinimumLevel"], out var parsedLevel)
    ? parsedLevel
    : LogEventLevel.Warning;
var levelSwitch = new LoggingLevelSwitch(initialLevel);
builder.Services.AddSingleton(levelSwitch);

var otelBuilder = builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        // SetDbQueryParameters defaults to false in this version — SQL text is not captured
        .AddEntityFrameworkCoreInstrumentation());

if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    otelBuilder.UseAzureMonitor();

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.MinimumLevel.ControlledBy(levelSwitch)
       .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
       .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .Enrich.WithThreadId()
       .ReadFrom.Configuration(ctx.Configuration));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                             | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMemoryCache();

builder.Services.AddIdentity<User, IdentityRole<Guid>>(opt =>
{
    opt.Password.RequireDigit = true;
    opt.Password.RequiredLength = 8;
    opt.SignIn.RequireConfirmedAccount = false;
    opt.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

var authBuilder = builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!);
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

if (!string.IsNullOrEmpty(builder.Configuration["Auth:Google:ClientId"]))
{
    authBuilder.AddGoogle(opt =>
    {
        opt.ClientId = builder.Configuration["Auth:Google:ClientId"]!;
        opt.ClientSecret = builder.Configuration["Auth:Google:ClientSecret"]!;
    });
}

if (!string.IsNullOrEmpty(builder.Configuration["Auth:Facebook:AppId"]))
{
    authBuilder.AddFacebook(opt =>
    {
        opt.AppId = builder.Configuration["Auth:Facebook:AppId"]!;
        opt.AppSecret = builder.Configuration["Auth:Facebook:AppSecret"]!;
    });
}

if (!string.IsNullOrEmpty(builder.Configuration["Auth:Apple:ClientId"]))
{
    authBuilder.AddApple(opt =>
    {
        opt.ClientId = builder.Configuration["Auth:Apple:ClientId"]!;
        opt.KeyId = builder.Configuration["Auth:Apple:KeyId"] ?? string.Empty;
        opt.TeamId = builder.Configuration["Auth:Apple:TeamId"] ?? string.Empty;
    });
}

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste your JWT from POST /api/auth/login"
            }
        };
        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var hasAuthorize = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>().Any();
        if (hasAuthorize)
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", context.Document, string.Empty)] = []
                }
            ];
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
        .AllowAnyMethod()
        .AllowAnyHeader()));

if (builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddSingleton<ICaptchaService, BypassCaptchaService>();
else
{
    builder.Services.AddHttpClient<TurnstileCaptchaService>();
    builder.Services.AddScoped<ICaptchaService, TurnstileCaptchaService>();
}

builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });
    opt.AddFixedWindowLimiter("feedback", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromHours(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
    {
        diagCtx.Set("RequestId", httpCtx.TraceIdentifier);
        diagCtx.Set("UserId",
            httpCtx.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "anon");
        diagCtx.Set("UserAgent", httpCtx.Request.Headers.UserAgent.ToString());
    };
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsEnvironment("Integration"))
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        await DatabaseSeeder.SeedAsync(dbContext, userManager);
    }
    else if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
    else
    {
        dbContext.Database.EnsureCreated();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
if (!app.Environment.IsEnvironment("Testing"))
    app.UseRateLimiter();

var uploadsPath = ParseUploadsPath(app.Configuration["Storage:ConnectionString"]);
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");
app.MapControllers();

app.Run();

static string ParseUploadsPath(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString)) return "/app/uploads";
    var idx = connectionString.IndexOf("path=", StringComparison.OrdinalIgnoreCase);
    var path = idx >= 0 ? connectionString[(idx + 5)..] : "/app/uploads";
    return Path.IsPathRooted(path) ? path : Path.GetFullPath(path);
}

public partial class Program { }
