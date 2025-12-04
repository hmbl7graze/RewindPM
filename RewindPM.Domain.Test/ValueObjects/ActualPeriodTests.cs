using RewindPM.Domain.ValueObjects;

namespace RewindPM.Domain.Test.ValueObjects;

public class ActualPeriodTests
{
    [Fact(DisplayName = "パラメータなしでActualPeriodのインスタンスを作成すると全てnullになる")]
    public void Constructor_WithNoParameters_ShouldCreateInstanceWithAllNulls()
    {
        // Act
        var actualPeriod = new ActualPeriod();

        // Assert
        Assert.Null(actualPeriod.StartDate);
        Assert.Null(actualPeriod.EndDate);
        Assert.Null(actualPeriod.ActualHours);
    }

    [Fact(DisplayName = "有効な値でActualPeriodのインスタンスを作成できる")]
    public void Constructor_WithValidValues_ShouldCreateInstance()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var actualHours = 35;

        // Act
        var actualPeriod = new ActualPeriod(startDate, endDate, actualHours);

        // Assert
        Assert.Equal(startDate, actualPeriod.StartDate);
        Assert.Equal(endDate, actualPeriod.EndDate);
        Assert.Equal(actualHours, actualPeriod.ActualHours);
    }

    [Fact(DisplayName = "実績開始日のみを指定してActualPeriodのインスタンスを作成できる")]
    public void Constructor_WithOnlyStartDate_ShouldCreateInstance()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);

        // Act
        var actualPeriod = new ActualPeriod(startDate: startDate);

        // Assert
        Assert.Equal(startDate, actualPeriod.StartDate);
        Assert.Null(actualPeriod.EndDate);
        Assert.Null(actualPeriod.ActualHours);
    }

    [Fact(DisplayName = "実績終了日が実績開始日より前の場合、ArgumentExceptionをスローする")]
    public void Constructor_WhenEndDateIsBeforeStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 10);
        var endDate = new DateTime(2025, 1, 1);
        var actualHours = 35;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new ActualPeriod(startDate, endDate, actualHours));
        Assert.Equal("実績終了日は実績開始日より後でなければなりません", exception.Message);
    }

    [Fact(DisplayName = "実績終了日と実績開始日が同じ場合、ArgumentExceptionをスローする")]
    public void Constructor_WhenEndDateEqualsStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        var date = new DateTime(2025, 1, 1);
        var actualHours = 35;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new ActualPeriod(date, date, actualHours));
        Assert.Equal("実績終了日は実績開始日より後でなければなりません", exception.Message);
    }

    [Fact(DisplayName = "実績工数が0の場合、ArgumentExceptionをスローする")]
    public void Constructor_WhenActualHoursIsZero_ShouldThrowArgumentException()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var actualHours = 0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new ActualPeriod(startDate, endDate, actualHours));
        Assert.Equal("実績工数は正の数でなければなりません", exception.Message);
    }

    [Fact(DisplayName = "実績工数が負の数の場合、ArgumentExceptionをスローする")]
    public void Constructor_WhenActualHoursIsNegative_ShouldThrowArgumentException()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var actualHours = -10;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new ActualPeriod(startDate, endDate, actualHours));
        Assert.Equal("実績工数は正の数でなければなりません", exception.Message);
    }

    [Fact(DisplayName = "両方の日付が設定されている場合、DurationInDaysが期間の日数を正しく計算する")]
    public void DurationInDays_WithBothDates_ShouldCalculateCorrectly()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var actualPeriod = new ActualPeriod(startDate, endDate, 35);

        // Act
        var duration = actualPeriod.DurationInDays;

        // Assert
        Assert.Equal(9, duration);
    }

    [Fact(DisplayName = "実績開始日のみ設定されている場合、DurationInDaysはnullを返す")]
    public void DurationInDays_WithOnlyStartDate_ShouldReturnNull()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var actualPeriod = new ActualPeriod(startDate: startDate);

        // Act
        var duration = actualPeriod.DurationInDays;

        // Assert
        Assert.Null(duration);
    }

    [Fact(DisplayName = "日付が設定されていない場合、DurationInDaysはnullを返す")]
    public void DurationInDays_WithNoDates_ShouldReturnNull()
    {
        // Arrange
        var actualPeriod = new ActualPeriod();

        // Act
        var duration = actualPeriod.DurationInDays;

        // Assert
        Assert.Null(duration);
    }

    [Fact(DisplayName = "実績開始日が設定されている場合、IsStartedはtrueを返す")]
    public void IsStarted_WhenStartDateIsSet_ShouldReturnTrue()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var actualPeriod = new ActualPeriod(startDate: startDate);

        // Act & Assert
        Assert.True(actualPeriod.IsStarted);
    }

    [Fact(DisplayName = "実績開始日が設定されていない場合、IsStartedはfalseを返す")]
    public void IsStarted_WhenStartDateIsNull_ShouldReturnFalse()
    {
        // Arrange
        var actualPeriod = new ActualPeriod();

        // Act & Assert
        Assert.False(actualPeriod.IsStarted);
    }

    [Fact(DisplayName = "実績終了日が設定されている場合、IsCompletedはtrueを返す")]
    public void IsCompleted_WhenEndDateIsSet_ShouldReturnTrue()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var actualPeriod = new ActualPeriod(startDate, endDate);

        // Act & Assert
        Assert.True(actualPeriod.IsCompleted);
    }

    [Fact(DisplayName = "実績終了日が設定されていない場合、IsCompletedはfalseを返す")]
    public void IsCompleted_WhenEndDateIsNull_ShouldReturnFalse()
    {
        // Arrange
        var actualPeriod = new ActualPeriod();

        // Act & Assert
        Assert.False(actualPeriod.IsCompleted);
    }

    [Fact(DisplayName = "同じ値を持つActualPeriodインスタンスは等価である")]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var period1 = new ActualPeriod(startDate, endDate, 35);
        var period2 = new ActualPeriod(startDate, endDate, 35);

        // Act & Assert
        Assert.Equal(period1, period2);
        Assert.True(period1 == period2);
    }

    [Fact(DisplayName = "異なる値を持つActualPeriodインスタンスは等価でない")]
    public void RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var period1 = new ActualPeriod(startDate, endDate, 35);
        var period2 = new ActualPeriod(startDate, endDate, 40);

        // Act & Assert
        Assert.NotEqual(period1, period2);
        Assert.True(period1 != period2);
    }

    [Fact(DisplayName = "全てnullのActualPeriodインスタンスは等価である")]
    public void RecordEquality_WithAllNulls_ShouldBeEqual()
    {
        // Arrange
        var period1 = new ActualPeriod();
        var period2 = new ActualPeriod();

        // Act & Assert
        Assert.Equal(period1, period2);
        Assert.True(period1 == period2);
    }
}
