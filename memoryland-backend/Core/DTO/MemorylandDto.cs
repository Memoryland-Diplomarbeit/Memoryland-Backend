using Core.Entities;

namespace Core.DTO;

public record MemorylandDto(
    long Id,
    string Name,
    MemorylandType MemorylandType,
    List<MemorylandConfigurationDto> MemorylandConfigurations);
