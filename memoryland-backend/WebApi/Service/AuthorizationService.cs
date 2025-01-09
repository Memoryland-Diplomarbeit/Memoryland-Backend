using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace WebApi.Service;

public class AuthorizationService
{
    private ApplicationDbContext Context { get; }
    
    public AuthorizationService(ApplicationDbContext context)
    {
        Context = context;
    }
    
    public async Task<bool> IsAuthorized(
        long memorylandId, 
        Guid memorylandToken)
    {
        var memoryland = await Context.Memorylands
            .Include(m => m.MemorylandTokens)
            .FirstOrDefaultAsync(m => m.Id == memorylandId);
        
        // memoryland not found -> unauthorized
        if (memoryland == null)
            return false;

        var token = Context.MemorylandTokens
            .FirstOrDefault(mt => 
                mt.Token == memorylandToken && 
                mt.MemorylandId == memorylandId);

        return token != null;
    }
}