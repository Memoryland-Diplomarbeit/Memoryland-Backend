namespace Core.DTO;

public record PostPhotoDto<T>(
    string FileName,
    int PhotoAlbumId,
    T Photo
);