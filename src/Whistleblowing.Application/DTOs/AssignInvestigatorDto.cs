namespace Whistleblowing.Application.DTOs;

public class AssignInvestigatorDto
{
    public Guid CaseId { get; set; }
    public string CaseTitle { get; set; } = string.Empty;

    /// <summary>Currently assigned investigator IDs</summary>
    public List<Guid> SelectedInvestigatorIds { get; set; } = new();

    /// <summary>All active investigators to choose from</summary>
    public List<InvestigatorOptionDto> AvailableInvestigators { get; set; } = new();
}

public class InvestigatorOptionDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
}

public record AssignResult(
    bool Success,
    List<Guid>? Added = null,
    List<Guid>? Removed = null,
    IEnumerable<string>? Errors = null);