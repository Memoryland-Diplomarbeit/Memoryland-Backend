using System.Security.Claims;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
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
    private async Task<User?> CreateUserIfNotExist(IEnumerable<Claim> claims)
    {
        var enumerable = claims.ToList();
        var email = enumerable
            .FirstOrDefault(c => c.Type == "emails")?.Value;
        
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var user = Context.Users.FirstOrDefault(u => u.Email == email);
        
        if (user != null)
            return user;
        
        var username = enumerable
            .FirstOrDefault(c => c.Type == "name")?.Value;
        
        if (string.IsNullOrWhiteSpace(username))
            return null;
        
        user = new User
        {
            Email = email,
            Username = username,
            PhotoAlbums = new HashSet<PhotoAlbum>(),
            Memorylands = new HashSet<Memoryland>()
        };
        
        await Context.AddAsync(user);
        await Context.SaveChangesAsync();
        
        return Context.Users.FirstOrDefault(u => u.Email == email);
    }

    public async Task<User?> CheckIfUserAuthenticated(IEnumerable<Claim> claims, bool createUserIfNotExist = false)
    {
        var claimList = claims.ToList();
        
        var email = claimList
            .FirstOrDefault(c => c.Type.Equals(
                "emails", 
                StringComparison.CurrentCultureIgnoreCase))
            ?.Value;

        if (email == null)
            return null;

        if (createUserIfNotExist)
            return await CreateUserIfNotExist(claimList); // needs all claims to get the name too
        
        var user = await Context.Users
            .FirstOrDefaultAsync(u => u.Username == email);

        return user;
    }
}