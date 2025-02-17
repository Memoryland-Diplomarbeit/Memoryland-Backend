using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[Index(nameof(Email), IsUnique = true)]
public class User : BaseEntity
{
    [Required, MaxLength(50)]
    public string Email { get; set; }
    
    public ICollection<PhotoAlbum> PhotoAlbums { get; set; }
    
    public ICollection<Memoryland> Memorylands { get; set; }
}