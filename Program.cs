using EPEAPI.Models;
using EPEAPI.Services;
using EPEAPI.TokenAuthentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("sql-cons");

builder.Services.AddDbContext<EpeDbContext>(options =>
    options.UseSqlServer(connectionString)
);

builder.Services.Configure<UrlBaseSettings>(
    builder.Configuration.GetSection("UrlBase"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Policy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:80",
                "http://localhost",
                "http://localhost/epe2api",
                "http://localhost/epe2dev",
                "http://192.168.136.64",
                "https://apps.fastlogistics.com.ph/epe2dev"
                )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();

        //policy
        //    .AllowAnyOrigin()
        //    .AllowAnyMethod()
        //    .AllowAnyHeader()
        //    .AllowCredentials();
    });
});

// SESSION
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None; // Changed to None for cross-origin
    options.Cookie.Name = ".AspNetCore.Session";
});

//Token manager(optional now)
builder.Services.AddScoped<ITokenManager, TokenManager>();

// In Program.cs
builder.Services.AddScoped<EmailApprovalTokenService>();

// Email Notification Service
builder.Services.AddHttpClient<EmailNotificationService>();
builder.Services.AddScoped<EmailNotificationService>();

// File Upload Service
builder.Services.AddScoped<FileUploadService>();

var app = builder.Build();

app.UseCors("Policy");

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles();

app.Run();
