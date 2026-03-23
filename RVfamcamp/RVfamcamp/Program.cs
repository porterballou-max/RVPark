using RVfamcamp.Configuration;
using RVfamcamp.Database;
using RVfamcamp.Services;
using Stripe;
using Microsoft.AspNetCore.Authentication.Cookies;
using RVfamcamp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe")
);

builder.Services.Configure<AppSettings>(
	builder.Configuration.GetSection("App")
);

builder.Services.AddScoped<StripeService>();
builder.Services.AddSingleton<PaymentRepo>(); // TODO: Changed back to AddScoped once I connect this Repo to the real data base

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.Configure<StripeSettings>(
	builder.Configuration.GetSection("Stripe")
);

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

// Add SQL statements to whole project
builder.Services.AddScoped<DatabaseStatements>();


// For database
AppDomain.CurrentDomain.SetData("DataDirectory", Directory.GetCurrentDirectory());
var app = builder.Build();

var stripeSettings =
	builder.Configuration.GetSection("Stripe").Get<StripeSettings>();

var appSettings = builder.Configuration.GetSection("app").Get<AppSettings>();

StripeConfiguration.ApiKey = stripeSettings.SecretKey;

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

app.MapControllers();

app.Run();
