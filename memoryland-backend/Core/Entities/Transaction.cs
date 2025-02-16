using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[Index(nameof(UserId), IsUnique = true)]
public class Transaction : BaseEntity
{
    [Required]
    public string PhotoAlbumPath { get; set; }
    
    public PhotoAlbum PhotoAlbum { get; set; }
    
    public long PhotoAlbumId { get; set; }
    
    public User User { get; set; }
    
    public long UserId { get; set; }
    
}