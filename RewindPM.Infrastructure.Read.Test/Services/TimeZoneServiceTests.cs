using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RewindPM.Infrastructure.Read.Configuration;
using RewindPM.Infrastructure.Read.Services;

namespace RewindPM.Infrastructure.Read.Test.Services;

/// <summary>
/// TimeZoneServiceのテスト
/// </summary>
public class TimeZoneServiceTests
{
    private readonly ILogger<TimeZoneService> _logger;

    public TimeZoneServiceTests()
    {
        _logger = Substitute.For<ILogger<TimeZoneService>>();
    }

    [Fact(DisplayName = "有効なタイムゾーンIDでTimeZoneInfoが正しく初期化されること")]
    public void Constructor_Should_Initialize_TimeZone_With_Valid_TimeZoneId()
    {
        // Arrange
        var settings = Options.Create(new TimeZoneSettings { TimeZoneId = "UTC" });

        // Act
        var service = new TimeZoneService(settings, _logger);

        // Assert
        Assert.NotNull(service.TimeZone);
        Assert.Equal("UTC", service.TimeZone.Id);
    }

    [Fact(DisplayName = "無効なタイムゾーンIDでUTCにフォールバックすること")]
    public void Constructor_Should_Fallback_To_UTC_When_Invalid_TimeZoneId()
    {
        // Arrange
        var settings = Options.Create(new TimeZoneSettings { TimeZoneId = "Invalid/TimeZone" });

        // Act
        var service = new TimeZoneService(settings, _logger);

        // Assert
        Assert.NotNull(service.TimeZone);
        Assert.Equal("UTC", service.TimeZone.Id);

        // ログに警告が記録されることを確認（NSubstituteではReceivedで確認）
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("Invalid TimeZone ID")),
            Arg.Any<TimeZoneNotFoundException>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(DisplayName = "UTC時刻から正しくスナップショット日付を計算できること（UTC）")]
    public void GetSnapshotDate_Should_Calculate_Correct_Date_For_UTC()
    {
        // Arrange
        var settings = Options.Create(new TimeZoneSettings { TimeZoneId = "UTC" });
        var service = new TimeZoneService(settings, _logger);
        var utcDateTime = new DateTimeOffset(2025, 12, 11, 23, 30, 0, TimeSpan.Zero);

        // Act
        var snapshotDate = service.GetSnapshotDate(utcDateTime);

        // Assert
        Assert.Equal(new DateTime(2025, 12, 11), snapshotDate);
    }

    [Fact(DisplayName = "UTC時刻から正しくスナップショット日付を計算できること（JST）")]
    public void GetSnapshotDate_Should_Calculate_Correct_Date_For_JST()
    {
        // Arrange
        // JSTは "Tokyo Standard Time" (Windows) または "Asia/Tokyo" (Linux/macOS)
        var timeZoneId = GetJstTimeZoneId();
        var settings = Options.Create(new TimeZoneSettings { TimeZoneId = timeZoneId });
        var service = new TimeZoneService(settings, _logger);
        
        // UTC 2025-12-11 15:00 は JST 2025-12-12 00:00
        var utcDateTime = new DateTimeOffset(2025, 12, 11, 15, 0, 0, TimeSpan.Zero);

        // Act
        var snapshotDate = service.GetSnapshotDate(utcDateTime);

        // Assert
        Assert.Equal(new DateTime(2025, 12, 12), snapshotDate);
    }

    [Fact(DisplayName = "タイムゾーン境界をまたぐ日付変換が正しく動作すること")]
    public void GetSnapshotDate_Should_Handle_TimeZone_Boundary_Correctly()
    {
        // Arrange
        var timeZoneId = GetJstTimeZoneId();
        var settings = Options.Create(new TimeZoneSettings { TimeZoneId = timeZoneId });
        var service = new TimeZoneService(settings, _logger);
        
        // UTC 2025-12-11 14:59:59 は JST 2025-12-11 23:59:59
        var beforeBoundary = new DateTimeOffset(2025, 12, 11, 14, 59, 59, TimeSpan.Zero);
        
        // UTC 2025-12-11 15:00:00 は JST 2025-12-12 00:00:00
        var afterBoundary = new DateTimeOffset(2025, 12, 11, 15, 0, 0, TimeSpan.Zero);

        // Act
        var dateBefore = service.GetSnapshotDate(beforeBoundary);
        var dateAfter = service.GetSnapshotDate(afterBoundary);

        // Assert
        Assert.Equal(new DateTime(2025, 12, 11), dateBefore);
        Assert.Equal(new DateTime(2025, 12, 12), dateAfter);
    }

    [Fact(DisplayName = "UTC時刻を正しくローカル時刻に変換できること")]
    public void ConvertUtcToLocal_Should_Convert_UTC_To_Local_Correctly()
    {
        // Arrange
        var timeZoneId = GetJstTimeZoneId();
        var settings = Options.Create(new TimeZoneSettings { TimeZoneId = timeZoneId });
        var service = new TimeZoneService(settings, _logger);
        var utcDateTime = new DateTimeOffset(2025, 12, 11, 15, 0, 0, TimeSpan.Zero);

        // Act
        var localDateTime = service.ConvertUtcToLocal(utcDateTime);

        // Assert
        Assert.Equal(new DateTimeOffset(2025, 12, 12, 0, 0, 0, TimeSpan.FromHours(9)), localDateTime);
    }

    /// <summary>
    /// プラットフォームに応じたJSTのタイムゾーンIDを取得
    /// </summary>
    private static string GetJstTimeZoneId()
    {
        // Windowsでは "Tokyo Standard Time"、Linux/macOSでは "Asia/Tokyo"
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
            return "Asia/Tokyo";
        }
        catch (TimeZoneNotFoundException)
        {
            return "Tokyo Standard Time";
        }
    }
}
