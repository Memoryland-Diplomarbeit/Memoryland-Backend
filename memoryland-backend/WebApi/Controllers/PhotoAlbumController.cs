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
    private static string ReservedCharacters { get; } = "!*'();:@&=+$,/?#[]";
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
    public async Task<Results<NotFound, Ok<IEnumerable<PhotoDto>>, BadRequest<string>, UnauthorizedHttpResult>> GetPhotoAlbumImagesWithDetails(long albumId)
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

    [HttpPost]
    [Authorize]
    [RequiredScope("backend.write")]
    public async Task<Results<Created, BadRequest<string>>> CreatePhotoAlbum(string albumName)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims, true);
        
        // check if the user exists
        if (user == null) 
            // if user was not able created then the claims had an issue meaning unauthorized
            throw new UnauthorizedAccessException();
        
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
            pa.Name.Equals(albumName, StringComparison.Ordinal)))
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

    [HttpGet]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Results<Ok<List<PhotoAlbumDto>>, BadRequest<string>, UnauthorizedHttpResult>>
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
                pa.Photos.Select(p => p.Name)))
            .ToList();

        return TypedResults.Ok(photoAlbums);
    }
}