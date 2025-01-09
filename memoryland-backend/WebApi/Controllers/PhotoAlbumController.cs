using Persistence;

namespace WebApi.Controllers;

public class PhotoAlbumController : ApiControllerBase
{
    private ApplicationDbContext Context { get; }
    
    public PhotoAlbumController(ApplicationDbContext context)
    {
        Context = context;
    }
    
}