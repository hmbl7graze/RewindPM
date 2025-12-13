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
        var date = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
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
        var startDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero);
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
        var startDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero);
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
        var startDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero);
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
        var startDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero);
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
        var startDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var period1 = new ScheduledPeriod(startDate, endDate, 40);
        var period2 = new ScheduledPeriod(startDate, endDate, 50);

        // Act & Assert
        Assert.NotEqual(period1, period2);
        Assert.True(period1 != period2);
    }

    [Fact(DisplayName = "パラメータなしでScheduledPeriodのインスタンスを作成すると全てnullになる")]
    public void Constructor_WithNoParameters_ShouldCreateInstanceWithAllNulls()
    {
        // Act
        var scheduledPeriod = new ScheduledPeriod();

        // Assert
        Assert.Null(scheduledPeriod.StartDate);
        Assert.Null(scheduledPeriod.EndDate);
        Assert.Null(scheduledPeriod.EstimatedHours);
    }

    [Fact(DisplayName = "予定開始日のみを指定してScheduledPeriodのインスタンスを作成できる")]
    public void Constructor_WithOnlyStartDate_ShouldCreateInstance()
    {
        // Arrange
        var startDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var scheduledPeriod = new ScheduledPeriod(startDate: startDate);

        // Assert
        Assert.Equal(startDate, scheduledPeriod.StartDate);
        Assert.Null(scheduledPeriod.EndDate);
        Assert.Null(scheduledPeriod.EstimatedHours);
    }

    [Fact(DisplayName = "予定終了日のみを指定してScheduledPeriodのインスタンスを作成できる")]
    public void Constructor_WithOnlyEndDate_ShouldCreateInstance()
    {
        // Arrange
        var endDate = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero);

        // Act
        var scheduledPeriod = new ScheduledPeriod(endDate: endDate);

        // Assert
        Assert.Null(scheduledPeriod.StartDate);
        Assert.Equal(endDate, scheduledPeriod.EndDate);
        Assert.Null(scheduledPeriod.EstimatedHours);
    }

    [Fact(DisplayName = "見積工数のみを指定してScheduledPeriodのインスタンスを作成できる")]
    public void Constructor_WithOnlyEstimatedHours_ShouldCreateInstance()
    {
        // Arrange
        var estimatedHours = 40;

        // Act
        var scheduledPeriod = new ScheduledPeriod(estimatedHours: estimatedHours);

        // Assert
        Assert.Null(scheduledPeriod.StartDate);
        Assert.Null(scheduledPeriod.EndDate);
        Assert.Equal(estimatedHours, scheduledPeriod.EstimatedHours);
    }

    [Fact(DisplayName = "日付が部分的に設定されている場合、DurationInDaysはnullを返す")]
    public void DurationInDays_WhenDatesArePartiallySet_ShouldReturnNull()
    {
        // Arrange & Act
        var periodWithStartOnly = new ScheduledPeriod(startDate: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var periodWithEndOnly = new ScheduledPeriod(endDate: new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.Null(periodWithStartOnly.DurationInDays);
        Assert.Null(periodWithEndOnly.DurationInDays);
    }

    [Fact(DisplayName = "日付が未設定の場合、DurationInDaysはnullを返す")]
    public void DurationInDays_WhenDatesAreNotSet_ShouldReturnNull()
    {
        // Arrange
        var scheduledPeriod = new ScheduledPeriod();

        // Act
        var duration = scheduledPeriod.DurationInDays;

        // Assert
        Assert.Null(duration);
    }

    [Fact(DisplayName = "両方の日付が設定されている場合のみバリデーションが実行される")]
    public void Constructor_ValidatesOnlyWhenBothDatesAreProvided()
    {
        // Arrange & Act - 開始日のみ設定（バリデーションはスキップ）
        var periodWithStartOnly = new ScheduledPeriod(startDate: new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero));

        // Assert - 例外がスローされない
        Assert.NotNull(periodWithStartOnly);
        Assert.Equal(new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero), periodWithStartOnly.StartDate);
        Assert.Null(periodWithStartOnly.EndDate);
    }

    [Fact(DisplayName = "工数が設定されている場合のみバリデーションが実行される")]
    public void Constructor_ValidatesEstimatedHoursOnlyWhenProvided()
    {
        // Arrange & Act - 工数未設定（バリデーションはスキップ）
        var periodWithoutHours = new ScheduledPeriod(
            startDate: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            endDate: new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero)
        );

        // Assert - 例外がスローされない
        Assert.NotNull(periodWithoutHours);
        Assert.Null(periodWithoutHours.EstimatedHours);
    }
}
