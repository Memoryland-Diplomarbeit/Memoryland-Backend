using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;

namespace BlobStoragePersistence;

public class BlobStoragePhotoService
{
    private BlobServiceClient BlobSvcClient { get; set; }
    private string AccessKey { get; }
    
    public BlobStoragePhotoService(BlobServiceClient blobServiceClient)
    {
        BlobSvcClient = blobServiceClient;
        
        // get the access key from the user secrets
        var config = new ConfigurationBuilder()
            .AddUserSecrets<BlobStoragePhotoService>()
            .Build();
        
        var accessKey = config.GetValue<string>(
            "ConnectionStrings:BlobStorageDefault");
        
        if (string.IsNullOrWhiteSpace(accessKey))
            throw new NullReferenceException(nameof(accessKey));
        
        foreach (var value in accessKey.Split(';'))
        {
            if (value.Contains("AccountKey"))
            {
                AccessKey = value.Replace("AccountKey=", "");
                return;
            }
        }
        
        // null-reference exception since the access key
        // isn't set in the config
        throw new NullReferenceException(nameof(accessKey));
    }

    public async Task UploadPhoto(
        long userId, 
        long photoId, 
        string photoName, 
        byte[] photoBytes)
    {
        var photo = photoBytes;

        if (photoName.Contains(".jpg") || photoName.Contains(".jpeg"))
            photo = RotateImage(photoBytes); // rotate images if needed
        
        var containerClient = BlobSvcClient
            .GetBlobContainerClient(PadLong(userId));
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient
            .GetBlobClient(PadLong(photoId));

        using var stream = new MemoryStream(photo);
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public async Task<Uri?> GetPhoto(
        long userId, 
        long photoId)
    {
        var containerClient = BlobSvcClient
            .GetBlobContainerClient(PadLong(userId));

        if (!await containerClient.ExistsAsync()) return null;
        
        var blobClient = containerClient.GetBlobClient(PadLong(photoId));

        if (!await blobClient.ExistsAsync()) return null;
        
        return SasTokenGeneratorService
            .CreateUserDelegationSasBlob(
                blobClient,
                AccessKey);
    }
    
    private static string PadLong(long number)
    {
        return number.ToString().PadLeft(
            20, 
            '0'); //long.MaxValue.ToString().Length + 1 reserve
    }
    
    private static byte[] RotateImage(byte[] imageBytes)
    {
        using var inputStream = new MemoryStream(imageBytes);
        var options = new DecoderOptions();
        using var image = Image.Load(options, inputStream);
        
        ApplyExifRotation(image);

        using var outputStream = new MemoryStream();
        image.Save(outputStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
        return outputStream.ToArray();
    }

    private static void ApplyExifRotation(Image image)
    {
        if (image.Metadata.ExifProfile == null) return; // Performance
        
        image.Metadata.ExifProfile.TryGetValue(
            ExifTag.Orientation, 
            out var orientation);

        if (orientation == null) return; // Performance

        switch (orientation.Value)
        {
            case 2: // Flip Horizontal
                image.Mutate(x => x.Flip(FlipMode.Horizontal));
                break;
            case 3: // Rotate 180°
                image.Mutate(x => x.Rotate(RotateMode.Rotate180));
                break;
            case 4: // Flip Vertical
                image.Mutate(x => x.Flip(FlipMode.Vertical));
                break;
            case 5: // Transpose (Flip & Rotate 90°)
                image.Mutate(x => x.Rotate(RotateMode.Rotate90).Flip(FlipMode.Horizontal));
                break;
            case 6: // Rotate 90°
                image.Mutate(x => x.Rotate(RotateMode.Rotate90));
                break;
            case 7: // Transverse (Flip & Rotate 270°)
                image.Mutate(x => x.Rotate(RotateMode.Rotate270).Flip(FlipMode.Horizontal));
                break;
            case 8: // Rotate 270°
                image.Mutate(x => x.Rotate(RotateMode.Rotate270));
                break;
        }
        
        image.Metadata.ExifProfile.SetValue(
            ExifTag.Orientation, 
            (ushort)1); // Normal
    }
}
