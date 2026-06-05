using Application.DTOs.Cloud;
using Application.DTOs.Responses;
using Application.Services.Interfaces;
using Azure.Storage;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implementitions
{
    public class CloudService : ICloudService
    {
        private readonly string _account = "eduverseblob";
        private readonly string _key = "pi6dI16kBdtn522hQVerKhXSU0IcWhBoROhCnMCeXk4Tw+c1DVHwpu0q4rm5x2CtaGOEpVH92vRL+AStsmrvww==";
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient container;

        public CloudService()
        {
            var caredentials = new StorageSharedKeyCredential(_account, _key);
            var blobUri = new Uri($"https://{_account}.blob.core.windows.net");
            _blobServiceClient = new BlobServiceClient(blobUri, caredentials);
            container = _blobServiceClient.GetBlobContainerClient("files");

        }

        public async Task<ServiceResponse> DeleteFileAsync(FileDetails details)
        {
            var blobClient = container.GetBlobClient($"{details.Folder}/{details.FileName}");
            if(await blobClient.ExistsAsync())
            {
                await blobClient.DeleteAsync();
                return new ServiceResponse { success = true, message = "File deleted successfully" };
            }
            return new ServiceResponse { success = true, message = "File not found" };
        }

        public async Task<GetCloudFile> GetFileAsync(FileDetails details)
        {
            var blobClient = container.GetBlobClient($"{details.Folder}/{details.FileName}");
            if (await blobClient.ExistsAsync())
            {
                var data = await blobClient.OpenReadAsync();
                Stream stream = data;
                var content=await blobClient.DownloadContentAsync();
                var fileDetails = new FileDetails { FileName = details.FileName, Folder = details.Folder };
                return new GetCloudFile { FileStream = stream, Details = fileDetails };
            }
            return null;

        }

        public async Task<ServiceResponse> UploadFileAsync(AddCloudFile cloudFile)
        {
            var blobClient = container.GetBlobClient($"{cloudFile.Details.Folder}/{cloudFile.Details.FileName}");
            try
            {
                await using (Stream stream = cloudFile.File.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream);
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { success = false, message = $"File upload failed: {ex.Message}" };
            }
            return new ServiceResponse { success = true, message = "File uploaded successfully" };

        }
    }
}
