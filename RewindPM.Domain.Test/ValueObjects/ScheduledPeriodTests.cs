using RewindPM.Domain.ValueObjects;

namespace RewindPM.Domain.Test.ValueObjects;

public class ScheduledPeriodTests
{
    [Fact(DisplayName = "有効な値でScheduledPeriodのインスタンスを作成できる")]
    public void Constructor_WithValidValues_ShouldCreateInstance()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var estimatedHours = 40;

        // Act
        var scheduledPeriod = new ScheduledPeriod(startDate, endDate, estimatedHours);

        // Assert
        Assert.Equal(startDate, scheduledPeriod.StartDate);
        Assert.Equal(endDate, scheduledPeriod.EndDate);
        Assert.Equal(estimatedHours, scheduledPeriod.EstimatedHours);
    }

    [Fact(DisplayName = "予定終了日が予定開始日より前の場合、ArgumentExceptionをスローする")]
    public void Constructor_WhenEndDateIsBeforeStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 10);
        var endDate = new DateTime(2025, 1, 1);
        var estimatedHours = 40;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new ScheduledPeriod(startDate, endDate, estimatedHours));
        Assert.Equal("予定終了日は予定開始日より後でなければなりません", exception.Message);
    }

    [Fact(DisplayName = "予定終了日と予定開始日が同じ場合、ArgumentExceptionをスローする")]
    public void Constructor_WhenEndDateEqualsStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        var date = new DateTime(2025, 1, 1);
        var estimatedHours = 40;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new ScheduledPeriod(date, date, estimatedHours));
        Assert.Equal("予定終了日は予定開始日より後でなければなりません", exception.Message);
    }

    [Fact(DisplayName = "見積工数が0の場合、ArgumentExceptionをスローする")]
    public void Constructor_WhenEstimatedHoursIsZero_ShouldThrowArgumentException()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var estimatedHours = 0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new ScheduledPeriod(startDate, endDate, estimatedHours));
        Assert.Equal("見積工数は正の数でなければなりません", exception.Message);
    }

    [Fact(DisplayName = "見積工数が負の数の場合、ArgumentExceptionをスローする")]
    public void Constructor_WhenEstimatedHoursIsNegative_ShouldThrowArgumentException()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var estimatedHours = -10;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new ScheduledPeriod(startDate, endDate, estimatedHours));
        Assert.Equal("見積工数は正の数でなければなりません", exception.Message);
    }

    [Fact(DisplayName = "DurationInDaysプロパティが期間の日数を正しく計算する")]
    public void DurationInDays_ShouldCalculateCorrectly()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var scheduledPeriod = new ScheduledPeriod(startDate, endDate, 40);

        // Act
        var duration = scheduledPeriod.DurationInDays;

        // Assert
        Assert.Equal(9, duration);
    }

    [Fact(DisplayName = "同じ値を持つScheduledPeriodインスタンスは等価である")]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var period1 = new ScheduledPeriod(startDate, endDate, 40);
        var period2 = new ScheduledPeriod(startDate, endDate, 40);

        // Act & Assert
        Assert.Equal(period1, period2);
        Assert.True(period1 == period2);
    }

    [Fact(DisplayName = "異なる値を持つScheduledPeriodインスタンスは等価でない")]
    public void RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var period1 = new ScheduledPeriod(startDate, endDate, 40);
        var period2 = new ScheduledPeriod(startDate, endDate, 50);

        // Act & Assert
        Assert.NotEqual(period1, period2);
        Assert.True(period1 != period2);
    }
}
