using System.Text.RegularExpressions;
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

// got its own controller because of clean separation
// of concerns and to get the right REST path
public class UploadController : ApiControllerBase
{
    // ^: start of string
    // (?!-): ensures string does not start with '-'
    // (?!.*--): ensures string does not contain '--' anywhere
    // [a-z0-9]: first character must be lowercase letter or digit
    // ([a-z0-9-]{1,61}[a-z0-9])?: optional group:
    //     - contains between 1 and 61 characters between first and last letter (3-63 in total)
    //     - must end with letter or digit
    //     - ensures string does not end with '-'
    // $: End of the string.
    public static readonly Regex ContainerNameRegex = new(
        "^(?!-)(?!.*--)[a-z0-9]([a-z0-9-]{1,61}[a-z0-9])?$",
        RegexOptions.Compiled
    );
    private ApplicationDbContext Context { get; }
    private UserService UserSvc { get; }
    private BlobStoragePhotoService PhotoSvc { get; }
    
    public UploadController(
        ApplicationDbContext context, 
        UserService userService,
        BlobStoragePhotoService photoService)
    {
        Context = context;
        UserSvc = userService;
        PhotoSvc = photoService;
    }

    [HttpPost]
    [Route("photo")]
    [Authorize]
    [RequiredScope("backend.write")]
    public async Task<Results<Created, BadRequest<string>, UnauthorizedHttpResult>> UploadPhoto([FromForm] PostPhotoDto<IFormFile> photoDto)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims, true);
        
        // check if the user exists
        if (user == null) 
            // if user was not able created then the claims had an issue meaning unauthorized
            return TypedResults.Unauthorized();
        
        // check if the photo album exists
        if (!Context.PhotoAlbums.Any(pa => pa.Id.Equals(photoDto.PhotoAlbumId)))
            return TypedResults.BadRequest("The photo album doesn't exist.");
        
        // check if the photo is empty
        if (photoDto.Photo.Length == 0)
            return TypedResults.BadRequest("No image file provided.");
        
        // check if the photo name is valid
        if (string.IsNullOrWhiteSpace(photoDto.FileName))
            return TypedResults.BadRequest("FileName name is required");
        
        if (photoDto.FileName.Length < 3 || photoDto.FileName.Length > 63)
            return TypedResults.BadRequest("A FileName name can't be longer than 63 characters or shorter than 3");
        
        // check if the album name doesn't contain invalid characters
        if (!ContainerNameRegex.IsMatch(photoDto.FileName))
            return TypedResults.BadRequest("FileName name contains invalid characters");
        
        // check if the file is unique in album
        if (Context.Photos.Any(p => 
                p.Name.Equals(photoDto.FileName, StringComparison.Ordinal) &&
                p.PhotoAlbumId.Equals(photoDto.PhotoAlbumId)))
            return TypedResults.BadRequest("FileName name already exists");
        
        // convert the photo to a byte array
        byte[] photoData;
        using (var memoryStream = new MemoryStream())
        {
            await photoDto.Photo.CopyToAsync(memoryStream);
            photoData = memoryStream.ToArray();
        }
        
        // create the photo entity
        var photo = new Photo
        {
            Name = photoDto.FileName,
            PhotoAlbumId = photoDto.PhotoAlbumId
        };
        
        await Context.AddAsync(photo);
        await Context.SaveChangesAsync();
        
        var album = Context.PhotoAlbums
            .Include(photoAlbum => photoAlbum.User)
            .FirstOrDefault(pa => pa.Id.Equals(photo.PhotoAlbumId));
        
        await PhotoSvc.UploadPhoto(
            album!.User.Id,
            photo.Id,
            photo.Name,
            photoData);
        
        return TypedResults.Created();
    }

    //TODO: resumable upload for albums
}