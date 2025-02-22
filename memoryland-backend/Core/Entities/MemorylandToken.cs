using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities;

#nullable disable
[
    Index(nameof(Token), IsUnique = true), 
    Index(nameof(MemorylandId), IsUnique = true)
]
public class MemorylandToken : BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Token { get; set; }

    public Memoryland Memoryland { get; set; }
    
    public long MemorylandId { get; set; }
}