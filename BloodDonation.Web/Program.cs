using BloodDonation.Domain;
using BloodDonation.Web.Data;
using BloodDonation.Web.Identity;
using BloodDonation.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// One in-memory Identity store per app instance, so test hosts do not share users or roles.
var identityDatabaseName = $"BloodDonationIdentity-{Guid.NewGuid()}";
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseInMemoryDatabase(identityDatabaseName));
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Matches the rule shown on the register page.
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddSingleton<SampleDataStore>();
builder.Services.AddSingleton<StandDownRegister>();
builder.Services.AddScoped<EligibilityService>();
builder.Services.AddScoped<BookingEligibilityCheck>();
builder.Services.AddScoped<AppointmentAnalyticsService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
});
var app = builder.Build();

SampleDataSeeder.Seed(app.Services.GetRequiredService<SampleDataStore>());
await IdentitySeeder.SeedAsync(app.Services, app.Configuration, app.Environment);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// The default route comes first: when a URL matches both a main page and an area page
// (/Staff/Bookings), the page the navigation bar uses wins (#35).
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
