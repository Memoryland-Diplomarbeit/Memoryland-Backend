using System.Security.Claims;
using Core.Entities;
using Persistence;

namespace WebApi.Service;

public class UserService
{
    private ApplicationDbContext Context { get; }
    
    public UserService(ApplicationDbContext context)
    {
        Context = context;
    }

    /// <summary>
    /// Creates user if both the email and the
    /// username exist in the claims and returns
    /// if the creation was successful
    /// </summary>
    /// <param name="claims"></param>
    /// <returns></returns>
    public async Task<bool> CreateUserIfNotExist(IEnumerable<Claim> claims)
    {
        var enumerable = claims.ToList();
        var email = enumerable
            .FirstOrDefault(c => c.Type == "email")?.Value;
        
        if (string.IsNullOrEmpty(email))
            return false;

        if (Context.Users.Any(u => u.Email == email))
            return true;
        
        var username = enumerable
            .FirstOrDefault(c => c.Type == "name")?.Value;
        
        if (string.IsNullOrEmpty(username))
            return false;
        
        var user = new User
        {
            Email = email,
            Username = username,
            PhotoAlbums = new HashSet<PhotoAlbum>(),
            Memorylands = new HashSet<Memoryland>()
        };
        
        await Context.AddAsync(user);
        await Context.SaveChangesAsync();
        return true;
    }
}