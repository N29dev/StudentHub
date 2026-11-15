using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using StudentHub.Core;

namespace StudentHub.Services;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        app.MapGet("/login", (HttpContext ctx, IAntiforgery anti) =>
            ctx.User.Identity?.IsAuthenticated == true ? Results.Redirect("/") : Form(ctx, anti, false));
        app.MapGet("/register", (HttpContext ctx, IAntiforgery anti) =>
            ctx.User.Identity?.IsAuthenticated == true ? Results.Redirect("/") : Form(ctx, anti, true));
        app.MapGet("/denied", () => Results.Redirect("/"));
        app.MapPost("/login", async (HttpContext ctx, IAntiforgery anti, CampusService campus) =>
        {
            if (!await Valid(ctx, anti)) return Results.BadRequest("Invalid form. Reload and try again.");
            var f = await ctx.Request.ReadFormAsync();
            return await SignIn(ctx, campus.Login(f["email"].ToString(), f["password"].ToString()), "/login");
        });
        app.MapPost("/register", async (HttpContext ctx, IAntiforgery anti, CampusService campus) =>
        {
            if (!await Valid(ctx, anti)) return Results.BadRequest("Invalid form. Reload and try again.");
            if (ctx.User.Identity?.IsAuthenticated == true) return Results.Redirect("/");
            var f = await ctx.Request.ReadFormAsync();
            return await SignIn(ctx, campus.Register(f["name"].ToString(), f["email"].ToString(),
                f["password"].ToString(), f["role"].ToString(), f["lecturerCode"].ToString(),
                f["groupCode"].ToString()), "/register");
        });
        app.MapPost("/logout", async (HttpContext ctx, IAntiforgery anti) =>
        {
            if (!await Valid(ctx, anti)) return Results.BadRequest("Invalid form. Reload and try again.");
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        }).RequireAuthorization();
    }

    private static async Task<bool> Valid(HttpContext ctx, IAntiforgery anti)
    {
        try { await anti.ValidateRequestAsync(ctx); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }

    private static async Task<IResult> SignIn(HttpContext ctx, Result<ProfileInfo> result, string returnPath)
    {
        if (result is Fail<ProfileInfo> fail)
            return Results.Redirect(returnPath + "?error=" + Uri.EscapeDataString(fail.Error));
        var account = ((Ok<ProfileInfo>)result).Value;
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Name), new Claim(ClaimTypes.Role, account.Role) };
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = true });
        return Results.Redirect("/");
    }

    private static IResult Form(HttpContext ctx, IAntiforgery anti, bool register)
    {
        var token = HtmlEncoder.Default.Encode(anti.GetAndStoreTokens(ctx).RequestToken ?? "");
        var error = HtmlEncoder.Default.Encode(ctx.Request.Query["error"].ToString());
        var notice = string.IsNullOrEmpty(error) ? "" : $"<p class='notice' role='alert'>{error}</p>";
        var title = register ? "Create account" : "Sign in";
        var path = register ? "/register" : "/login";
        var fields = register ? """
            <label>Full name<input name="name" required maxlength="100" autocomplete="name"></label>
            <label>Role<select name="role" id="role"><option>Student</option><option>Lecturer</option></select></label>
            <label id="group-field">Group join code (optional)<input name="groupCode" maxlength="30"></label>
            <label id="lecturer-field" hidden>Lecturer invitation code<input name="lecturerCode" type="password"></label>
            """ : "";
        var demoEnabled = ctx.RequestServices.GetRequiredService<IConfiguration>()
            .GetValue("StudentHub:SeedDemo", true);
        var showDemo = !register && demoEnabled &&
            ctx.RequestServices.GetRequiredService<CampusService>().HasDemoData;
        var demo = showDemo ? """
            <div class="demo-logins"><p>Try a demo account</p>
            <button type="button" class="secondary" data-demo="student@demo.test">Student</button>
            <button type="button" class="secondary" data-demo="lecturer@demo.test">Lecturer</button>
            <small>Password: Demo12345</small></div>
            """ : "";
        var other = register ? "Already registered? <a href='/login'>Sign in</a>" :
            "New to StudentHub? <a href='/register'>Create an account</a>";
        return Results.Content($$"""
            <!DOCTYPE html><html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{title}} · StudentHub</title><link rel="stylesheet" href="/app.css"></head>
            <body class="auth-body"><div class="auth-card"><a class="brand" href="/login">StudentHub<span> / </span></a>
            <h1>{{title}}</h1><p class="muted">Your academic workspace.</p>{{notice}}
            <form method="post" action="{{path}}" class="auth-form">
            <input type="hidden" name="__RequestVerificationToken" value="{{token}}">
            {{fields}}
            <label>Email<input type="email" name="email" id="email" required maxlength="200" autocomplete="email"></label>
            <label>Password<input type="password" name="password" id="password" required minlength="8" maxlength="128" autocomplete="{{(register ? "new-password" : "current-password")}}"></label>
            <button class="button" type="submit">{{title}}</button></form>{{demo}}<p class="auth-other">{{other}}</p></div>
            <script>const role=document.getElementById('role');if(role){const sync=()=>{document.getElementById('group-field').hidden=role.value==='Lecturer';document.getElementById('lecturer-field').hidden=role.value!=='Lecturer';};role.addEventListener('change',sync);sync();}document.querySelectorAll('[data-demo]').forEach(b=>b.addEventListener('click',()=>{document.getElementById('email').value=b.dataset.demo;document.getElementById('password').value='Demo12345';}));</script>
            </body></html>
            """, "text/html; charset=utf-8");
    }
}
