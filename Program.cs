using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;
using SmartRoomFinder.Services.Interfaces;
using SmartRoomFinder.Services.Implementations;
using SmartRoomFinder.Hubs;
using SmartRoomFinder.Services;
using PayOS;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register SQLite Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/Auth/Login?error=access_denied");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

// Register Business Services
builder.Services.AddScoped<IPasswordHasher<UserModel>, PasswordHasher<UserModel>>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAIAssistantService, AIAssistantService>();

// Configure Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserConnectionManager, UserConnectionManager>();
builder.Services.AddHttpContextAccessor();

// Add PayOS
PayOSClient payOS = new PayOSClient(
    builder.Configuration["PayOS:ClientId"],
    builder.Configuration["PayOS:ApiKey"],
    builder.Configuration["PayOS:ChecksumKey"]
);
builder.Services.AddSingleton(payOS);
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Add Hosted Services
builder.Services.AddHostedService<DepositExpiryWorker>();

// Add Memory Cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Auto-migrate database & seed data on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    DbSeeder.Seed(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SmartRoomFinder.Middlewares.MaintenanceMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
