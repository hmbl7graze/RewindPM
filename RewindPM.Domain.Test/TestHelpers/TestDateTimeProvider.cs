using RewindPM.Domain.Common;

namespace RewindPM.Domain.Test.TestHelpers;

/// <summary>
/// テスト用のIDateTimeProvider実装
/// </summary>
public class TestDateTimeProvider : IDateTimeProvider
{
    private DateTimeOffset _currentTime;

    public TestDateTimeProvider(DateTimeOffset? fixedTime = null)
    {
        _currentTime = fixedTime ?? DateTimeOffset.UtcNow;
    }

    public DateTimeOffset UtcNow => _currentTime;

    public void SetTime(DateTimeOffset time)
    {
        _currentTime = time;
    }

    public void Advance(TimeSpan duration)
    {
        _currentTime = _currentTime.Add(duration);
    }
}
