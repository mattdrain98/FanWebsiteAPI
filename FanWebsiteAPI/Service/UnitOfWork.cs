using Fan_Website.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly IWebHostEnvironment _env;

    public UnitOfWork(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return;

        string fileName = EnsureFileName(file.FileName);
        string path = GetPathAndFileName(fileName);

        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
    }

    // Get the file path and file name
    private string GetPathAndFileName(string filename)
    {
        string path = Path.Combine(_env.WebRootPath, "images");

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return Path.Combine(path, filename);
    }

    private string EnsureFileName(string fileName)
    {
        return Path.GetFileName(fileName);
    }
}