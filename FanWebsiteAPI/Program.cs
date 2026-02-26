using Fan_Website;
using Fan_Website.Infrastructure;
using Fan_Website.Service;
using Fan_Website.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);

// --------------------
// Services
// --------------------

// Routing
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.AppendTrailingSlash = true;
});

// Controllers (API only)
builder.Services.AddControllers();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Fanwebsite")));

// Identity (cleaned up, single registration)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>,
    UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>>();

// Application services
builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IForum, ForumService>();
builder.Services.AddScoped<IPost, PostService>();
builder.Services.AddScoped<IApplicationUser, ApplicationUserService>();
builder.Services.AddScoped<IUpload, UploadService>();
builder.Services.AddScoped<IScreenshot, ScreenshotService>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Azure Storage
builder.Services.AddAzureClients(azureBuilder =>
{
    azureBuilder.AddBlobServiceClient(
        builder.Configuration["ConnectionStrings:AzureStorageAccount:blob"]);

    azureBuilder.AddQueueServiceClient(
        builder.Configuration["ConnectionStrings:AzureStorageAccount:queue"]);
});

// Auth
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (Angular / mobile)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FanWebsiteAPI", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// --------------------
// Middleware
// --------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("FanWebsiteAPI");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();