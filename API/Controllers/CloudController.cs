using Application.Services.Interfaces;
using API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.IO.Compression;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CloudController(ICloudService cloudService) : ControllerBase
    {
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        [HttpPost("Add/{Folder}")]
        [Authorize(Roles = AppRoles.AdminOrOrganizationAdmin)]
        public async Task<IActionResult> AddPhoto(IFormFile file, string Folder)
        {
            var cloudFile = new Application.DTOs.Cloud.AddCloudFile
            {
                Details = new Application.DTOs.Cloud.FileDetails
                {
                    FileName = file.FileName,
                    Folder = Folder
                },
                File = file
            };
            var result = await cloudService.UploadFileAsync(cloudFile);
            return Ok(result);
        }
        [HttpGet("Get/{Folder}/{FileName}")]
        public async Task<IActionResult> GetPhoto(string FileName , string Folder, [FromQuery] bool download = false)
        {
            var details = new Application.DTOs.Cloud.FileDetails
            {
                FileName = FileName,
                Folder = Folder
            };
            var result = await cloudService.GetFileAsync(details);
            if (result != null)
            {
                var contentType = ResolveContentType(result.FileStream, result.Details.FileName);
                if (download)
                    return File(result.FileStream, contentType, result.Details.FileName, enableRangeProcessing: true);

                return File(result.FileStream, contentType, enableRangeProcessing: true);
            }
            return NotFound();
        }
        [HttpGet("GetSas/{Folder}/{FileName}")]
        public async Task<IActionResult> GEtSas(string FileName, string Folder)
        {
            var details = new Application.DTOs.Cloud.FileDetails
            {
                FileName = FileName,
                Folder = Folder
            };
            var result = await cloudService.GetCloudUrl(details);
            Response.Headers["Accept-Ranges"] = "bytes";
            if (result != null)
                return Ok(result);
            return NotFound();
        }
        [HttpDelete("Delete/{Folder}/{FileName}")]
        [Authorize(Roles = AppRoles.AdminOrOrganizationAdmin)]
        public async Task<IActionResult> DeletePhoto(string FileName, string Folder)
        {
            var details = new Application.DTOs.Cloud.FileDetails
            {
                FileName = FileName,
                Folder = Folder
            };
            var result = await cloudService.DeleteFileAsync(details);
            if (result.success)
                return Ok(result);
            return NotFound(result);
        }

        private static string ResolveContentType(Stream fileStream, string fileName)
        {
            if (ContentTypeProvider.TryGetContentType(fileName, out var contentType))
                return contentType;

            if (!fileStream.CanSeek)
                return "application/octet-stream";

            var originalPosition = fileStream.Position;
            try
            {
                fileStream.Position = 0;
                var header = new byte[16];
                var bytesRead = fileStream.Read(header, 0, header.Length);
                if (bytesRead >= 4)
                {
                    if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
                        return "application/pdf";

                    if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                        return "image/png";

                    if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                        return "image/jpeg";

                    if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46)
                        return "image/gif";

                    if (header[0] == 0x42 && header[1] == 0x4D)
                        return "image/bmp";
                }

                if (bytesRead >= 12
                    && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                    && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                {
                    return "image/webp";
                }

                if (bytesRead >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04)
                {
                    fileStream.Position = 0;
                    using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
                    if (archive.Entries.Any(entry => entry.FullName.StartsWith("word/", StringComparison.OrdinalIgnoreCase)))
                        return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    if (archive.Entries.Any(entry => entry.FullName.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)))
                        return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    if (archive.Entries.Any(entry => entry.FullName.StartsWith("ppt/", StringComparison.OrdinalIgnoreCase)))
                        return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                }
            }
            catch
            {
                return "application/octet-stream";
            }
            finally
            {
                fileStream.Position = originalPosition;
            }

            return "application/octet-stream";
        }
    }
}
