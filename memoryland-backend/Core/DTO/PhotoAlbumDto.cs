namespace Core.DTO;

public record PhotoAlbumDto(
    long Id,
    string Name,
    IEnumerable<string> Photos);
    