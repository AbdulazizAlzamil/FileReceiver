using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendTask1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileReceiverController : ControllerBase
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public FileReceiverController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, string fileName, string owner, string description)
        {
            if(file == null || file.Length == 0)
            {
                return BadRequest("File is required.");
            }

            if(string.IsNullOrEmpty(fileName))
            {
                return BadRequest("FileName is required.");
            }

            if(string.IsNullOrEmpty(owner))
            {
                return BadRequest("Owner is required.");
            }

            if(string.IsNullOrEmpty(description))
            {
                return BadRequest("Description is required.");
            }

            if(!(file.ContentType == "image/jpg" || file.ContentType == "video/mp4" || file.ContentType == "image/jpeg"))
            {
                return BadRequest("File type not supported. Supported types: jpg, mp4");
            }

            var uploadDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Files");
            if(!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            var uniqueFileName = fileName + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadDirectory, uniqueFileName);

            using(var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var jsonData = new
            {
                fileName = fileName,
                owner = owner,
                description = description,
            };

            var jsonString = JsonSerializer.Serialize(jsonData);
            var jsonFilePath = Path.Combine(uploadDirectory, $"{fileName}.json");
            using(StreamWriter sw = new StreamWriter(jsonFilePath))
            {
                await sw.WriteAsync(jsonString);
            }

            return Ok("File uploaded successfully.");
        }
    }
}
