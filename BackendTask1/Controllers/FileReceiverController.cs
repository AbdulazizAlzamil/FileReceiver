using System.Text.Json;
using BackendTask1.Services;
using Microsoft.AspNetCore.Mvc;
using BackendTask1.Models;
using BackendTask1.Enums;
using System.Runtime.InteropServices;

namespace BackendTask1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileReceiverController : ControllerBase
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IFileService _fileService;

        public FileReceiverController(IWebHostEnvironment hostingEnvironment, IFileService fileService)
        {
            _hostingEnvironment = hostingEnvironment;
            _fileService = fileService;
        }

        [HttpGet]
        public IActionResult Log([FromForm]string dateTime)
        {
            List<FileInfoModel> filesInfo = _fileService.GetJsonFiles();

            return Ok(filesInfo);
        }

        [HttpPost("filter-by-date")]
        public async Task<IActionResult> FilterByDate([FromForm]string? startDate, [FromForm] string? endDate, [FromForm]SortType sortType)
        {
            try
            {
                List<FileInfoModel> filesInfo = _fileService.GetJsonFiles();

                switch(sortType)
                {
                    case SortType.CreationDateAsc:
                        filesInfo = (List<FileInfoModel>)filesInfo.OrderBy(f => f.CreationDate);
                        break;

                    case SortType.CreationDateDesc:
                        filesInfo = (List<FileInfoModel>)filesInfo.OrderByDescending(f => f.CreationDate);
                        break;

                    case SortType.ModificationDateAsc:
                        filesInfo = (List<FileInfoModel>)filesInfo.OrderBy(f => f.ModificationDate);
                        break;

                    case SortType.ModificationDateDesc:
                        filesInfo = (List<FileInfoModel>)filesInfo.OrderByDescending(f => f.ModificationDate);
                        break;

                    case SortType.DateRange:
                        filesInfo = (List<FileInfoModel>)filesInfo.Where(f => DateTime.Parse(f.CreationDate) >= DateTime.Parse(startDate) && DateTime.Parse(f.CreationDate) <= DateTime.Parse(endDate));
                        break;
                    default:
                        filesInfo = (List<FileInfoModel>)filesInfo.OrderBy(f => f.CreationDate);
                        break;
                }

                return Ok(filesInfo);
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("filter-by-user")]
        public async Task<IActionResult> FilterByUser([FromForm]string users, [FromForm]string? startDate, [FromForm]string? endDate, [FromForm]SortType sortType)
        {
            string[] newUsers = users.Split(',');
            var filesInfoGroups = _fileService.GetJsonFiles()
                .Where(d => newUsers.Contains(d.Owner))
                .GroupBy(d => d.Owner);

            IEnumerable<FileInfoModel> filesInfo;

            switch(sortType)
            {
                case SortType.CreationDateAsc:
                    filesInfo = filesInfoGroups.SelectMany(group => group.OrderBy(d => d.CreationDate));
                    break;

                case SortType.CreationDateDesc:
                    filesInfo = filesInfoGroups.SelectMany(group => group.OrderByDescending(d => d.CreationDate));
                    break;

                case SortType.ModificationDateAsc:
                    filesInfo = filesInfoGroups.SelectMany(group => group.OrderBy(d => d.ModificationDate));
                    break;

                case SortType.ModificationDateDesc:
                    filesInfo = filesInfoGroups.SelectMany(group => group.OrderByDescending(d => d.ModificationDate));
                    break;

                case SortType.DateRange:
                    filesInfo = filesInfoGroups.SelectMany(group => group
                        .Where(f => DateTime.Parse(f.CreationDate) >= DateTime.Parse(startDate) &&
                                    DateTime.Parse(f.CreationDate) <= DateTime.Parse(endDate)));
                    break;

                default:
                    filesInfo = filesInfoGroups.SelectMany(group => group.OrderBy(d => d.CreationDate));
                    break;
            }

            return Ok(filesInfo.ToList());
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] UploadFormData formData)
        {
            switch(formData.QueryType)
            {
                case QueryType.Create:
                    return await CreateFile(formData);
                case QueryType.Update:
                    return await UpdateFile(formData);
                case QueryType.Delete:
                    return await DeleteFile(formData);
                case QueryType.Retrieve:
                    return await RetrieveFile(formData);
                default:
                    return BadRequest("Invalid query type.");
            }
        }

        private async Task<IActionResult> CreateFile(UploadFormData formData)
        {
            if(formData.File == null || formData.File.Length == 0)
            {
                return BadRequest("File is required.");
            }

            if(string.IsNullOrEmpty(formData.FileName))
            {
                return BadRequest("File Name is required.");
            }

            if(string.IsNullOrEmpty(formData.Owner))
            {
                return BadRequest("Owner is required.");
            }

            if(string.IsNullOrEmpty(formData.Description))
            {
                return BadRequest("Description is required.");
            }

            if(!(formData.File.ContentType == "image/jpg" ||
                  formData.File.ContentType == "video/mp4" ||
                  formData.File.ContentType == "image/jpeg" ||
                  formData.File.ContentType == "application/pdf"))
            {
                return BadRequest("File type not supported. Supported types: jpg, mp4");
            }

            var uploadDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Files");
            var filePath = Path.Combine(uploadDirectory, $"{formData.FileName}.json");
            if(System.IO.File.Exists(filePath))
            {
                return Conflict("File already exists.");
            }

            var uniqueFileName = formData.FileName + Path.GetExtension(formData.File.FileName);
            filePath = Path.Combine(uploadDirectory, uniqueFileName);
            using(var stream = new FileStream(filePath, FileMode.Create))
            {
                await formData.File.CopyToAsync(stream);
            }

            var jsonData = new
            {
                FileName = formData.FileName,
                Owner = formData.Owner,
                Description = formData.Description,
                CreationDate = DateTime.Now.ToString("o"),
                ModificationDate = DateTime.Now.ToString("o"),
            };

            var jsonFilePath = Path.Combine(uploadDirectory, $"{formData.FileName}.json");

            using(StreamWriter streamWriter = new StreamWriter(jsonFilePath))
            {
                await streamWriter.WriteAsync(JsonSerializer.Serialize(jsonData));
            }

            return Ok("File uploaded successfully.");
        }

        private async Task<IActionResult> UpdateFile(UploadFormData formData)
        {
            if(string.IsNullOrEmpty(formData.FileName))
            {
                return BadRequest("File Name is required.");
            }

            if(string.IsNullOrEmpty(formData.Owner))
            {
                return BadRequest("Owner is required.");
            }

            // Check if the file exists
            var uploadDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Files");
            var filePath = Path.Combine(uploadDirectory, $"{formData.FileName}{Path.GetExtension(formData.File.FileName)}");
            if(!System.IO.File.Exists(filePath))
            {
                return NotFound("File does not exist.");
            }

            // Read the existing JSON file
            var jsonFilePath = Path.Combine(uploadDirectory, $"{formData.FileName}.json");
            string jsonContent = await System.IO.File.ReadAllTextAsync(jsonFilePath);

            // Deserialize the JSON content to extract the owner
            var jsonData = JsonSerializer.Deserialize<JsonDataModel>(jsonContent);
            if(jsonData == null || jsonData.Owner != formData.Owner)
            {
                return BadRequest("Invalid file owner.");
            }

            // Update the description if provided
            if(!string.IsNullOrEmpty(formData.Description))
            {
                jsonData.Description = formData.Description;
            }

            // Update the file content if provided
            if(formData.File != null)
            {
                using(var stream = new FileStream(filePath, FileMode.Create))
                {
                    await formData.File.CopyToAsync(stream);
                }
            }

            jsonData.ModificationDate = DateTime.Now.ToString("o");

            // Write the updated JSON data back to the file
            await System.IO.File.WriteAllTextAsync(jsonFilePath, JsonSerializer.Serialize(jsonData));

            return Ok("File updated successfully.");
        }

        private async Task<IActionResult> DeleteFile(UploadFormData formData)
        {
            if (string.IsNullOrEmpty(formData.FileName))
            {
                return BadRequest("File Name is required.");
            }

            if (string.IsNullOrEmpty(formData.Owner))
            {
                return BadRequest("Owner is required.");
            }

            // Check if the file exists
            var uploadDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Files");
            var filePath = Path.Combine(uploadDirectory, $"{formData.FileName}{Path.GetExtension(formData.File.FileName)}");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File does not exist.");
            }

            // Read the existing JSON file
            var jsonFilePath = Path.Combine(uploadDirectory, $"{formData.FileName}.json");
            string jsonContent = await System.IO.File.ReadAllTextAsync(jsonFilePath);

            // Deserialize the JSON content to extract the owner
            var jsonData = JsonSerializer.Deserialize<JsonDataModel>(jsonContent);
            if (jsonData == null || jsonData.Owner != formData.Owner)
            {
                return BadRequest("Invalid file owner.");
            }

            // Delete the file
            System.IO.File.Delete(filePath);

            // Delete the JSON record
            System.IO.File.Delete(jsonFilePath);

            return Ok("File and JSON record deleted successfully.");
        }

        private async Task<IActionResult> RetrieveFile(UploadFormData formData)
        {
            // Check if filename and owner are provided
            if(string.IsNullOrEmpty(formData.FileName) || string.IsNullOrEmpty(formData.Owner))
            {
                return BadRequest("File Name and Owner are required.");
            }

            // Check if the file exists
            var uploadDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Files");
            var filePath = Path.Combine(uploadDirectory, $"{formData.FileName}.json");
            if(!System.IO.File.Exists(filePath))
            {
                return NotFound("File does not exist.");
            }

            // Read the JSON file
            string jsonContent = await System.IO.File.ReadAllTextAsync(filePath);

            // Deserialize JSON content
            var jsonData = JsonSerializer.Deserialize<JsonDataModel>(jsonContent);

            // Verify owner
            if(jsonData.Owner != formData.Owner)
            {
                return BadRequest("Invalid file owner.");
            }

            // Prepare response form data
            var responseFormData = new
            {
                FileName = jsonData.FileName,
                Owner = jsonData.Owner,
                Description = jsonData.Description,
                FileContent = await System.IO.File.ReadAllBytesAsync(filePath) // Read file content
            };

            // Return the response
            return Ok(responseFormData);
        }

    }
}
