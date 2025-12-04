namespace RewindPM.Domain.Common;

/// <summary>
/// ドメイン層の不変条件違反を表す例外
/// Application層のバリデーションが正しく動作していれば、通常は発生しない
/// 発生した場合はバリデーション漏れの可能性がある
/// </summary>
public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
