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
    [Route("/all")]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Results<Ok<List<MemorylandInfoDto>>, UnauthorizedHttpResult>> GetAllMemorylands()
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);
        
        // check if the user exists
        if (user == null)
            return TypedResults.Unauthorized();
        
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
    [Route("/{id:int}")]
    [RequiredScope("backend.read")]
    public async Task<Results<NotFound, Ok<MemorylandDto>, UnauthorizedHttpResult>> GetCompleteMemoryland(int id)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);

        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        // check if the user has an authorization header
        if (authorizationHeader == null || !authorizationHeader.StartsWith("Bearer "))
            return TypedResults.Unauthorized();

        var token = authorizationHeader.Replace("Bearer ", "");
        
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
            .FirstOrDefault(m => m.Id == id);
        
        if (memoryland == null)
            return TypedResults.NotFound();
        
        var memorylandToken = memoryland.MemorylandTokens
            .FirstOrDefault(mt => mt.Token.Equals(guidToken));
        
        if (memorylandToken == null)
            return TypedResults.Unauthorized();
        
        // check if the user exists
        if (memorylandToken.IsInternal && user == null)
            return TypedResults.Unauthorized();

        var memorylandDto = new MemorylandDto(
            memoryland.Id,
            memoryland.Name,
            memoryland.MemorylandType,
            []);
        
        var tasks = memoryland.MemorylandConfigurations.Select(async mc =>
        {
            var photo = await PhotoSvc.GetPhoto(memoryland.UserId, mc.PhotoId);
            
            if (photo != null)
                memorylandDto.MemorylandConfigurations
                    .Add(new MemorylandConfigurationDto(mc.Position, photo));
        }).ToList();
        
        await Task.WhenAll(tasks);
        return TypedResults.Ok(memorylandDto);
    }
    
    [HttpGet]
    [Route("/{id:int}/configuration")]
    [RequiredScope("backend.read")]
    public async Task<Results<NotFound, Ok<List<MemorylandConfigurationDto>>, UnauthorizedHttpResult>> GetMemorylandConfig(int id)
    {
        var result = await GetCompleteMemoryland(id);

        return result.Result switch
        {
            Ok<MemorylandDto> { Value: null } => TypedResults.NotFound(),
            Ok<MemorylandDto> ok => TypedResults.Ok(ok.Value.MemorylandConfigurations),
            Microsoft.AspNetCore.Http.HttpResults.NotFound => TypedResults.NotFound(),
            UnauthorizedHttpResult => TypedResults.Unauthorized(),
            _ => throw new UnreachableException("Should not have gotten here")
        };
    }
    
    [HttpGet]
    [Route("/types")]
    public async Task<Ok<List<MemorylandType>>> GetMemorylandTypes()
    {
        return TypedResults.Ok(await Context.MemorylandTypes.ToListAsync());
    }
    
    [HttpGet]
    [Route("/{id:int}/token/{isPublic:bool?}")]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Results<NotFound, Ok<TokenDto>, BadRequest<string>, UnauthorizedHttpResult>> GetTokenForMemoryland(int id, bool isPublic = false)
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
                mt.MemorylandId.Equals(memoryland.Id) &&
                mt.IsInternal.Equals(!isPublic));

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
    [Route("/{memorylandName}/{memorylandTypeId:long}")]
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
                m.Name.Equals(memorylandName, StringComparison.Ordinal) &&
                m.UserId.Equals(user.Id)))
            return TypedResults.BadRequest("Memoryland name already exists");
        
        if (!Context.MemorylandTypes.Any(mt => mt.Id.Equals(memorylandTypeId)))
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
    [Route("/{memorylandId:long}")]
    [RequiredScope("backend.write")]
    public async Task<Results<Created, BadRequest<string>, UnauthorizedHttpResult>> CreateMemoryland(
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
        if (memoryland.MemorylandType.PhotoAmount >= postConfDto.Position || postConfDto.Position < 0)
            return TypedResults.BadRequest("Position is invalid");
        
        // check if the position already has a photo
        if (memoryland.MemorylandConfigurations.Any(mc => 
                mc.Position.Equals(postConfDto.Position)))
            return TypedResults.BadRequest("Position already has a photo");
        
        // check if the photo exists
        if (!Context.Photos.Any(p => 
                p.Id.Equals(postConfDto.PhotoId) && 
                p.PhotoAlbum.UserId.Equals(user.Id)))
            return TypedResults.BadRequest("Photo does not exist");
        
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
    [Route("/{id:long}")]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Results<Ok, UnauthorizedHttpResult>> DeleteMemorylandById(long id)
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
            .FirstOrDefault(m => m.Id == id);
        
        if (memoryland == null)
            return TypedResults.Ok();
        
        if (memoryland.UserId != user.Id)
            return TypedResults.Unauthorized();
        
        Context.Memorylands.Remove(memoryland);
        await Context.SaveChangesAsync();
            
        return TypedResults.Ok();
    }
    
    [HttpDelete]
    [Route("/{id:long}")]
    [Authorize]
    [RequiredScope("backend.read")]
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
            .FirstOrDefault(mc => mc.Id == id);
        
        if (memorylandConfig == null)
            return TypedResults.Ok();
        
        if (!memorylandConfig.Memoryland.UserId.Equals(user.Id))
            return TypedResults.Unauthorized();
        
        Context.MemorylandConfigurations.Remove(memorylandConfig);
        await Context.SaveChangesAsync();
            
        return TypedResults.Ok();
    }
    
    #endregion
    
    #region Put-Endpoints
    
    //TODO: rename memoryland
    
    //TODO: update memoryland configuration
    
    #endregion
    
}
