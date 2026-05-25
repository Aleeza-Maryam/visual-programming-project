
using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// =========================================================
// DATABASE CONNECTION FOR .NET 10
// =========================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// For .NET 10 - Using Official MySQL Package
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString)  // UseMySQL (not UseMySql)
);

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add custom services
builder.Services.AddScoped<IAIRecommendationService, AIRecommendationService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IItineraryGenerator, ItineraryGenerator>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();

// Add HTTP Client for APIs
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed database with sample data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbInitializer.InitializeAsync(dbContext);
}

app.Run();