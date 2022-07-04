using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using BlazorShared;
using Microsoft.Extensions.Options;

namespace Microsoft.eShopWeb.Infrastructure.Services;

public class BlobStorageConnector
{
    private const string ContainerName = "Orders";
    private string _blobUri;

    public BlobStorageConnector(IOptions<BaseUrlConfiguration> options)
    {
        _blobUri = options.Value.StorageBase ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task WriteDataToBlob(string data)
    {
        var blobServiceClient = new BlobServiceClient(_blobUri);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

        byte[] byteArray = Encoding.UTF8.GetBytes(data);
        var stream = new MemoryStream(byteArray);

        string fileName = "order-" + Guid.NewGuid().ToString() + ".txt";

        await containerClient.UploadBlobAsync(fileName, stream).ConfigureAwait(false);
    }
}
