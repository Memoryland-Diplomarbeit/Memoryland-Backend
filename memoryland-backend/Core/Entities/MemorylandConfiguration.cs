using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[Index(nameof(Position), nameof(MemorylandId), IsUnique = true)]
public class MemorylandConfiguration : BaseEntity
{
    [Required]
    public int Position { get; set; }
    
    public Memoryland Memoryland { get; set; }
    
    public long MemorylandId { get; set; }
    
    public Photo Photo { get; set; }
    
    public long PhotoId { get; set; }
}