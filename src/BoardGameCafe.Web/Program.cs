using BoardGameCafe.Web.Infrastructure;
using BoardGameCafe.Web.Infrastructure.Identity;
using BoardGameCafe.Web.Infrastructure.Persistence;
using BoardGameCafe.Web.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCafeIdentity(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapRazorPages();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await DbInitializer.InitializeAsync(app.Services);

app.Run();
