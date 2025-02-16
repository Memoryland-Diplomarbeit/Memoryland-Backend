using System.Diagnostics;
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

public class MemorylandController : ApiControllerBase
{
    private ApplicationDbContext Context { get; }
    private UserService UserSvc { get; }
    private BlobStoragePhotoService PhotoSvc { get; }
    
    public MemorylandController(
        ApplicationDbContext context, 
        UserService userSvc, 
        BlobStoragePhotoService photoSvc)
    {
        Context = context;
        UserSvc = userSvc;
        PhotoSvc = photoSvc;
        
        //TODO: add memoryland types
    }

    #region Get-Endpoints
    
    [HttpGet]
    [Route("all")]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Ok<List<MemorylandInfoDto>>> GetAllMemorylands()
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);
        
        // check if the user exists
        if (user == null)
            return TypedResults.Ok(new List<MemorylandInfoDto>());
        
        // check if there are any memorylands at all, for performance
        if (!Context.Memorylands.Any()) 
            return TypedResults.Ok(new List<MemorylandInfoDto>());
        
        // check if the memoryland exists and if the user is the owner
        var memorylands = Context.Memorylands
            .Include(m => m.User)
            .Include(m => m.MemorylandType)
            .Where(m => m.UserId == user.Id);

        var memorylandDtos = memorylands
            .Select(m => new MemorylandInfoDto(
                m.Id,
                m.Name,
                m.MemorylandType));
            
        return TypedResults.Ok(await memorylandDtos.ToListAsync());
    }
    
    [HttpGet]
    [RequiredScope("backend.read")]
    public async Task<Results<NotFound, Ok<MemorylandDto>, UnauthorizedHttpResult>> GetCompleteMemoryland([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !Guid.TryParse(token, out var guidToken))
            return TypedResults.Unauthorized();
        
        // check if there are any memorylands at all, for performance
        if (!Context.Memorylands.Any()) 
            return TypedResults.NotFound();
        
        var memoryland = Context.Memorylands
            .Include(m => m.User)
            .Include(m => m.MemorylandType)
            .Include(m => m.MemorylandConfigurations)
            .Include(m => m.MemorylandTokens)
            .FirstOrDefault(m => m.MemorylandTokens.Any(c => c.Token.ToString() == token));
        
        if (memoryland == null)
            return TypedResults.NotFound();
        
        var memorylandToken = memoryland.MemorylandTokens
            .FirstOrDefault(mt => mt.Token.Equals(guidToken));
        
        if (memorylandToken == null)
            return TypedResults.Unauthorized();

        var memorylandDto = new MemorylandDto(
            memoryland.Id,
            memoryland.Name,
            memoryland.MemorylandType,
            []);
        
        var tasks = memoryland.MemorylandConfigurations.Select(async mc =>
        {
            var dbPhoto = Context.Photos.FirstOrDefault(p => p.Id == mc.PhotoId);

            if (dbPhoto != null)
            {
                var photo = await PhotoSvc.GetPhoto(
                    memoryland.UserId, 
                    mc.PhotoId,
                    dbPhoto.Name);
            
                if (photo != null)
                    memorylandDto.MemorylandConfigurations
                        .Add(new MemorylandConfigurationDto(mc.Position, photo));
            }
        }).ToList();
        
        await Task.WhenAll(tasks);
        return TypedResults.Ok(memorylandDto);
    }
    
    [HttpGet]
    [Route("{id:long}/configuration")]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Results<NotFound, Ok<List<MemorylandConfigurationDataDto>>, UnauthorizedHttpResult>> GetMemorylandConfig(long id)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);
        
        // check if the user exists
        if (user == null)
            return TypedResults.Unauthorized();
        
        // check if there are any memorylands at all, for performance
        if (!Context.Memorylands.Any()) 
            return TypedResults.NotFound();
        
        // check if the memoryland exists and if the user is the owner
        var memoryland = Context.Memorylands
            .Include(m => m.MemorylandConfigurations)
            .ThenInclude(memorylandConfiguration => memorylandConfiguration.Photo)
            .FirstOrDefault(m => m.Id == id && m.UserId == user.Id);
        
        if (memoryland == null)
            return TypedResults.NotFound();
        
        var memorylandConfigDtos = memoryland.MemorylandConfigurations
            .Select(mc => new MemorylandConfigurationDataDto(
                mc.Id,
                mc.Position,
                new PhotoDataDto(mc.Photo.Id, mc.Photo.Name)));
        
        return TypedResults.Ok(memorylandConfigDtos.ToList());
    }
    
    [HttpGet]
    [Route("types")]
    public async Task<Ok<List<MemorylandType>>> GetMemorylandTypes()
    {
        return TypedResults.Ok(await Context.MemorylandTypes.ToListAsync());
    }
    
    [HttpGet]
    [Route("{id:long}/token/{isPublic:bool?}")]
    [Authorize]
    [RequiredScope("backend.write", "backend.read")]
    public async Task<Results<NotFound, Ok<TokenDto>, BadRequest<string>, UnauthorizedHttpResult>> GetTokenForMemoryland(long id, bool isPublic = false)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);
        
        // check if the user exists
        if (user == null)
            return TypedResults.Unauthorized();
        
        // check if there are any memorylands at all, for performance
        if (!Context.Memorylands.Any()) 
            return TypedResults.NotFound();
        
        // check if the memoryland exists and if the user is the owner
        var memoryland = Context.Memorylands
            .Include(m => m.User)
            .Include(m => m.MemorylandType)
            .FirstOrDefault(m => m.Id == id && m.UserId == user.Id);

        if (memoryland == null)
            return TypedResults.NotFound();
        
        // Generate new token and delete old one
        var memorylandToken = Context.MemorylandTokens
            .FirstOrDefault(mt => 
                mt.MemorylandId == memoryland.Id &&
                mt.IsInternal != isPublic);

        if (memorylandToken != null)
        {
            Context.MemorylandTokens.Remove(memorylandToken);
            await Context.SaveChangesAsync();
        }

        memorylandToken = new MemorylandToken
        {
            IsInternal = !isPublic,
            MemorylandId = memoryland.Id
        };

        await Context.MemorylandTokens.AddAsync(memorylandToken);
        await Context.SaveChangesAsync();
        
        // Retrieve Token
        var token = new TokenDto(
            memorylandToken.Token.ToString(), 
            isPublic);
        
        // Set security headers
        // -> not cached
        // -> not stored
        // -> disappear from the browser immediately once the tab is closed
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Expires = "0";

        if (string.IsNullOrWhiteSpace(token.Token))
            return TypedResults.BadRequest("Token could not be retrieved.");
        
        return TypedResults.Ok(token);
    }
    
    #endregion
    
    #region Post-Endpoints
    
    [HttpPost]
    [Authorize]
    [Route("{memorylandName}/{memorylandTypeId:long}")]
    [RequiredScope("backend.write")]
    public async Task<Results<Created, BadRequest<string>, UnauthorizedHttpResult>> CreateMemoryland(string memorylandName, long memorylandTypeId)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims, true);
        
        // check if the user exists
        if (user == null) 
            // if user was not able created then the claims had an issue meaning unauthorized
            return TypedResults.Unauthorized();
        
        // check if the album name is valid
        if (string.IsNullOrWhiteSpace(memorylandName))
            return TypedResults.BadRequest("Memoryland name is required");
        
        if (memorylandName.Length > 1024)
            return TypedResults.BadRequest("A Memoryland name can't be longer than 1024 characters");
        
        // check if the album name doesn't contain invalid characters
        if (memorylandName.Any(c => PhotoAlbumController.ReservedCharacters.Contains(c)))
            return TypedResults.BadRequest("Memoryland name contains invalid characters");
        
        // check if the album name is unique
        if (Context.Memorylands.Any(m => 
                m.Name == memorylandName &&
                m.UserId == user.Id))
            return TypedResults.BadRequest("Memoryland name already exists");
        
        if (!Context.MemorylandTypes.Any(mt => mt.Id == memorylandTypeId))
            return TypedResults.BadRequest("Memoryland type does not exist");
        
        var memoryland = new Memoryland
        {
            Name = memorylandName,
            MemorylandTypeId = memorylandTypeId,
            UserId = user.Id
        };

        await Context.Memorylands.AddAsync(memoryland);
        await Context.SaveChangesAsync();
        
        return TypedResults.Created();
    }
    
    [HttpPost]
    [Authorize]
    [Route("{memorylandId:long}")]
    [RequiredScope("backend.write")]
    public async Task<Results<Created, BadRequest<string>, UnauthorizedHttpResult>> CreateMemorylandConfig(
        long memorylandId, 
        [FromBody] PostMemorylandConfigDto postConfDto)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims, true);
        
        // check if the user exists
        if (user == null) 
            // if user was not able created then the claims had an issue meaning unauthorized
            return TypedResults.Unauthorized();
        
        // check if the memoryland exists
        var memoryland = Context.Memorylands
            .Include(m => m.User)
            .Include(m => m.MemorylandType)
            .Include(m => m.MemorylandConfigurations)
            .FirstOrDefault(m => m.Id == memorylandId && m.UserId == user.Id);
        
        if (memoryland is null)
            return TypedResults.BadRequest("Memoryland does not exist");
        
        // check if the position is valid (starts with 0)
        if (memoryland.MemorylandType.PhotoAmount <= postConfDto.Position || postConfDto.Position < 0)
            return TypedResults.BadRequest("Position is invalid");
        
        // check if the photo exists
        if (!Context.Photos.Any(p => 
                p.Id == postConfDto.PhotoId && 
                p.PhotoAlbum.UserId == user.Id))
            return TypedResults.BadRequest("Photo does not exist");
        
        // check if the position already has a photo
        var photoConfig = memoryland.MemorylandConfigurations.FirstOrDefault(mc =>
            mc.Position == postConfDto.Position);
        
        if (photoConfig != null)
        {
            Context.MemorylandConfigurations.Remove(photoConfig);
            await Context.SaveChangesAsync();
        }
        
        var memorylandConf = new MemorylandConfiguration
        {
            Position = postConfDto.Position,
            MemorylandId = memoryland.Id,
            PhotoId = postConfDto.PhotoId
        };

        await Context.MemorylandConfigurations.AddAsync(memorylandConf);
        await Context.SaveChangesAsync();
        
        return TypedResults.Created();
    }
    
    #endregion
    
    #region Delete-Endpoints
    
    [HttpDelete]
    [Route("{memorylandId:long}")]
    [Authorize]
    [RequiredScope("backend.write")]
    public async Task<Results<Ok, UnauthorizedHttpResult>> DeleteMemorylandById(long memorylandId)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);
        
        // check if the user exists
        if (user == null)
            return TypedResults.Unauthorized();
        
        // check if there are any memorylands at all, for performance
        if (!Context.Memorylands.Any()) 
            return TypedResults.Ok();
        
        // check if the memoryland exists and if the user is the owner
        var memoryland = Context.Memorylands
            .FirstOrDefault(m => m.Id == memorylandId && m.UserId == user.Id);
        
        if (memoryland == null)
            return TypedResults.Ok();
        
        Context.Memorylands.Remove(memoryland);
        await Context.SaveChangesAsync();
            
        return TypedResults.Ok();
    }
    
    [HttpDelete]
    [Route("config/{id:long}")]
    [Authorize]
    [RequiredScope("backend.write")]
    public async Task<Results<Ok, UnauthorizedHttpResult>> DeleteMemorylandConfigById(long id)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);
        
        // check if the user exists
        if (user == null)
            return TypedResults.Unauthorized();
        
        // check if there are any memorylands at all, for performance
        if (!Context.Memorylands.Any()) 
            return TypedResults.Ok();
        
        // check if the memoryland exists and if the user is the owner
        var memorylandConfig = Context.MemorylandConfigurations
            .Include(mc => mc.Memoryland)
            .FirstOrDefault(mc => mc.Id == id && mc.Memoryland.UserId == user.Id);
        
        if (memorylandConfig == null)
            return TypedResults.Ok();
        
        Context.MemorylandConfigurations.Remove(memorylandConfig);
        await Context.SaveChangesAsync();
            
        return TypedResults.Ok();
    }
    
    #endregion
    
    #region Put-Endpoints
    
    [HttpPut]
    [Authorize]
    [RequiredScope("backend.write")]
    public async Task<Results<Ok, BadRequest<string>, UnauthorizedHttpResult>> EditMemorylandName(EditNameDto editNameDto)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims, true);
        
        // check if the user exists
        if (user == null) 
            // if user was not able created then the claims had an issue meaning unauthorized
            return TypedResults.Unauthorized();
        
        var oldMemoryland = Context.Memorylands.FirstOrDefault(p => 
            p.Id == editNameDto.OldId && 
            p.UserId == user.Id);
        
        if (oldMemoryland == null)
            return TypedResults.BadRequest("Original memoryland doesn't exist");
        
        // check if the memoryland name is valid
        if (string.IsNullOrWhiteSpace(editNameDto.NewName))
            return TypedResults.BadRequest("Memoryland name is required");
        
        if (editNameDto.NewName.Length > 1024)
            return TypedResults.BadRequest("A Memoryland name can't be longer than 1024 characters");
        
        // check if the album name doesn't contain invalid characters
        if (editNameDto.NewName.Any(c => PhotoAlbumController.ReservedCharacters.Contains(c)))
            return TypedResults.BadRequest("Memoryland name contains invalid characters");
        
        // check if the album name is unique
        if (Context.Memorylands.Any(m => 
                m.Name == editNameDto.NewName &&
                m.UserId == user.Id))
            return TypedResults.BadRequest("Memoryland name already exists");
        
        oldMemoryland.Name = editNameDto.NewName;
        await Context.SaveChangesAsync();
        return TypedResults.Ok();
    }
    
    #endregion
    
}
