using NphiesBridge.Web.Services;
using NphiesBridge.Web.Services.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

// ADD SESSION SUPPORT
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "NcdoxsProviderSession";
});

// API Base URL Configuration
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7262";

// Add HTTP client for existing API calls (named client)
builder.Services.AddHttpClient("NphiesAPI", client =>
{
    client.BaseAddress = new Uri($"{apiBaseUrl}/api/");
    client.DefaultRequestHeaders.Add("User-Agent", "NCDOXS-Provider-Portal");
});

// Add ICD Mapping API Service (typed client)
builder.Services.AddHttpClient<IcdMappingApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // For potentially long AI processing
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "NCDOXS-ICD-Mapping");
});

// Add Service Mapping API Service (typed client)
builder.Services.AddHttpClient<ServiceMappingApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // For potentially long service mapping processing
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "NCDOXS-Service-Mapping");
});

// Register app services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ExcelTemplateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ADD SESSION MIDDLEWARE (IMPORTANT: Before UseRouting)
app.UseSession();

app.UseRouting();
app.UseAuthorization();

// Add ICD Mapping routes
app.MapControllerRoute(
    name: "icd-mapping",
    pattern: "IcdMapping/{action=Index}/{id?}",
    defaults: new { controller = "IcdMapping", action = "Index" }
);

// Add Service Mapping routes (default to MappingSetup page)
app.MapControllerRoute(
    name: "service-mapping",
    pattern: "ServiceMapping/{action=Index}/{id?}",
    defaults: new { controller = "ServiceMapping", action = "Index" }
);

// Set default route to Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}"
);

app.Run();