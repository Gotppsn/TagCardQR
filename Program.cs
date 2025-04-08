// Path: Program.cs
using CardTagManager.Data;
using CardTagManager.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Antiforgery;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.Kestrel.Core;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configuration Management
        ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

        var app = builder.Build();

        // Middleware Configuration
        ConfigureMiddleware(app, builder.Environment);

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<DepartmentAccessService>();

        // Configure form options for file uploads
        services.Configure<FormOptions>(options =>
        {
            options.ValueLengthLimit = 30 * 1024 * 1024; // 30MB
            options.MultipartBodyLengthLimit = 30 * 1024 * 1024; // 30MB
            options.MultipartHeadersLengthLimit = 8192; // 8KB
        });

        // Configure IIS server options
        services.Configure<IISServerOptions>(options =>
        {
            options.MaxRequestBodySize = 30 * 1024 * 1024; // 30MB
        });

        // Configure Kestrel server options
        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = 30 * 1024 * 1024; // 30MB
        });

        // Database Context Configuration
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);

                    // Additional SQL Server configuration
                    sqlOptions.CommandTimeout(120); // 2-minute timeout
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                }
            );

            // Enable sensitive data logging only in development
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Core MVC and API Configuration
        services.AddControllersWithViews(options =>
        {
            // Production-specific configurations
            if (!environment.IsDevelopment())
            {
                options.Filters.Add(new Microsoft.AspNetCore.Mvc.RequireHttpsAttribute());
            }
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.WriteIndented = environment.IsDevelopment();
        });

        // Antiforgery Configuration - Updated for CSRF handling
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "XSRF-TOKEN";
            options.FormFieldName = "__RequestVerificationToken";
            options.SuppressXFrameOptionsHeader = false;
            options.Cookie.Path = "/"; // Ensure cookie applies to all paths
            options.Cookie.SameSite = SameSiteMode.Lax; // Changed from Strict to Lax
        });

        // Authentication Configuration
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(12);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Changed from Always
                options.Cookie.SameSite = SameSiteMode.Lax; // Changed from Strict
                options.Cookie.IsEssential = true;
                options.Cookie.MaxAge = TimeSpan.FromDays(30);
            });

        // CORS Configuration
        services.AddCors(options =>
        {
            options.AddPolicy("ProductionPolicy", builder =>
            {
                builder
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // Memory Caching
        services.AddMemoryCache();

        // Application Services
        services.AddScoped<QrCodeService>();
        services.AddScoped<FileUploadService>();
        services.AddScoped<UserProfileService>();
        services.AddScoped<RoleService>();
        services.AddSingleton<LdapAuthenticationService>(provider =>
            new LdapAuthenticationService(configuration["LdapSettings:Domain"] ?? "thaiparkerizing",
            provider.GetRequiredService<ILogger<LdapAuthenticationService>>()));
    }

    private static void ConfigureMiddleware(WebApplication app, IWebHostEnvironment environment)
    {
        // Set up logging for debugging
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        // Global Exception Handler
        app.UseExceptionHandler(appBuilder =>
        {
            appBuilder.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/html";
                
                var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerPathFeature?.Error;
                
                if (exception != null)
                {
                    logger.LogError(exception, "Unhandled exception");
                    
                    // In development, show more details
                    if (environment.IsDevelopment())
                    {
                        await context.Response.WriteAsync($"<html><body><h2>An error occurred:</h2><div>{exception.Message}</div><pre>{exception.StackTrace}</pre></body></html>");
                    }
                    else
                    {
                        await context.Response.WriteAsync("<html><body><h2>An error occurred. Please try again.</h2></body></html>");
                    }
                }
                else
                {
                    await context.Response.WriteAsync("<html><body><h2>An error occurred. Please try again.</h2></body></html>");
                }
            });
        });

        // Configure PathBase from appsettings.json - IMPORTANT!
        var pathBase = app.Configuration["PathBase"];
        if (!string.IsNullOrEmpty(pathBase))
        {
            // Ensure path starts with /
            if (!pathBase.StartsWith("/"))
                pathBase = "/" + pathBase;
                
            logger.LogInformation($"Setting PathBase to: {pathBase}");
            app.UsePathBase(pathBase);
            
            // Make sure all middleware sees the correct PathBase
            app.Use((context, next) => {
                context.Request.PathBase = pathBase;
                return next();
            });
        }

        // Standard middleware pipeline
        app.UseHttpsRedirection();

        // Static files with detailed options
        app.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream"
        });

        app.UseCors("ProductionPolicy");
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        // Log routing information
        logger.LogInformation($"Default Controller: Card, Default Action: Index");
        logger.LogInformation($"Application Root Path: {app.Environment.ContentRootPath}");

        // Configure endpoints
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers(); // This ensures API controller routes are registered
            endpoints.MapControllerRoute(
                name: "updateIssueStatus",
                pattern: "Card/UpdateIssueStatus",
                defaults: new { controller = "Card", action = "UpdateIssueStatus" }
            );

            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Card}/{action=Index}/{id?}"
            );
        });
    }
}

// Session Store Implementation (Optional Performance Enhancement)
public class MemoryCacheTicketStore : ITicketStore
{
    private readonly IMemoryCache _cache;

    public MemoryCacheTicketStore(IMemoryCache memoryCache)
    {
        _cache = memoryCache;
    }

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString();
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = ticket.Properties.ExpiresUtc
        };

        _cache.Set(key, ticket, options);
        return Task.FromResult(key);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        _cache.Set(key, ticket, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = ticket.Properties.ExpiresUtc
        });
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket> RetrieveAsync(string key)
    {
        _cache.TryGetValue(key, out AuthenticationTicket ticket);
        return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}