using BackendTask1.Models;

namespace BackendTask1.Services
{
    public interface IFileService
    {
        string[] GetJsonFileNames();
        List<FileInfoModel> GetJsonFiles();
    }
}
