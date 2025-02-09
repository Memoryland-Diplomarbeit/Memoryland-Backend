using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[Index(nameof(Name), nameof(UserId), IsUnique = true)]
public class Memoryland : BaseEntity
{
    [Required, MaxLength(50)]
    public string Name { get; set; }
    
    public MemorylandType MemorylandType { get; set; }
    
    public long MemorylandTypeId { get; set; }
    
    public User User { get; set; }
    
    public long UserId { get; set; }
    
    public ICollection<MemorylandConfiguration> MemorylandConfigurations { get; set; }
    
    public ICollection<MemorylandToken> MemorylandTokens { get; set; }
}
