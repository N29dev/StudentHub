using StudentHub.Components;
using StudentHub.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton(new HubStore(Path.Combine(builder.Environment.ContentRootPath,
    "App_Data", "studenthub.json")));

var app = builder.Build();
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/error");

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
