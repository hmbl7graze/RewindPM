namespace RewindPM.Domain.Common;

/// <summary>
/// 楽観的同時実行制御の違反時にスローされる例外
/// DomainExceptionを継承し、ドメイン層の例外として統一的に扱えるようにする
/// </summary>
public class ConcurrencyException : DomainException
{
    /// <summary>
    /// 競合が発生したAggregateのID
    /// </summary>
    public Guid AggregateId { get; }

    /// <summary>
    /// 期待されていたバージョン
    /// </summary>
    public int ExpectedVersion { get; }

    /// <summary>
    /// 実際のバージョン
    /// </summary>
    public int ActualVersion { get; }

    public ConcurrencyException(Guid aggregateId, int expectedVersion, int actualVersion)
        : base($"Aggregate {aggregateId} の同時実行制御違反: 期待バージョン={expectedVersion}, 実際バージョン={actualVersion}")
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public ConcurrencyException(string message) : base(message)
    {
    }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
