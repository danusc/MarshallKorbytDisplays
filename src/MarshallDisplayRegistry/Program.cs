using Azure.Identity;
using MarshallDisplayRegistry.Data;
using MarshallDisplayRegistry.Security;
using MarshallDisplayRegistry.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

builder.Services.Configure<SignageOptions>(builder.Configuration.GetSection(SignageOptions.SectionName));
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.Configure<TelemetryConfiguration>(config =>
{
    config.DisableTelemetry = string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"])
        && string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=MarshallDisplayRegistry;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<DisplayRegistryContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<UrlPolicyService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<DeviceAuthService>();
builder.Services.AddScoped<DisplayStatusService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AdminAuthService>();
builder.Services.AddScoped<SeedData>();
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isPublicPath = path.StartsWithSegments("/api")
        || path.StartsWithSegments("/swagger")
        || path.StartsWithSegments("/Error")
        || path.Value?.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase) == true;

    var adminAuth = context.RequestServices.GetRequiredService<AdminAuthService>();
    if (isPublicPath || adminAuth.IsAdmin(context))
    {
        await next();
        return;
    }

    if (!adminAuth.HasEasyAuthPrincipal(context))
    {
        var returnUrl = Uri.EscapeDataString(context.Request.PathBase + context.Request.Path + context.Request.QueryString);
        context.Response.Redirect($"/.auth/login/aad?post_login_redirect_uri={returnUrl}");
        return;
    }

    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    await context.Response.WriteAsync("You are signed in, but you are not in the approved Marshall Korbyt Display admin group.");
});
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();

if (app.Configuration.GetValue("SeedData:Enabled", true))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<SeedData>().SeedAsync();
}

app.Run();

public partial class Program
{
}
