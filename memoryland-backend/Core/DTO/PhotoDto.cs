namespace Core.DTO;

public record PhotoDto(
    string Name,
    long PhotoAlbumId,
    Uri Photo);