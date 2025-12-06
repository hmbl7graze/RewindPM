namespace RewindPM.Application.Read.DTOs;

/// <summary>
/// プロジェクトの読み取りモデル
/// </summary>
public record ProjectDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime? UpdatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
