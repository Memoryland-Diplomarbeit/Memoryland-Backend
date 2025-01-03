using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[Index(nameof(Email), IsUnique = true), Index(nameof(Username), IsUnique = true)]
public class User : BaseEntity
{
    [Required, MaxLength(50)]
    public string Email { get; set; }
    
    [Required, MaxLength(50)]
    public string Username { get; set; }
    
    public ICollection<PhotoAlbum> PhotoAlbums { get; set; }
    
    public ICollection<Memoryland> Memorylands { get; set; }
}