using Azure.Storage.Blobs;
using Fan_Website;
using Fan_Website.Infrastructure;
using Fan_Website.Service;
using Fan_Website.Services;
using FanWebsiteAPI.Hubs;
using FanWebsiteAPI.Infrastructure;
using FanWebsiteAPI.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Text;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IEmailSender, EmailSender>();

builder.Configuration
    .AddJsonFile("storagesettings.json", optional: true, reloadOnChange: true);

// Routing
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.AppendTrailingSlash = false;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Fanwebsite")));

// Single Identity registration with all options
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;  
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".AspNetCore.Identity.Application";
});

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>,
    UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>>();

builder.Services.AddScoped<IForum, ForumService>();
builder.Services.AddScoped<IPost, PostService>();
builder.Services.AddScoped<IApplicationUser, ApplicationUserService>();
builder.Services.AddScoped<IUpload, UploadService>();
builder.Services.AddScoped<IScreenshot, ScreenshotService>();
builder.Services.AddScoped<INotificationService, NotificationService>(); 
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddHttpClient();

var storageConnection = builder.Configuration.GetConnectionString("AzureStorageAccount");
if (!string.IsNullOrEmpty(storageConnection))
{
    builder.Services.AddAzureClients(azureBuilder =>
    {
        azureBuilder.AddBlobServiceClient(storageConnection);
        azureBuilder.AddQueueServiceClient(storageConnection);
    });

    var dpContainer = new BlobContainerClient(storageConnection, "data-protection-keys");
    dpContainer.CreateIfNotExists();

    builder.Services.AddDataProtection()
        .PersistKeysToAzureBlobStorage(dpContainer.GetBlobClient("keys.xml"))
        .SetApplicationName("FanWebsiteAPI");
}

builder.Services.AddSignalR();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.UseAllOfToExtendReferenceSchemas();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200", 
            "https://fanwebsiteapi20260228180319-hsfjg3dtchcgebe9.canadacentral-01.azurewebsites.net",
            "https://www.dismino.com",
            "https://dismino.com"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.Run();