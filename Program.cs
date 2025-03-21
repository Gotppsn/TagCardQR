using CardTagManager.Data;
using CardTagManager.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options => {
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }
    );
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
// Register services
builder.Services.AddScoped<QrCodeService>();
builder.Services.AddSingleton<LdapAuthenticationService>(provider => 
    new LdapAuthenticationService(builder.Configuration["LdapSettings:Domain"] ?? "thaiparkerizing"));

builder.Services.AddMemoryCache();
builder.Services.AddScoped<FileUploadService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "CardTagManagerAuth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SessionStore = new MemoryCacheTicketStore();
    });    

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Card}/{action=Index}/{id?}");

app.Run();

// Memory cache ticket store implementation
public class MemoryCacheTicketStore : ITicketStore
{
    private readonly IMemoryCache _cache;
    
    public MemoryCacheTicketStore()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }
    
    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var id = Guid.NewGuid().ToString();
        RenewAsync(id, ticket);
        return Task.FromResult(id);
    }
    
    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = ticket.Properties.ExpiresUtc
        };
        
        _cache.Set(key, ticket, options);
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