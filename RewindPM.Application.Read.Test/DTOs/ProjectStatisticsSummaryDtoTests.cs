using RewindPM.Application.Read.DTOs;
using Xunit;

namespace RewindPM.Application.Read.Test.DTOs;

public class ProjectStatisticsSummaryDtoTests
{
    [Fact(DisplayName = "CompletionRate: タスクがない場合は0を返す")]
    public void CompletionRate_ReturnsZero_WhenNoTasks()
    {
        // Arrange
        var dto = new ProjectStatisticsSummaryDto
        {
            ProjectId = Guid.NewGuid(),
            TotalTasks = 0,
            CompletedTasks = 0,
            InProgressTasks = 0,
            InReviewTasks = 0,
            TodoTasks = 0
        };

        // Act
        var rate = dto.CompletionRate;

        // Assert
        Assert.Equal(0, rate);
    }

    [Fact(DisplayName = "CompletionRate: 完了率を正しく計算する")]
    public void CompletionRate_CalculatesCorrectly()
    {
        // Arrange
        var dto = new ProjectStatisticsSummaryDto
        {
            ProjectId = Guid.NewGuid(),
            TotalTasks = 10,
            CompletedTasks = 8,
            InProgressTasks = 1,
            InReviewTasks = 1,
            TodoTasks = 0
        };

        // Act
        var rate = dto.CompletionRate;

        // Assert
        Assert.Equal(80.0, rate);
    }

    [Fact(DisplayName = "CompletionRate: 小数点以下を四捨五入する")]
    public void CompletionRate_RoundsToOneDecimalPlace()
    {
        // Arrange
        var dto = new ProjectStatisticsSummaryDto
        {
            ProjectId = Guid.NewGuid(),
            TotalTasks = 3,
            CompletedTasks = 2,
            InProgressTasks = 1,
            InReviewTasks = 0,
            TodoTasks = 0
        };

        // Act
        var rate = dto.CompletionRate;

        // Assert
        Assert.Equal(66.7, rate);
    }

    [Fact(DisplayName = "すべてのプロパティが正しく設定される")]
    public void AllProperties_AreSetCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var dto = new ProjectStatisticsSummaryDto
        {
            ProjectId = projectId,
            TotalTasks = 12,
            CompletedTasks = 8,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 0
        };

        // Assert
        Assert.Equal(projectId, dto.ProjectId);
        Assert.Equal(12, dto.TotalTasks);
        Assert.Equal(8, dto.CompletedTasks);
        Assert.Equal(3, dto.InProgressTasks);
        Assert.Equal(1, dto.InReviewTasks);
        Assert.Equal(0, dto.TodoTasks);
    }
}
