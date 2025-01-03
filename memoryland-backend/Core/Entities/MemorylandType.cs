using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[Index(nameof(Name), IsUnique = true)]
public class MemorylandType
    : BaseEntity
{
    [Required, MaxLength(50)]
    public string Name { get; set; }
    
    [Required, MaxLength(50)]
    public int PhotoAmount { get; set; }
}