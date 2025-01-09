using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[
    Index(nameof(Token), IsUnique = true), 
    Index(
        nameof(IsInternal), 
        nameof(MemorylandId), 
        IsUnique = true)
]
public class MemorylandToken : BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Token { get; set; }

    public bool IsInternal { get; set; } = true;
    
    public Memoryland Memoryland { get; set; }
    
    public long MemorylandId { get; set; }
}