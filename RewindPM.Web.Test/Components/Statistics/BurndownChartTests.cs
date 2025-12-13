using Bunit;
using NSubstitute;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Statistics;
using RewindPM.Web.Components.Statistics;

namespace RewindPM.Web.Test.Components.Statistics;

public class BurndownChartTests : Bunit.TestContext
{
    private readonly IMediator _mediatorMock;

    public BurndownChartTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        Services.AddSingleton(_mediatorMock);

        // ApexChartsのJSInterop呼び出しをLooseモードで許可
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "BurndownChart: データがない場合はエラーメッセージを表示")]
    public void BurndownChart_NoData_DisplaysErrorMessage()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _mediatorMock.Send(Arg.Any<GetProjectStatisticsTimeSeriesQuery>(), Arg.Any<CancellationToken>())
            .Returns((ProjectStatisticsTimeSeriesDto?)null);

        // Act
        var cut = RenderComponent<BurndownChart>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert
        cut.WaitForState(() => !cut.Instance.GetType()
            .GetField("_isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!.Equals(true), timeout: TimeSpan.FromSeconds(5));

        var errorDiv = cut.Find(".chart-error");
        Assert.Contains("表示するデータがありません", errorDiv.TextContent);
    }

    [Fact(DisplayName = "BurndownChart: データがある場合はチャートを表示")]
    public void BurndownChart_WithData_DisplaysChart()
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
        var cut = RenderComponent<BurndownChart>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert
        cut.WaitForState(() => !cut.Instance.GetType()
            .GetField("_isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!.Equals(true), timeout: TimeSpan.FromSeconds(5));

        var chartDiv = cut.Find(".burndown-chart");
        Assert.Contains("Burndown Chart", chartDiv.TextContent);
    }

    [Fact(DisplayName = "BurndownChart: AsOfDateが指定されている場合はその日付までのデータを取得")]
    public async Task BurndownChart_WithAsOfDate_QueriesCorrectDateRange()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = new DateTimeOffset(2024, 2, 15, 0, 0, 0, TimeSpan.Zero);

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
        var cut = RenderComponent<BurndownChart>(parameters => parameters
            .Add(p => p.ProjectId, projectId)
            .Add(p => p.AsOfDate, asOfDate));

        // Assert
        await _mediatorMock.Received(1).Send(
            Arg.Is<GetProjectStatisticsTimeSeriesQuery>(q =>
                q.ProjectId == projectId &&
                q.EndDate == asOfDate &&
                q.StartDate == asOfDate.AddDays(-30)),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "BurndownChart: 1日分のデータのみの場合、ゼロ除算エラーが発生しない")]
    public void BurndownChart_WithOneDayData_NoZeroDivisionError()
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
                }
            }
        };

        _mediatorMock.Send(Arg.Any<GetProjectStatisticsTimeSeriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(timeSeriesData);

        // Act & Assert - 例外が発生しないことを確認
        var cut = RenderComponent<BurndownChart>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        cut.WaitForState(() => !cut.Instance.GetType()
            .GetField("_isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!.Equals(true), timeout: TimeSpan.FromSeconds(5));

        // エラーメッセージが表示されていないことを確認
        var chartElements = cut.FindAll(".chart-error");
        Assert.Empty(chartElements);
    }

    [Fact(DisplayName = "BurndownChart: パラメータ変更時にデータを再読み込み")]
    public async Task BurndownChart_OnParameterChange_ReloadsData()
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
        var cut = RenderComponent<BurndownChart>(parameters => parameters
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
}
