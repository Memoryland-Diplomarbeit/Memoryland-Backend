using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[Index(nameof(Name), nameof(UserId), IsUnique = true)]
public class PhotoAlbum : BaseEntity
{
    [Required, MaxLength(50)]
    public string Name { get; set; }
    
    public User User { get; set; }
    
    public long UserId { get; set; }
    
    public ICollection<Photo> Photos { get; set; }
}