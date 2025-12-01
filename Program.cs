using AuthServices.Api;
using AuthServices.Api.Controllers;
using AuthServices.Api.Data;
using AuthServices.Api.Models;
using AuthServices.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Builder;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
var emailUser = Environment.GetEnvironmentVariable("EMAIL_USER");
var emailPass = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
if (string.IsNullOrEmpty(jwtKey))
{
    Console.WriteLine("❌ Error: JWT_KEY Environment Variable is not set!");
    throw new Exception("JWT_KEY Environment Variable is not set!");
}

if (string.IsNullOrEmpty(emailUser) || string.IsNullOrEmpty(emailPass))
{
    Console.WriteLine("❌ Error: Email environment variables (EMAIL_USER / EMAIL_PASSWORD) are not set!");
    throw new Exception("Email environment variables are missing!");
}
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

builder.Services.AddDbContext<AppDbContext>(opations=>opations.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<ApplicationUser,IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

var emailSettings =new EmailSettings
{
    SmtpServer = builder.Configuration["EmailSettings:SmtpServer"],
    Port = int.Parse(builder.Configuration["EmailSettings:Port"]),
  Password= emailPass,
   
  Email= emailUser,

};
builder.Services.AddSingleton<EmailService>(sp =>
new EmailService(emailSettings));




var jwtSettings = builder.Configuration.GetSection("jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;      // read cookies
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme; // save cookies (VERY IMPORTANT)
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;           // redirect to Google
})

.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
})
.AddGoogle(options =>
{
    options.ClientId =Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
    options.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
    options.CallbackPath = "/api/ExternalAuth/GoogleResponse";
    options.SaveTokens = true;
})
// نضيف JwtBearer كذلك / علشان الـ APIs تحمي بـ JWT بعد إصدار التوكن
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCookiePolicy();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();




app.Run();
