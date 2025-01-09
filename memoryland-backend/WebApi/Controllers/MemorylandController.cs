using Persistence;

namespace WebApi.Controllers;

public class MemorylandController : ApiControllerBase
{
    private ApplicationDbContext Context { get; }
    
    public MemorylandController(ApplicationDbContext context)
    {
        Context = context;
    }
    
}