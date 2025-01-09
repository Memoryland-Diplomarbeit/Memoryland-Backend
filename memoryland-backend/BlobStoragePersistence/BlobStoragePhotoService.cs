using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

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
        
        if (string.IsNullOrEmpty(accessKey))
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
        string blobContainerName, 
        string albumName, 
        string photoName, 
        byte[] photoBytes)
    {
        var containerClient = BlobSvcClient
            .GetBlobContainerClient(blobContainerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(
            $"{albumName}/{photoName}");

        using var stream = new MemoryStream(photoBytes);
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public async Task<Uri?> GetPhoto(
        long userId, 
        string albumName, 
        string photoName)
    {
        var containerClient = BlobSvcClient
            .GetBlobContainerClient(PadBlobClientName(userId));

        if (!await containerClient.ExistsAsync()) return null;
        
        var blobClient = containerClient.GetBlobClient(
            $"{albumName}/{photoName}");

        if (!await blobClient.ExistsAsync()) return null;
        
        return SasTokenGeneratorService
            .CreateUserDelegationSasBlob(
                blobClient,
                AccessKey);

    }
    
    private string PadBlobClientName(long blobClientName)
    {
        return blobClientName.ToString().PadLeft(
            long.MaxValue.ToString().Length, 
            '0');
    }
}