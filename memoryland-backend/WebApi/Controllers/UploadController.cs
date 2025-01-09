using Persistence;

namespace WebApi.Controllers;

// got its own controller because of clean separation
// of concerns and to get the right REST path
public class UploadController : ApiControllerBase
{
    private ApplicationDbContext Context { get; }
    
    public UploadController(ApplicationDbContext context)
    {
        Context = context;
    }
    
    
    //TODO: Consumable upload logic
}