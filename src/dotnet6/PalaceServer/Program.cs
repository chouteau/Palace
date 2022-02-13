using LogRWebMonitor;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using PalaceServer.Extensions;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Palace.Tests")]

var builder = WebApplication.CreateBuilder(args);

var palaceSection = builder.Configuration.GetSection("PalaceServer");
var palaceSettings = new PalaceServer.Configuration.PalaceServerSettings();
palaceSection.Bind(palaceSettings);
palaceSettings.PrepareFolders();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

builder.AddLogRWebMonitor(cfg =>
{
    if (builder.Environment.IsDevelopment())
	{
        cfg.LogLevel = LogLevel.Trace;
	}
    cfg.HostName = "PalaceServer";
});

builder.Services.AddSingleton(palaceSettings);
builder.Services.AddSingleton<PalaceServer.Services.MicroServiceCollectorManager>();
builder.Services.AddSingleton<PalaceServer.Services.PalaceInfoManager>();
builder.Services.AddSingleton<PalaceServer.Services.AdminLoginContext>();
builder.Services.AddHostedService<PalaceServer.Services.ZipRepositoryWatcher>();

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

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Trace);
}

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

app.UseLogRWebMonitor();

app.Run();
