using System.Text;
using Whistleblowing.Domain.Entities;
using Whistleblowing.Infrastructure.Data;

namespace Whistleblowing.Web.Services;

public sealed class EvidenceUploadService
{
    public const int MaxFilesPerUpload = 5;

    private static readonly IReadOnlyDictionary<string, string> SupportedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".txt"] = "text/plain",
    };

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public EvidenceUploadService(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    public async Task<string?> ValidateAsync(IReadOnlyCollection<IFormFile>? files, int maxSizeMb, CancellationToken ct = default)
    {
        var selectedFiles = files?.Where(file => file.Length > 0).ToList() ?? new List<IFormFile>();
        if (selectedFiles.Count > MaxFilesPerUpload)
            return $"Choose no more than {MaxFilesPerUpload} evidence files at a time.";
        if (selectedFiles.Sum(file => file.Length) > 25L * 1024 * 1024)
            return "The combined size of evidence files must be 25 MB or less per upload.";

        foreach (var file in selectedFiles)
        {
            if (file.Length > (long)maxSizeMb * 1024 * 1024)
                return $"Each evidence file must be no larger than {maxSizeMb} MB.";

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !SupportedTypes.TryGetValue(extension, out var contentType))
                return "Supported evidence formats are PDF, PNG, JPG, GIF, WEBP, and TXT.";

            if (!await HasValidSignatureAsync(file, extension, ct))
                return $"The file '{Path.GetFileName(file.FileName)}' does not match its file format.";
        }

        return null;
    }

    public async Task SaveAsync(
        IReadOnlyCollection<IFormFile>? files,
        Guid complaintId,
        Guid? uploadedById,
        string? description,
        Guid? actorId,
        CancellationToken ct = default)
    {
        var selectedFiles = files?.Where(file => file.Length > 0).ToList() ?? new List<IFormFile>();
        if (selectedFiles.Count == 0) return;

        var directory = Path.Combine(_environment.ContentRootPath, "App_Data", "Evidence");
        Directory.CreateDirectory(directory);
        var storedPaths = new List<string>();

        try
        {
            foreach (var file in selectedFiles)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var storedName = $"{Guid.NewGuid():N}{extension}";
                var path = Path.Combine(directory, storedName);
                await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                    await file.CopyToAsync(stream, ct);
                storedPaths.Add(path);

                var displayName = Path.GetFileName(file.FileName);
                if (displayName.Length > 255) displayName = displayName[^255..];
                _db.Evidence.Add(new Evidence
                {
                    ComplaintId = complaintId,
                    FileName = displayName,
                    FileType = SupportedTypes[extension],
                    FilePath = storedName,
                    FileSize = file.Length,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                    UploadedById = uploadedById,
                });
            }

            _db.ActivityLogs.Add(new ActivityLog
            {
                EventType = "EvidenceUploaded",
                Description = "Evidence was uploaded for a concern.",
                EntityType = "Complaint",
                EntityId = complaintId,
                ActorId = actorId,
            });
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            foreach (var path in storedPaths)
                if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    private static async Task<bool> HasValidSignatureAsync(IFormFile file, string extension, CancellationToken ct)
    {
        var buffer = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => read >= 5 && Encoding.ASCII.GetString(buffer, 0, 5) == "%PDF-",
            ".png" => read >= 8 && buffer.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            ".gif" => read >= 6 && (Encoding.ASCII.GetString(buffer, 0, 6) is "GIF87a" or "GIF89a"),
            ".webp" => read >= 12 && Encoding.ASCII.GetString(buffer, 0, 4) == "RIFF" && Encoding.ASCII.GetString(buffer, 8, 4) == "WEBP",
            ".txt" => read > 0 && !buffer.AsSpan(0, read).Contains((byte)0),
            _ => false,
        };
    }
}
