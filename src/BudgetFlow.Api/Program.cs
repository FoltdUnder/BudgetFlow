using BudgetFlow.Infrastructure;
using BudgetFlow.Api.Middleware;
using Serilog;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using BudgetFlow.Infrastructure.Auth;
using BudgetFlow.Domain.Entities;
using BudgetFlow.Api.Common;

const string FrontendCorsPolicy = "FrontendCors";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddOpenApi();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers();
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<RefreshTokenCookieManager>();

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(FrontendCorsPolicy, policy =>
        {
            if (allowedOrigins is { Length: > 0 })
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        });
    });

    builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

    var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
    var jwtOptions = jwtSection.Get<JwtOptions>() 
                    ?? throw new InvalidOperationException("Jwt settings are missing.");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Key)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = JwtAuthErrorResponseWriter.HandleAuthenticationFailedAsync,
                OnChallenge = JwtAuthErrorResponseWriter.HandleChallengeAsync
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("UserOnly", policy =>
            policy.RequireRole(Roles.User, Roles.Admin));

        options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole(Roles.Admin));
    });

    var app = builder.Build();

    await app.Services.InitializeDatabaseAsync();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseGlobalExceptionMiddleware();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseCors(FrontendCorsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
