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
    }
    
    [HttpGet]
    [Route("/all")]
    [Authorize]
    [RequiredScope("backend.read")]
    public async Task<Results<NotFound, Ok<List<MemorylandInfoDto>>, BadRequest<string>, UnauthorizedHttpResult>> GetAllMemorylands()
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
            
        return TypedResults.Ok(memorylandDtos.ToList());
    }
    
    [HttpGet]
    [Route("/{id:int}")]
    [RequiredScope("backend.read")]
    public async Task<Results<NotFound, Ok<MemorylandDto>, UnauthorizedHttpResult>> GetCompleteMemoryland(int id)
    {
        // check if the user is authenticated without errors
        var user = await UserSvc.CheckIfUserAuthenticated(User.Claims);

        var authorizationHeader = Request.Headers["Authorization"].FirstOrDefault();

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
    public async Task<Results<NotFound, Ok<List<MemorylandConfigurationDto>>, BadRequest<string>, UnauthorizedHttpResult>> GetMemorylandConfig(int id)
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
    
}