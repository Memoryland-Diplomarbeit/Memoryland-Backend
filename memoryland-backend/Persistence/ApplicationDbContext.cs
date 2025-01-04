using System.Diagnostics;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly ILogger<ApplicationDbContext> _logger;

    public ApplicationDbContext(ILogger<ApplicationDbContext> logger)
    {
        _logger = logger;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<PhotoAlbum> PhotoAlbums { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<Memoryland> Memorylands { get; set; }
    public DbSet<MemorylandType> MemorylandTypes { get; set; }
    public DbSet<MemorylandConfiguration> MemorylandConfigurations { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;
        
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ApplicationDbContext>()
            .Build();
        
        var useLocalDbIsValidValue = bool.TryParse(
            configuration["UseLocalDb"], 
            out var useLocalDb);

        if (useLocalDbIsValidValue && useLocalDb)
        {
            _logger.Log(LogLevel.Information, "Using 'DefaultLocal' connection string");
            optionsBuilder
                .LogTo(
                    msg => Debug.WriteLine(msg),
                    LogLevel.Debug,
                    DbContextLoggerOptions.SingleLine | DbContextLoggerOptions.UtcTime)
                .UseNpgsql(configuration["ConnectionStrings:DefaultLocal"]);
        }
        else
        {
            _logger.Log(LogLevel.Information, "Using 'Default' connection string");
            optionsBuilder
                .LogTo(
                    msg => Debug.WriteLine(msg),
                    LogLevel.Debug,
                    DbContextLoggerOptions.SingleLine | DbContextLoggerOptions.UtcTime)
                .UseNpgsql(configuration["ConnectionStrings:Default"]); 
        }
    }
}