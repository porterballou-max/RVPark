using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// We love cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options=>
    {
        options.Cookie.HttpOnly = true;     // Prevents Javascript
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;    // Only use on HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax;     // Keeps cookie just on our site
        options.Cookie.IsEssential = true;      // For compliance with some European privacy law

        options.ExpireTimeSpan = TimeSpan.FromDays(14);     // Cookie expires 14 days after being created
        options.SlidingExpiration = true;       // Allows cookie expiration to move if use logs in again
        options.LoginPath = "/Login";            // Where to redirect user if not logged in (if cookie expired)
        options.AccessDeniedPath = "/Login";     // Where to redirect user if cookie fails (i.e. password changed)
        options.ReturnUrlParameter = "ReturnUrl";
    });
    

// For database
AppDomain.CurrentDomain.SetData("DataDirectory", Directory.GetCurrentDirectory());
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCookiePolicy();      
app.UseAuthentication();    // For Cookies
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
