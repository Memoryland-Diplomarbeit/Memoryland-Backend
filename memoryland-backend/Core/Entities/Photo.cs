using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[Index(nameof(Name), nameof(PhotoAlbumId), IsUnique = true)]
public class Photo : BaseEntity
{
    [Required, MaxLength(50)]
    public string Name { get; set; }
    
    public PhotoAlbum PhotoAlbum { get; set; }
    
    public int PhotoAlbumId { get; set; }
}