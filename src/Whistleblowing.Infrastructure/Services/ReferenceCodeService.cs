using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Whistleblowing.Application.Interfaces;
using Whistleblowing.Infrastructure.Data;

namespace Whistleblowing.Infrastructure.Services;

public class ReferenceCodeService : IReferenceCodeService
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly AppDbContext _db;

    public ReferenceCodeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateUniqueAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = $"WB-{year}-{RandomCode(12)}";
            var exists = await _db.Complaints
                .AnyAsync(c => c.ReferenceCode == code, ct);

            if (!exists) return code;
        }

        throw new InvalidOperationException("Failed to generate unique reference code.");
    }

    private static string RandomCode(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Chars[RandomNumberGenerator.GetInt32(Chars.Length)];
        }
        return new string(chars);
    }
}
