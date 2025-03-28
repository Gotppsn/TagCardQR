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
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
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
            new LdapAuthenticationService(configuration["LdapSettings:Domain"] ?? "thaiparkerizing"));
    }

    private static void ConfigureMiddleware(WebApplication app, IWebHostEnvironment environment)
    {
        // Set up logging for debugging
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        // Configure PathBase from appsettings.json
        var pathBase = app.Configuration["PathBase"];
        if (!string.IsNullOrEmpty(app.Configuration["PathBase"]))
        {
            app.UsePathBase(app.Configuration["PathBase"]);
        }
        
        // Configure forwarded headers to handle proxy scenarios
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        // Configure environment-specific middleware
        if (environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            logger.LogInformation("Running in Development mode");
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
            logger.LogInformation("Running in Production mode");
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
        
        // Add antiforgery middleware - new addition
        app.Use(async (context, next) =>
        {
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            // Send the token in the response cookies for JavaScript to use
            var tokens = antiforgery.GetAndStoreTokens(context);
            
            if (context.Request.Path.Value?.StartsWith("/api/") == true)
            {
                // For API requests, respond to token requests
                if (context.Request.Method == "GET" && context.Request.Headers.ContainsKey("X-Request-CSRF-Token"))
                {
                    context.Response.Headers.Append("X-CSRF-TOKEN", tokens.RequestToken);
                }
            }
            
            await next();
        });
        
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