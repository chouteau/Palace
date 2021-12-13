using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Palace.Tests")]

var builder = WebApplication.CreateBuilder(args);

var palaceSection = builder.Configuration.GetSection("PalaceServer");
var palaceSettings = new PalaceServer.Configuration.PalaceServerSettings();
palaceSection.Bind(palaceSettings);
if (palaceSettings.MicroServiceRepositoryFolder.StartsWith(@".\"))
{
    var directoryName = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location);
    palaceSettings.MicroServiceRepositoryFolder = System.IO.Path.Combine(directoryName, palaceSettings.MicroServiceRepositoryFolder.Replace(@".\", ""));
    if (!System.IO.Directory.Exists(palaceSettings.MicroServiceRepositoryFolder))
    {
        System.IO.Directory.CreateDirectory(palaceSettings.MicroServiceRepositoryFolder);
    }
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton(palaceSettings);
builder.Services.AddSingleton<PalaceServer.Services.LogCollector>();
builder.Services.AddSingleton<PalaceServer.Services.MicroServiceCollectorManager>();
builder.Services.AddSingleton<PalaceServer.Services.PalaceInfoManager>();
builder.Services.AddHostedService<PalaceServer.Services.ZipRepositoryWatcher>();   

builder.Services.AddControllers();
builder.Services.AddScoped<AuthenticationStateProvider, PalaceServer.Services.CustomAuthStateProvider>();
builder.Services.AddSingleton<PalaceServer.Services.AdminLoginContext>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "PalaceServer";
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(15);
                    options.Cookie.HttpOnly = true;
                });

var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PalaceServer");
if (!Directory.Exists(folder))
{
    Directory.CreateDirectory(folder);
}
builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new System.IO.DirectoryInfo(folder))
        .SetApplicationName("PalaceServer")
        .SetDefaultKeyLifetime(TimeSpan.FromDays(60));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
