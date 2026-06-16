using Application.Services.FileService;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Adapters.FileService;

public class FileServiceAdapter : FileServiceBase
{
    private readonly string _uploadPath;

    public FileServiceAdapter(IConfiguration configuration)
    {
        _uploadPath = configuration["FileStorage:UploadPath"] ?? "uploads";
    }

    public override async Task<string> UploadAsync(IFormFile formFile, string path)
    {
        if (formFile == null || formFile.Length == 0)
            throw new ArgumentException("Invalid file.");

        var fileExtension = Path.GetExtension(formFile.FileName);
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var directoryPath = Path.Combine(_uploadPath, path);

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        var filePath = Path.Combine(directoryPath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await formFile.CopyToAsync(stream);

        return Path.Combine(path, fileName);
    }

    public override async Task DeleteAsync(string fileUrl)
    {
        var filePath = Path.Combine(_uploadPath, fileUrl);
        if (File.Exists(filePath))
            File.Delete(filePath);

        await Task.CompletedTask;
    }

    public override async Task<bool> CreateDirectory(string directoryPath)
    {
        try
        {
            var fullPath = Path.Combine(_uploadPath, directoryPath);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
