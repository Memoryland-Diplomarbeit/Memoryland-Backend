using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[Index(nameof(Name), IsUnique = true)]
public class Memoryland : BaseEntity
{
    [Required, MaxLength(50)]
    public string Name { get; set; }
    
    public MemorylandType MemorylandType { get; set; }
    
    public int MemorylandTypeId { get; set; }
    
    public User User { get; set; }
    
    public int UserId { get; set; }
    
    public ICollection<MemorylandConfiguration> MemorylandConfigurations { get; set; }
}
