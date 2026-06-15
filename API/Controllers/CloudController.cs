using Application.Services.Interfaces;
using API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CloudController(ICloudService cloudService) : ControllerBase
    {
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
        public async Task<IActionResult> GetPhoto(string FileName , string Folder)
        {
            var details = new Application.DTOs.Cloud.FileDetails
            {
                FileName = FileName,
                Folder = Folder
            };
            var result = await cloudService.GetFileAsync(details);
            Response.Headers.Add("Accept-Ranges", "bytes");
            if (result != null)
                return File(result.FileStream , "application/octet-stream", result.Details.FileName);
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
    }
}
