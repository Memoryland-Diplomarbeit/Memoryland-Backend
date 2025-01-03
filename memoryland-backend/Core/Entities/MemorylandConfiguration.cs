using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[Index(nameof(Position), nameof(MemorylandId), IsUnique = true)]
public class MemorylandConfiguration : BaseEntity
{
    [Required, MaxLength(50)]
    public int Position { get; set; }
    
    public Memoryland Memoryland { get; set; }
    
    public int MemorylandId { get; set; }
    
    public Photo Photo { get; set; }
    
    public int PhotoId { get; set; }
}