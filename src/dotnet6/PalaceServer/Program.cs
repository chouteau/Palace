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
builder.Services.AddMemoryCache();

builder.Services.AddSingleton(palaceSettings);
builder.Services.AddTransient<PalaceServer.Services.MicroServiceCollectorManager>();
builder.Services.AddSingleton<PalaceServer.Services.PalaceInfoManager>();
builder.Services.AddHostedService<PalaceServer.Services.ZipRepositoryWatcher>();   

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
