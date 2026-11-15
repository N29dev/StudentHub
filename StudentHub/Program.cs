using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Net;
using StudentHub.Components;
using StudentHub.Services;

var builder = WebApplication.CreateBuilder(args);
// Local exam demonstration defaults. Environment variables are optional overrides.
var demoMode = builder.Configuration.GetValue("StudentHub:SeedDemo", true);
if (demoMode && string.IsNullOrEmpty(builder.Configuration["StudentHub:LecturerCode"]))
    builder.Configuration["StudentHub:LecturerCode"] = "12345678";
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
    builder.WebHost.UseUrls("http://localhost:5080");
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var store = new CampusStore(Path.Combine(dataDirectory, "campus.json"));
if (demoMode)
{
    try
    {
        if (DemoData.Seed(store, DateOnly.FromDateTime(DateTime.Today)))
            Console.WriteLine("Demo data created.");
        Console.WriteLine("Demo logins: student@demo.test or lecturer@demo.test / Demo12345");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"Demo data not created: {ex.Message}");
    }
}
builder.Services.AddSingleton(store);
builder.Services.AddSingleton<CampusService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/denied";
        options.Cookie.Name = "StudentHub.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.Events.OnValidatePrincipal = async context =>
        {
            var service = context.HttpContext.RequestServices.GetRequiredService<CampusService>();
            if (!Guid.TryParse(context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                || service.GetProfile(id) is not { } profile
                || !context.Principal!.IsInRole(profile.Role))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();
// Shared public demo passwords must not be reachable from other computers.
if (demoMode)
{
    app.Use(async (context, next) =>
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip is not null && !IPAddress.IsLoopback(ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("StudentHub demo is available on localhost only.");
            return;
        }
        await next(context);
    });
}
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/error");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapAccountEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode().RequireAuthorization();
app.Run();
