using System.Security.Authentication;
using BlobStoragePersistence;
using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web.Resource;
using Persistence;

namespace WebApi.Controllers;

public class PhotoController : ApiControllerBase
{
    private ApplicationDbContext Context { get; }
    private BlobStoragePhotoService PhotoSvc { get; }

    private IHttpContextAccessor ContextAccessor { get; }

    public PhotoController(
        ApplicationDbContext context, 
        BlobStoragePhotoService photoService, 
        IHttpContextAccessor contextAccessor)
    {
        Context = context;
        PhotoSvc = photoService;
        ContextAccessor = contextAccessor;
    }
    
    [HttpGet]
    [Route("{albumId:int}/{photoName}")]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Results<NotFound, Ok<PhotoDto>, BadRequest<string>, UnauthorizedHttpResult>> GetImageWithDetails(
        int albumId,
        string photoName)
    {
        // check if the user is authenticated without errors
        var email = User.Claims
            .FirstOrDefault(c => c.Type.Equals(
                "email", 
                StringComparison.CurrentCultureIgnoreCase))
            ?.Value;
        
        if (email == null)
            throw new AuthenticationException("User email not found.");
        
        var user = await Context.Users
            .FirstOrDefaultAsync(u => 
                u.Username.Equals(email));
        
        // check if the user exists and if there are any
        // photos at all, for performance
        if (user == null || !Context.Photos.Any()) 
            return TypedResults.NotFound();
        
        if (!Context.PhotoAlbums.Any(pa => pa.Id == albumId))
            return TypedResults
                .BadRequest("The photo album doesn't exist.");
        
        // check if the photo exists and if the user is the owner
        var photo = await Context.Photos
            .Include(p => p.PhotoAlbum.User)
            .FirstOrDefaultAsync(p => 
                p.PhotoAlbumId == albumId && p.Name == photoName);

        if (photo == null) return TypedResults.NotFound();
        
        if (!photo.PhotoAlbum.User.Email.Equals(
                email, 
                StringComparison.CurrentCultureIgnoreCase))
            return TypedResults.Unauthorized();
        
        // get the photo uri from the blob storage
        var uri = await PhotoSvc.GetPhoto(
            user.Id,
            photo.PhotoAlbum.Name,
            photo.Name);
        
        if (uri == null) 
            return TypedResults.NotFound();
        
        var photoDto = new PhotoDto(
            photo.Name,
            photo.PhotoAlbumId,
            uri);
            
        return TypedResults.Ok(photoDto);
    }
}