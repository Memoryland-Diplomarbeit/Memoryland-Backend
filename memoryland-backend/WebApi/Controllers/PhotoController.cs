using Persistence;

namespace WebApi.Controllers;

public class PhotoController : ApiControllerBase
{
    private ApplicationDbContext Context { get; }

    public PhotoController(ApplicationDbContext context)
    {
        Context = context;
    }
}