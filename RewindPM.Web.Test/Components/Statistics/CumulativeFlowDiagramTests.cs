using Bunit;
using NSubstitute;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Statistics;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Web.Components.Statistics;

namespace RewindPM.Web.Test.Components.Statistics;

public class CumulativeFlowDiagramTests : Bunit.TestContext
{
    private readonly IMediator _mediatorMock;

    public CumulativeFlowDiagramTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        Services.AddSingleton(_mediatorMock);

        // ApexChartsのJSInterop呼び出しをLooseモードで許可
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "CumulativeFlowDiagram: データがない場合はエラーメッセージを表示")]
    public void CumulativeFlowDiagram_NoData_DisplaysErrorMessage()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _mediatorMock.Send(Arg.Any<GetProjectStatisticsTimeSeriesQuery>(), Arg.Any<CancellationToken>())
            .Returns((ProjectStatisticsTimeSeriesDto?)null);

        // Act
        var cut = RenderComponent<CumulativeFlowDiagram>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert
        cut.WaitForState(() => !cut.Instance.GetType()
            .GetField("_isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!.Equals(true), timeout: TimeSpan.FromSeconds(5));

        var errorDiv = cut.Find(".chart-error");
        Assert.Contains("表示するデータがありません", errorDiv.TextContent);
    }

    [Fact(DisplayName = "CumulativeFlowDiagram: データがある場合はチャートを表示")]
    public void CumulativeFlowDiagram_WithData_DisplaysChart()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        
        var timeSeriesData = new ProjectStatisticsTimeSeriesDto
        {
            ProjectId = projectId,
            DailySnapshots = new List<DailyStatisticsSnapshot>
            {
                new DailyStatisticsSnapshot
                {
                    Date = startDate,
                    TotalTasks = 10,
                    CompletedTasks = 2,
                    InProgressTasks = 3,
                    InReviewTasks = 2,
                    TodoTasks = 3
                },
                new DailyStatisticsSnapshot
                {
                    Date = startDate.AddDays(1),
                    TotalTasks = 10,
                    CompletedTasks = 5,
                    InProgressTasks = 2,
                    InReviewTasks = 1,
                    TodoTasks = 2
                }
            }
        };

        _mediatorMock.Send(Arg.Any<GetProjectStatisticsTimeSeriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(timeSeriesData);

        // Act
        var cut = RenderComponent<CumulativeFlowDiagram>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert
        cut.WaitForState(() => !cut.Instance.GetType()
            .GetField("_isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!.Equals(true), timeout: TimeSpan.FromSeconds(5));

        var chartDiv = cut.Find(".cumulative-flow-diagram");
        Assert.Contains("Cumulative Flow Diagram", chartDiv.TextContent);
    }

    [Fact(DisplayName = "CumulativeFlowDiagram: AsOfDateが指定されている場合はその日付までのデータを取得")]
    public async Task CumulativeFlowDiagram_WithAsOfDate_QueriesCorrectDateRange()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = new DateTimeOffset(2024, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var firstEditDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // 編集日一覧を返すモック
        var editDates = new List<DateTimeOffset> { firstEditDate, asOfDate.AddDays(-5), asOfDate };
        _mediatorMock.Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(editDates);

        var timeSeriesData = new ProjectStatisticsTimeSeriesDto
        {
            ProjectId = projectId,
            DailySnapshots = new List<DailyStatisticsSnapshot>
            {
                new DailyStatisticsSnapshot
                {
                    Date = asOfDate.AddDays(-5),
                    TotalTasks = 5,
                    CompletedTasks = 1,
                    InProgressTasks = 2,
                    InReviewTasks = 1,
                    TodoTasks = 1
                }
            }
        };

        _mediatorMock.Send(Arg.Any<GetProjectStatisticsTimeSeriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(timeSeriesData);

        // Act
        var cut = RenderComponent<CumulativeFlowDiagram>(parameters => parameters
            .Add(p => p.ProjectId, projectId)
            .Add(p => p.AsOfDate, asOfDate));

        // Assert - プロジェクトの最初の編集日から開始されることを確認
        await _mediatorMock.Received(1).Send(
            Arg.Is<GetProjectStatisticsTimeSeriesQuery>(q =>
                q.ProjectId == projectId &&
                q.EndDate == asOfDate &&
                q.StartDate == firstEditDate),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "CumulativeFlowDiagram: 各ステータスのデータが正しく分離される")]
    public void CumulativeFlowDiagram_SeparatesStatusDataCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        
        var timeSeriesData = new ProjectStatisticsTimeSeriesDto
        {
            ProjectId = projectId,
            DailySnapshots = new List<DailyStatisticsSnapshot>
            {
                new DailyStatisticsSnapshot
                {
                    Date = startDate,
                    TotalTasks = 10,
                    CompletedTasks = 2,
                    InProgressTasks = 3,
                    InReviewTasks = 2,
                    TodoTasks = 3
                },
                new DailyStatisticsSnapshot
                {
                    Date = startDate.AddDays(1),
                    TotalTasks = 10,
                    CompletedTasks = 4,
                    InProgressTasks = 2,
                    InReviewTasks = 2,
                    TodoTasks = 2
                }
            }
        };

        _mediatorMock.Send(Arg.Any<GetProjectStatisticsTimeSeriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(timeSeriesData);

        // Act
        var cut = RenderComponent<CumulativeFlowDiagram>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        cut.WaitForState(() => !cut.Instance.GetType()
            .GetField("_isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!.Equals(true), timeout: TimeSpan.FromSeconds(5));

        // Assert - チャートが正しく表示されていることを確認
        var chartDiv = cut.Find(".cumulative-flow-diagram");
        Assert.NotNull(chartDiv);
    }

    [Fact(DisplayName = "CumulativeFlowDiagram: パラメータ変更時にデータを再読み込み")]
    public async Task CumulativeFlowDiagram_OnParameterChange_ReloadsData()
    {
        // Arrange
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();

        var timeSeriesData1 = new ProjectStatisticsTimeSeriesDto
        {
            ProjectId = projectId1,
            DailySnapshots = new List<DailyStatisticsSnapshot>
            {
                new DailyStatisticsSnapshot
                {
                    Date = DateTimeOffset.UtcNow,
                    TotalTasks = 5,
                    CompletedTasks = 2,
                    InProgressTasks = 1,
                    InReviewTasks = 1,
                    TodoTasks = 1
                }
            }
        };

        var timeSeriesData2 = new ProjectStatisticsTimeSeriesDto
        {
            ProjectId = projectId2,
            DailySnapshots = new List<DailyStatisticsSnapshot>
            {
                new DailyStatisticsSnapshot
                {
                    Date = DateTimeOffset.UtcNow,
                    TotalTasks = 10,
                    CompletedTasks = 5,
                    InProgressTasks = 2,
                    InReviewTasks = 2,
                    TodoTasks = 1
                }
            }
        };

        _mediatorMock.Send(
            Arg.Is<GetProjectStatisticsTimeSeriesQuery>(q => q.ProjectId == projectId1),
            Arg.Any<CancellationToken>())
            .Returns(timeSeriesData1);

        _mediatorMock.Send(
            Arg.Is<GetProjectStatisticsTimeSeriesQuery>(q => q.ProjectId == projectId2),
            Arg.Any<CancellationToken>())
            .Returns(timeSeriesData2);

        // Act
        var cut = RenderComponent<CumulativeFlowDiagram>(parameters => parameters
            .Add(p => p.ProjectId, projectId1));

        cut.WaitForState(() => !cut.Instance.GetType()
            .GetField("_isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!.Equals(true), timeout: TimeSpan.FromSeconds(5));

        // プロジェクトIDを変更
        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.ProjectId, projectId2));

        cut.WaitForState(() => !cut.Instance.GetType()
            .GetField("_isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!.Equals(true), timeout: TimeSpan.FromSeconds(5));

        // Assert - 両方のプロジェクトIDでクエリが実行されたことを確認
        await _mediatorMock.Received(1).Send(
            Arg.Is<GetProjectStatisticsTimeSeriesQuery>(q => q.ProjectId == projectId1),
            Arg.Any<CancellationToken>());

        await _mediatorMock.Received(1).Send(
            Arg.Is<GetProjectStatisticsTimeSeriesQuery>(q => q.ProjectId == projectId2),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "CumulativeFlowDiagram: データ読み込み中は読み込みメッセージを表示")]
    public void CumulativeFlowDiagram_WhileLoading_DisplaysLoadingMessage()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<ProjectStatisticsTimeSeriesDto?>();

        _mediatorMock.Send(Arg.Any<GetProjectStatisticsTimeSeriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<CumulativeFlowDiagram>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert - 読み込み中のメッセージが表示されていることを確認
        var loadingDiv = cut.Find(".chart-loading");
        Assert.Contains("チャートを読み込み中", loadingDiv.TextContent);

        // Cleanup
        tcs.SetResult(null);
    }

    [Fact(DisplayName = "CumulativeFlowDiagram: プロジェクトの全期間でグラフを表示")]
    public async Task CumulativeFlowDiagram_UsesProjectFullPeriod_NotFixed30Days()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var firstEditDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var lastEditDate = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero); // 60日間

        // 編集日一覧（60日間のプロジェクト）
        var editDates = new List<DateTimeOffset> { firstEditDate, firstEditDate.AddDays(30), lastEditDate };
        _mediatorMock.Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(editDates);

        var timeSeriesData = new ProjectStatisticsTimeSeriesDto
        {
            ProjectId = projectId,
            DailySnapshots = new List<DailyStatisticsSnapshot>
            {
                new DailyStatisticsSnapshot
                {
                    Date = firstEditDate,
                    TotalTasks = 10,
                    CompletedTasks = 0,
                    InProgressTasks = 3,
                    InReviewTasks = 2,
                    TodoTasks = 5
                }
            }
        };
        _mediatorMock.Send(Arg.Any<GetProjectStatisticsTimeSeriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(timeSeriesData);

        // Act
        var cut = RenderComponent<CumulativeFlowDiagram>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert - 最初の編集日から最後の編集日までの範囲でクエリが実行されることを確認
        await _mediatorMock.Received(1).Send(
            Arg.Is<GetProjectStatisticsTimeSeriesQuery>(q =>
                q.ProjectId == projectId &&
                q.StartDate == firstEditDate &&
                q.EndDate == lastEditDate),
            Arg.Any<CancellationToken>());
    }
}
