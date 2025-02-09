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

public class PhotoController : ApiControllerBase
{
    private ApplicationDbContext Context { get; }
    private BlobStoragePhotoService PhotoSvc { get; }
    private UserService UserSvc { get; }

    public PhotoController(
        ApplicationDbContext context, 
        BlobStoragePhotoService photoService, 
        UserService userService)
    {
        Context = context;
        PhotoSvc = photoService;
        UserSvc = userService;
    }

    #region Get-Endpoints
    
    [HttpGet]
    [Route("{albumId:int}/{photoName}")]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Results<NotFound, Ok<PhotoDto>, BadRequest<string>, UnauthorizedHttpResult>> GetImageWithDetails(
        long albumId,
        string photoName)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);
        
        // check if the user exists
        if (user == null)
            return TypedResults.Unauthorized();
        
        // check if there are any photos at all, for performance
        if (!Context.Photos.Any()) 
            return TypedResults.NotFound();
        
        if (!Context.PhotoAlbums.Any(pa => pa.Id == albumId))
            return TypedResults
                .BadRequest("The photo album doesn't exist.");
        
        // check if the photo exists and if the user is the owner
        var photo = await Context.Photos
            .FirstOrDefaultAsync(p => 
                p.PhotoAlbumId == albumId && 
                p.Name == photoName &&
                p.PhotoAlbum.UserId == user.Id);

        if (photo == null) 
            return TypedResults.NotFound();
        
        // get the photo uri from the blob storage
        var uri = await PhotoSvc.GetPhoto(
            user.Id,
            photo.Id);
        
        if (uri == null) 
            return TypedResults.NotFound();
        
        var photoDto = new PhotoDto(
            photo.Name,
            photo.PhotoAlbumId,
            uri);
            
        return TypedResults.Ok(photoDto);
    }

    #endregion
    
    #region Delete-Endpoints
    
    //TODO: delete photo
    
    #endregion

    #region Put-Endpoints
    
    [HttpPut]
    [Authorize]
    [RequiredScope("backend.write")]
    public async Task<Results<Ok, BadRequest<string>, UnauthorizedHttpResult>> EditPhotoName(EditNameDto editNameDto)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims, true);
        
        // check if the user exists
        if (user == null) 
            // if user was not able created then the claims had an issue meaning unauthorized
            return TypedResults.Unauthorized();
        
        var oldPhoto = Context.Photos.FirstOrDefault(p => 
            p.Id == editNameDto.OldId && 
            p.PhotoAlbum.UserId == user.Id);
        
        if (oldPhoto == null)
            return TypedResults.BadRequest("Original photo doesn't exist");
        
        // check if the photo name is valid
        if (string.IsNullOrWhiteSpace(editNameDto.NewName))
            return TypedResults.BadRequest("FileName name is required");
        
        if (editNameDto.NewName.Length < 3 || editNameDto.NewName.Length > 63)
            return TypedResults.BadRequest("A FileName name can't be longer than 63 characters or shorter than 3");
        
        // check if the album name doesn't contain invalid characters
        if (!UploadController.ContainerNameRegex.IsMatch(editNameDto.NewName))
            return TypedResults.BadRequest("FileName name contains invalid characters");
        
        // check if the album name is unique
        if (Context.Photos.Any(p => 
                p.Name.Equals(editNameDto.NewName, StringComparison.Ordinal) &&
                p.PhotoAlbumId.Equals(oldPhoto.PhotoAlbumId)))
            return TypedResults.BadRequest("FileName name already exists");
        
        oldPhoto.Name = editNameDto.NewName;
        await Context.SaveChangesAsync();
        return TypedResults.Ok();
    }
    
    #endregion
}