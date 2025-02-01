using BlobStoragePersistence;
using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web.Resource;
using Persistence;
using WebApi.Service;

namespace WebApi.Controllers;

public class PhotoAlbumController : ApiControllerBase
{
    private ApplicationDbContext Context { get; }
    private BlobStoragePhotoService PhotoSvc { get; }
    private UserService UserSvc { get; }

    public PhotoAlbumController(
        ApplicationDbContext context, 
        BlobStoragePhotoService photoService, 
        UserService userService)
    {
        Context = context;
        PhotoSvc = photoService;
        UserSvc = userService;
    }
    
    [HttpGet]
    [Route("{albumId:int}")]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Results<NotFound, Ok<IEnumerable<PhotoDto>>, BadRequest<string>, UnauthorizedHttpResult>> GetImageWithDetails(long albumId)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);
        
        // check if the user exists and if there are any
        // photos at all, for performance
        if (user == null || !Context.Photos.Any()) 
            return TypedResults.NotFound();
        
        if (!Context.PhotoAlbums.Any(pa => pa.Id == albumId))
            return TypedResults.NotFound();
        
        // check if the photo exists and if the user is the owner
        var photoAlbum = await Context.PhotoAlbums
            .Include(p => p.User)
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.Id == albumId);

        if (photoAlbum == null || photoAlbum.Photos.Count == 0) 
            return TypedResults.NotFound();
        
        if (!photoAlbum.User.Email.Equals(
                user.Email, 
                StringComparison.CurrentCultureIgnoreCase))
            return TypedResults.Unauthorized();

        var photos = new List<PhotoDto>();

        foreach (var photo in photoAlbum.Photos)
        {
            // get the photo uri from the blob storage
            var uri = await PhotoSvc.GetPhoto(
                user.Id,
                photo.PhotoAlbum.Name,
                photo.Name);
        
            if (uri == null) 
                return TypedResults.NotFound();

            photos.Add(new PhotoDto(
                photo.Name,
                photo.PhotoAlbumId,
                uri));
        }
        
        photos.Sort((a, b) => 
            string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            
        return TypedResults.Ok(photos.AsEnumerable());
    }
}