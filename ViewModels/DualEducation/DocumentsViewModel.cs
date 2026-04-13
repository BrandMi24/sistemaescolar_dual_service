using Microsoft.AspNetCore.Http;

namespace ControlEscolar.ViewModels.DualEducation;

public class DocumentsViewModel
{
    public IFormFile? ResumeSpanishFile { get; set; }
    public IFormFile? ResumeEnglishFile { get; set; }
    public IFormFile? IMSSCertificateFile { get; set; }
}
