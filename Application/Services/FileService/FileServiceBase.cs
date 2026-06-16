
using Core.CrossCuttingConcerns.Exception.Types;
using Microsoft.AspNetCore.Http;

namespace Application.Services.FileService;

public abstract class FileServiceBase
{
    public abstract Task<string> UploadAsync(IFormFile formFile,string path);

    public abstract Task DeleteAsync(string imageUrl);
    
    public abstract Task<bool> CreateDirectory(string directoryPath);


    protected async Task FileMustBeInImageFormat(IFormFile formFile)
    {
        List<string> extensions = [".jpg", ".png", ".jpeg", ".webp"];

        string extension = Path.GetExtension(formFile.FileName).ToLower();
        if (!extensions.Contains(extension))
            throw new BusinessException("Unsupported format");
        await Task.CompletedTask;
    }
}