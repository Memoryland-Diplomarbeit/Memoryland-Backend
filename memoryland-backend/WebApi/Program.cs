using System.IdentityModel.Tokens.Jwt;
using BlobStoragePersistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Identity.Web;
using Persistence;
using WebApi.Service;

var builder = WebApplication.CreateBuilder(args);

// build configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddUserSecrets<Program>()
    .AddUserSecrets<BlobStoragePhotoService>();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddSingleton<BlobStoragePhotoService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<UserService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll", 
        b =>
        {
            b
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

#region BlobStorage Initialisation

var storageConnection = builder.Configuration["ConnectionStrings:BlobStorageDefault"];

builder.Services.AddAzureClients(azureBuilder =>
{
    azureBuilder.AddBlobServiceClient(storageConnection);
});

#endregion

#region Authentication and Authorization Initialisation

// This is required to be instantiated before the OpenIdConnectOptions starts getting configured.
// By default, the claims mapping will map claim names in the old format to accommodate older SAML applications.
// For instance, 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' instead of 'roles' claim.
// This flag ensures that the ClaimsIdentity claims collection will be built from the claims in the token
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

// Adds Microsoft Identity platform (AAD v2.0) support to protect this Api
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        options => 
        {
            builder.Configuration.Bind("AzureAdB2C", options);
        },
        options =>
        {
            builder.Configuration.Bind("AzureAdB2C", options);
        });

#endregion

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Load the db connection once, to get the log message, which db I am connected to
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var connection = dbContext.Database.GetDbConnection();
    
    Console.WriteLine($"Connected to database: {connection.Database}");
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseHsts();
app.UseHttpsRedirection();

app.MapGet("hello", () => "Hello World!");

app.MapControllers();

app.Run();
