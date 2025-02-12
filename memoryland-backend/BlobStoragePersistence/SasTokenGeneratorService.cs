using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;

namespace BlobStoragePersistence;

public static class SasTokenGeneratorService
{
    private static double TokenLifeTimeInHours => 4;

    public static Uri CreateUserDelegationSasBlob(
        BlobClient blobClient,
        string accessKey)
    {
        var tokenStartTime = DateTimeOffset.UtcNow;
        var tokenEndTime = tokenStartTime
            .AddHours(TokenLifeTimeInHours);
        
        var sasBuilder = new BlobSasBuilder()
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b", // Blob resource
            StartsOn = tokenStartTime,
            ExpiresOn = tokenEndTime
        };

        sasBuilder.SetPermissions(
            BlobSasPermissions.Read | BlobSasPermissions.Write);

        var blobSvcClient = blobClient
            .GetParentBlobContainerClient()
            .GetParentBlobServiceClient();
        
        var storageSharedKeyCredential = new StorageSharedKeyCredential(
            blobSvcClient.AccountName,
            accessKey);

        var uriBuilder = new BlobUriBuilder(blobClient.Uri)
        {
            Sas = sasBuilder.ToSasQueryParameters(storageSharedKeyCredential)
        };

        return uriBuilder.ToUri();
    }
}