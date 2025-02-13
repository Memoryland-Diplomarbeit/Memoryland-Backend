using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Persistence;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<PhotoAlbum> PhotoAlbums { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<Memoryland> Memorylands { get; set; }
    public DbSet<MemorylandToken> MemorylandTokens { get; set; }
    public DbSet<MemorylandType> MemorylandTypes { get; set; }
    public DbSet<MemorylandConfiguration> MemorylandConfigurations { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<ApplicationDbContext>()
            .Build();
        
        var useLocalDbIsValidValue = bool.TryParse(
            configuration["UseLocalDb"], 
            out var useLocalDb);
        
        if (useLocalDbIsValidValue && useLocalDb)
        {
            Console.WriteLine("Using the DefaultLocal connection string.");
            optionsBuilder
                .LogTo(
                    msg => Debug.WriteLine(msg),
                    LogLevel.Debug,
                    DbContextLoggerOptions.SingleLine | DbContextLoggerOptions.UtcTime)
                .UseNpgsql(configuration["ConnectionStrings:DefaultLocal"]);
        }
        else
        {
            Console.WriteLine("Using the Default connection string.");
            optionsBuilder
                .LogTo(
                    msg => Debug.WriteLine(msg),
                    LogLevel.Debug,
                    DbContextLoggerOptions.SingleLine | DbContextLoggerOptions.UtcTime)
                .UseNpgsql(configuration["ConnectionStrings:Default"]); 
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes()
                     .Where(t =>
                         t.ClrType.GetProperties()
                             .Any(p => p.CustomAttributes
                                 .Any(a => a.AttributeType == typeof(DatabaseGeneratedAttribute)))))
        {
            foreach (var property in entity.ClrType.GetProperties()
                         .Where(p =>
                             p.PropertyType == typeof(Guid) &&
                             p.CustomAttributes
                                 .Any(a => a.AttributeType == typeof(DatabaseGeneratedAttribute))))
            {
                modelBuilder
                    .Entity(entity.ClrType)
                    .Property(property.Name)
                    .HasDefaultValueSql("gen_random_uuid()");
            }
        }
    }
}