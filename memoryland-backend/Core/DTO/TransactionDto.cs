namespace Core.DTO;

public record TransactionDto(
    long Id,
    PhotoAlbumDto DestAlbum,
    string SrcAlbumPath);
