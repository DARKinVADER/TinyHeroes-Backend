using System.Text;
using System.Text.Json.Serialization;
using AspNet.Security.OAuth.Apple;
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
using TinyHeroes.Api.Middleware;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure;
using TinyHeroes.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.IsRelational())
        dbContext.Database.Migrate();
    else
        dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

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
