namespace Whistleblowing.Application.Interfaces;

public interface IReferenceCodeService
{
    /// <summary>
    /// Generates a unique reference code in the format WB-YYYY-XXXXXX
    /// </summary>
    Task<string> GenerateUniqueAsync(CancellationToken ct = default);
}
