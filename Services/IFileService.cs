using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ControlEscolar.Services
{
    public interface IFileService
    {
        Task<string?> SavePdfAsync(IFormFile? file, string folderName);
        void DeleteFile(string? path);
    }
}
