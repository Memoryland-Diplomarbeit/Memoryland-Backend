using Core.Entities;

namespace Core.DTO;

public record MemorylandInfoDto(
    long Id,
    string Name,
    MemorylandType MemorylandType);