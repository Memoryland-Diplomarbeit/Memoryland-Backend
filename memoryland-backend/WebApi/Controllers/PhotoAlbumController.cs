using BlobStoragePersistence;
using Core.DTO;
using Core.Entities;
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
    public static string ReservedCharacters { get; } = "!*'();:@&=+$,/?#[]";
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

    #region Get-Endpoints
    
    [HttpGet]
    [Route("{albumId:long}")]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Results<NotFound, Ok<IEnumerable<PhotoDto>>, UnauthorizedHttpResult>> GetPhotoAlbumImagesWithDetails(long albumId)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);
        
        // check if the user exists
        if (user == null)
            return TypedResults.Unauthorized();
        
        // check if there are any photos at all, for performance
        if (!Context.Photos.Any()) 
            return TypedResults.NotFound();
        
        if (!Context.PhotoAlbums.Any(pa => pa.Id == albumId && pa.UserId == user.Id))
            return TypedResults.NotFound();
        
        // check if the photo exists and if the user is the owner
        var photoAlbum = await Context.PhotoAlbums
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.Id == albumId && p.UserId == user.Id);

        if (photoAlbum == null || photoAlbum.Photos.Count == 0) 
            return TypedResults.NotFound();

        var photos = new List<PhotoDto>();

        foreach (var photo in photoAlbum.Photos)
        {
            // get the photo uri from the blob storage
            var uri = await PhotoSvc.GetPhoto(
                user.Id,
                photo.Id,
                photo.Name);
        
            if (uri == null) 
                return TypedResults.NotFound();

            photos.Add(new PhotoDto(
                photo.Name,
                photo.PhotoAlbumId,
                uri.AbsoluteUri));
        }
        
        photos.Sort((a, b) => 
            string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            
        return TypedResults.Ok(photos.AsEnumerable());
    }

    [HttpGet]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Ok<List<PhotoAlbumDto>>>
        GetPhotoAlbumsData()
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);
        
        // check if the user exists
        if (user == null)
            return TypedResults.Ok(new List<PhotoAlbumDto>());
            // would throw an exception if the user were not allowed

        var photoAlbums = Context.PhotoAlbums
            .Where(pa => pa.UserId == user.Id)
            .Include(pa => pa.Photos)
            .Select(pa => new PhotoAlbumDto(
                pa.Id, 
                pa.Name, 
                pa.Photos.Select(p => new PhotoDataDto(p.Id, p.Name))))
            .ToList();

        return TypedResults.Ok(photoAlbums);
    }
    
    #endregion

    #region Post-Endpoints

    [HttpPost]
    [Authorize]
    [Route("{albumName}")]
    [RequiredScope("backend.write")]
    public async Task<Results<Created, BadRequest<string>, UnauthorizedHttpResult>> CreatePhotoAlbum(string albumName)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims, true);
        
        // check if the user exists
        if (user == null) 
            // if user was not able created then the claims had an issue meaning unauthorized
            return TypedResults.Unauthorized();
        
        // check if the album name is valid
        if (string.IsNullOrWhiteSpace(albumName))
            return TypedResults.BadRequest("Album name is required");
        
        if (albumName.Length > 1024)
            return TypedResults.BadRequest("An Album name can't be longer than 1024 characters");
        
        // check if the album name doesn't contain invalid characters
        if (albumName.Any(c => ReservedCharacters.Contains(c)))
            return TypedResults.BadRequest("Album name contains invalid characters");
        
        // check if the album name is unique
        if (Context.PhotoAlbums.Any(pa => 
                pa.Name == albumName &&
                pa.UserId == user.Id))
            return TypedResults.BadRequest("Album name already exists");
        
        var photoAlbum = new PhotoAlbum
        {
            Name = albumName,
            UserId = user.Id
        };

        await Context.PhotoAlbums.AddAsync(photoAlbum);
        await Context.SaveChangesAsync();
        
        return TypedResults.Created();
    }
    
    #endregion
    
    #region Delete-Endpoints
    
    [HttpDelete]
    [Route("{photoAlbumId:long}")]
    [Authorize]
    [RequiredScope("backend.write")]
    public async Task<Results<Ok, UnauthorizedHttpResult>> DeletePhotoAlbumById(long photoAlbumId)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);
        
        // check if the user exists
        if (user == null)
            return TypedResults.Unauthorized();
        
        // check if there are any photo-albums at all, for performance
        if (!Context.PhotoAlbums.Any()) 
            return TypedResults.Ok();
        
        // check if the photo-album exists and if the user is the owner
        var photoAlbum = Context.PhotoAlbums
            .Include(pa => pa.Photos)
            .FirstOrDefault(pa => pa.Id == photoAlbumId && pa.UserId == user.Id);
        
        if (photoAlbum == null)
            return TypedResults.Ok();
        
        await PhotoSvc.DeletePhotos(
            user.Id, 
            photoAlbum.Photos.ToList());
        
        Context.PhotoAlbums.Remove(photoAlbum);
        await Context.SaveChangesAsync();
            
        return TypedResults.Ok();
    }

    #endregion
    
    #region Put-Endpoints
    
    [HttpPut]
    [Authorize]
    [RequiredScope("backend.write")]
    public async Task<Results<Ok, BadRequest<string>, UnauthorizedHttpResult>> EditPhotoAlbumName(EditNameDto editNameDto)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims, true);
        
        // check if the user exists
        if (user == null) 
            // if user was not able created then the claims had an issue meaning unauthorized
            return TypedResults.Unauthorized();
        
        var oldAlbum = Context.PhotoAlbums.FirstOrDefault(pa => 
            pa.Id == editNameDto.OldId &&
            pa.UserId == user.Id);
        
        if (oldAlbum == null)
            return TypedResults.BadRequest("Original album doesn't exist");
        
        // check if the album name is valid
        if (string.IsNullOrWhiteSpace(editNameDto.NewName))
            return TypedResults.BadRequest("Album name is required");
        
        if (editNameDto.NewName.Length > 1024)
            return TypedResults.BadRequest("An Album name can't be longer than 1024 characters");
        
        // check if the album name doesn't contain invalid characters
        if (editNameDto.NewName.Any(c => ReservedCharacters.Contains(c)))
            return TypedResults.BadRequest("Album name contains invalid characters");
        
        // check if the album name is unique
        if (Context.PhotoAlbums.AsEnumerable()
            .Any(pa => 
                pa.Name == editNameDto.NewName &&
                pa.UserId == user.Id))
            return TypedResults.BadRequest("Album name already exists");
        
        oldAlbum.Name = editNameDto.NewName;
        await Context.SaveChangesAsync();
        return TypedResults.Ok();
    }
    
    #endregion
}