using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using BlazorShared;
using Microsoft.Extensions.Options;

namespace Microsoft.eShopWeb.Infrastructure.Services;

public class BlobStorageConnector
{
    private const string ContainerName = "orders";
    private readonly string _connectionString;

    public BlobStorageConnector(IOptions<BaseUrlConfiguration> options)
    {
        _connectionString = options.Value.StorageBase ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task WriteDataToBlob(string data)
    {
        var blobServiceClient = new BlobServiceClient(_connectionString);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

        byte[] byteArray = Encoding.UTF8.GetBytes(data);
        var stream = new MemoryStream(byteArray);

        string fileName = "order-" + Guid.NewGuid().ToString() + ".txt";

        await containerClient.UploadBlobAsync(fileName, stream).ConfigureAwait(false);
    }
}
