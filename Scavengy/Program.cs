using Microsoft.EntityFrameworkCore;
using Scavengy.Data;
using Scavengy.ServiceInterface;
using ServiceStack;

var builder = WebApplication.CreateBuilder(args);

var licenseKey = builder.Configuration["ServiceStack:License"];
if (!string.IsNullOrEmpty(licenseKey))
{
    Licensing.RegisterLicense(licenseKey);
}

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient(); // for Azure OpenAI / Google Places later

builder.Services.AddDbContext<ScavengyDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddServiceStack(typeof(HuntService).Assembly);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ScavengyDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseServiceStack(new AppHost());

app.Run();

public class AppHost : AppHostBase
{
    public AppHost() : base("Scavengy", typeof(HuntService).Assembly) { }

    public override void Configure(Funq.Container container)
    {
        container.AddScoped<ScavengyDbContext>();
    }
}