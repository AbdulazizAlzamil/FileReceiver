using BackendTask1.Models;
using System.Text.Json;

namespace BackendTask1.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public FileService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public string[] GetJsonFileNames()
        {
            try
            {
                var uploadDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Files");
                var jsonFileNames = Directory.GetFiles(uploadDirectory, "*.json").Select(Path.GetFileName).ToArray();
                return jsonFileNames;
            }
            catch(Exception)
            {
                return new string[0];
            }
        }

        public List<FileInfoModel> GetJsonFiles()
        {
            var jsonFileNames = GetJsonFileNames();

            var filesInfo = jsonFileNames.Select(fileName =>
            {
                var jsonFilePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Files", fileName);
                var jsonData = System.IO.File.ReadAllText(jsonFilePath);
                var fileData = JsonSerializer.Deserialize<JsonDataModel>(jsonData);
                var fileInfo = new FileInfoModel
                {
                    FileName = Path.GetFileNameWithoutExtension(fileName),
                    Owner = fileData.Owner,
                    Description = fileData.Description,
                    CreationDate = fileData.CreationDate,
                    ModificationDate = fileData.ModificationDate
                };
                return fileInfo;
            });

            return filesInfo.ToList();
        }
    }
}
