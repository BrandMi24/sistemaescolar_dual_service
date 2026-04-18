using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ControlEscolar.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string?> SavePdfAsync(IFormFile? file, string folderName)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            const long maxFileSize = 5_242_880;
            if (file.Length > maxFileSize)
            {
                throw new ArgumentException("El archivo no debe exceder 5 MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Solo se permiten archivos PDF.");
            }

            using (var reader = new BinaryReader(file.OpenReadStream()))
            {
                var header = reader.ReadBytes(4);
                if (header.Length < 4 || header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46)
                {
                    throw new ArgumentException("El archivo no es un PDF válido.");
                }
            }

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", folderName);
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{folderName}/{fileName}";
        }

        public void DeleteFile(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var relativePath = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_environment.WebRootPath, relativePath);

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
    }
}
