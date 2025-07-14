using NphiesBridge.Web.Services;
using NphiesBridge.Web.Services.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

// ? ADD SESSION SUPPORT
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "NcdoxsProviderSession";
});

// Add HTTP client for API calls
builder.Services.AddHttpClient("NphiesAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7262/api/"); // Your API URL
    client.DefaultRequestHeaders.Add("User-Agent", "NCDOXS-Provider-Portal");
});

// Register Auth service
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

// ? ADD SESSION MIDDLEWARE (IMPORTANT: Before UseRouting)
app.UseSession();

app.UseRouting();
app.UseAuthorization();

// Set default route to Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();