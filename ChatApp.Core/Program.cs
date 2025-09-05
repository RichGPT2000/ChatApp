using ChatApp.Core.Data;
using Microsoft.EntityFrameworkCore;
using ChatApp.Core.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();
builder.Services.AddScoped<ChatApp.Core.Services.ChatService>();

builder.Services.AddDbContextFactory<AppDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Default")!;
    opt.UseSqlite(cs);
});

// System info abstractions
builder.Services.AddSingleton<IAssemblyInfoProvider, AssemblyInfoProvider>();
builder.Services.AddSingleton<IRuntimeInfoProvider, RuntimeInfoProvider>();
builder.Services.AddSingleton<IEnvironmentReader, EnvironmentReader>();
builder.Services.AddSingleton<ICommitProvider, CommitProvider>();

// EF diagnostics + version provider
builder.Services.AddSingleton<IEFDiagnosticsProvider, EFDiagnosticsProvider>();
builder.Services.AddSingleton<IVersionInfoProvider, VersionInfoProvider>();

var app = builder.Build();

// Create DB schema if missing and initialize version provider
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();

    var vprov = scope.ServiceProvider.GetRequiredService<IVersionInfoProvider>();
    await vprov.GetOrCreateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Diagnostics endpoint
app.MapGet("/version", (IVersionInfoProvider v) =>
{
    return Results.Json(v.Current, new JsonSerializerOptions { WriteIndented = true });
});

app.MapBlazorHub();
app.MapHub<ChatApp.Core.Hubs.ChatHub>("/chathub");
app.MapFallbackToPage("/_Host");

app.Run();
