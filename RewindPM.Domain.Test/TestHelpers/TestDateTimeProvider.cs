using RewindPM.Domain.Common;

namespace RewindPM.Domain.Test.TestHelpers;

/// <summary>
/// テスト用のIDateTimeProvider実装
/// </summary>
public class TestDateTimeProvider : IDateTimeProvider
{
    private DateTime _currentTime;

    public TestDateTimeProvider(DateTime? fixedTime = null)
    {
        _currentTime = fixedTime ?? DateTime.UtcNow;
    }

    public DateTime UtcNow => _currentTime;

    public void SetTime(DateTime time)
    {
        _currentTime = time;
    }

    public void Advance(TimeSpan duration)
    {
        _currentTime = _currentTime.Add(duration);
    }
}
