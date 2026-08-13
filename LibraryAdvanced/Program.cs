using LibraryAdvanced.Models;
using LibraryAdvanced.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();

builder.Services.AddSession();

builder.Services.AddDbContext<LibraryAdvancedDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<InterfaceEmailService, EmailService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddAuthentication("CookieAuth") // Đặt Scheme mặc định là "CookieAuth"
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "LibraryUserCookie";
        options.LoginPath = "/Account/Login";            // Tự động chuyển về đây nếu chưa đăng nhập
        options.AccessDeniedPath = "/Account/AccessDenied"; // Tự động chuyển về đây nếu không có quyền
        options.ExpireTimeSpan = TimeSpan.FromHours(8);   // Thời gian sống của Cookie
    });
var app = builder.Build();


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
    pattern: "{controller=Home}/{action=Landing}/{id?}");

app.Run();