using RewindPM.Application.Read.DTOs;

namespace RewindPM.Application.Read.Test.DTOs;

public class ProjectStatisticsDetailDtoTests
{
    [Fact(DisplayName = "完了率: タスクがある場合、正しく計算される")]
    public void CompletionRate_WithTotalTasks_ShouldCalculateCorrectly()
    {
        // Arrange
        var dto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 4,
            InProgressTasks = 3,
            InReviewTasks = 2,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 80,
            RemainingEstimatedHours = 60,
            OnTimeTasks = 3,
            DelayedTasks = 1,
            AverageDelayDays = 2.5,
            AsOfDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        Assert.Equal(40.0, dto.CompletionRate);
    }

    [Fact(DisplayName = "完了率: タスクがない場合、ゼロを返す")]
    public void CompletionRate_WithNoTotalTasks_ShouldReturnZero()
    {
        // Arrange
        var dto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 0,
            CompletedTasks = 0,
            InProgressTasks = 0,
            InReviewTasks = 0,
            TodoTasks = 0,
            TotalEstimatedHours = 0,
            TotalActualHours = 0,
            RemainingEstimatedHours = 0,
            OnTimeTasks = 0,
            DelayedTasks = 0,
            AverageDelayDays = 0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        Assert.Equal(0, dto.CompletionRate);
    }

    [Fact(DisplayName = "工数オーバーラン: 実績工数が予定を超える場合、正の値を返す")]
    public void HoursOverrun_WithOverrunActualHours_ShouldReturnPositive()
    {
        // Arrange
        var dto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 120,
            RemainingEstimatedHours = 50,
            OnTimeTasks = 4,
            DelayedTasks = 1,
            AverageDelayDays = 1.0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        Assert.Equal(20, dto.HoursOverrun);
    }

    [Fact(DisplayName = "工数オーバーラン: 実績工数が予定を下回る場合、負の値を返す")]
    public void HoursOverrun_WithUnderrunActualHours_ShouldReturnNegative()
    {
        // Arrange
        var dto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 80,
            RemainingEstimatedHours = 60,
            OnTimeTasks = 5,
            DelayedTasks = 0,
            AverageDelayDays = 0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        Assert.Equal(-20, dto.HoursOverrun);
    }

    [Fact(DisplayName = "オーバーラン率: 予定工数がある場合、正しく計算される")]
    public void OverrunRate_WithEstimatedHours_ShouldCalculateCorrectly()
    {
        // Arrange
        var dto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 125,
            RemainingEstimatedHours = 50,
            OnTimeTasks = 4,
            DelayedTasks = 1,
            AverageDelayDays = 1.0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        Assert.Equal(25.0, dto.OverrunRate);
    }

    [Fact(DisplayName = "オーバーラン率: 予定工数がない場合、ゼロを返す")]
    public void OverrunRate_WithNoEstimatedHours_ShouldReturnZero()
    {
        // Arrange
        var dto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 0,
            TotalActualHours = 50,
            RemainingEstimatedHours = 0,
            OnTimeTasks = 4,
            DelayedTasks = 1,
            AverageDelayDays = 1.0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        Assert.Equal(0, dto.OverrunRate);
    }

    [Fact(DisplayName = "期限内完了率: 完了タスクがある場合、正しく計算される")]
    public void OnTimeRate_WithCompletedTasks_ShouldCalculateCorrectly()
    {
        // Arrange
        var dto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 80,
            RemainingEstimatedHours = 60,
            OnTimeTasks = 4,
            DelayedTasks = 1,
            AverageDelayDays = 2.0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        Assert.Equal(80.0, dto.OnTimeRate);
    }

    [Fact(DisplayName = "期限内完了率: 完了タスクがない場合、ゼロを返す")]
    public void OnTimeRate_WithNoCompletedTasks_ShouldReturnZero()
    {
        // Arrange
        var dto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 0,
            InProgressTasks = 5,
            InReviewTasks = 3,
            TodoTasks = 2,
            TotalEstimatedHours = 100,
            TotalActualHours = 0,
            RemainingEstimatedHours = 100,
            OnTimeTasks = 0,
            DelayedTasks = 0,
            AverageDelayDays = 0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        Assert.Equal(0, dto.OnTimeRate);
    }

    [Fact(DisplayName = "全ての計算プロパティ: 小数点第一位に丸められる")]
    public void AllCalculatedProperties_ShouldRoundToOneDecimalPlace()
    {
        // Arrange
        var dto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 3,
            CompletedTasks = 1,
            InProgressTasks = 1,
            InReviewTasks = 1,
            TodoTasks = 0,
            TotalEstimatedHours = 3,
            TotalActualHours = 4,
            RemainingEstimatedHours = 2,
            OnTimeTasks = 0,
            DelayedTasks = 1,
            AverageDelayDays = 1.5,
            AsOfDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        Assert.Equal(33.3, dto.CompletionRate);
        Assert.Equal(33.3, dto.OverrunRate);
        Assert.Equal(0.0, dto.OnTimeRate);
    }
}
