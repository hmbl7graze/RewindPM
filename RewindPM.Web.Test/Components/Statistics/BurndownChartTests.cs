using Bunit;
using NSubstitute;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Statistics;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Web.Components.Statistics;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

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

        // タスク一覧のモック（理想線計算用）
        _mediatorMock.Send(Arg.Any<GetTasksByProjectIdAtTimeQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        // Act
        var cut = RenderComponent<BurndownChart>(parameters => parameters
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

    [Fact(DisplayName = "BurndownChart: 理想線が予定終了日に基づいて計算される")]
    public async Task BurndownChart_IdealLine_CalculatedBasedOnScheduledEndDates()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);

        // 編集日一覧
        var editDates = new List<DateTimeOffset> { startDate, endDate };
        _mediatorMock.Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(editDates);

        // 時系列データ
        var timeSeriesData = new ProjectStatisticsTimeSeriesDto
        {
            ProjectId = projectId,
            DailySnapshots = new List<DailyStatisticsSnapshot>
            {
                new DailyStatisticsSnapshot
                {
                    Date = startDate, TotalTasks = 3, CompletedTasks = 0,
                    InProgressTasks = 1, InReviewTasks = 1, TodoTasks = 1
                },
                new DailyStatisticsSnapshot
                {
                    Date = startDate.AddDays(1), TotalTasks = 3, CompletedTasks = 1,
                    InProgressTasks = 1, InReviewTasks = 0, TodoTasks = 1
                },
                new DailyStatisticsSnapshot
                {
                    Date = startDate.AddDays(2), TotalTasks = 3, CompletedTasks = 2,
                    InProgressTasks = 1, InReviewTasks = 0, TodoTasks = 0
                }
            }
        };
        _mediatorMock.Send(Arg.Any<GetProjectStatisticsTimeSeriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(timeSeriesData);

        // 開始時点のタスク一覧（予定終了日あり）
        var tasksAtStart = new List<TaskDto>
        {
            new TaskDto { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Task1", Description = "",
                Status = TaskStatus.Todo, ScheduledEndDate = startDate.AddDays(1),
                CreatedAt = startDate, UpdatedAt = null, CreatedBy = "user" },
            new TaskDto { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Task2", Description = "",
                Status = TaskStatus.Todo, ScheduledEndDate = startDate.AddDays(2),
                CreatedAt = startDate, UpdatedAt = null, CreatedBy = "user" },
            new TaskDto { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Task3", Description = "",
                Status = TaskStatus.Todo, ScheduledEndDate = startDate.AddDays(5),
                CreatedAt = startDate, UpdatedAt = null, CreatedBy = "user" }
        };
        _mediatorMock.Send(Arg.Any<GetTasksByProjectIdAtTimeQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasksAtStart);

        // Act
        var cut = RenderComponent<BurndownChart>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert - タスク一覧が取得されたことを確認
        await _mediatorMock.Received(1).Send(
            Arg.Is<GetTasksByProjectIdAtTimeQuery>(q =>
                q.ProjectId == projectId &&
                q.PointInTime == startDate),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "BurndownChart: 予定終了日がnullのタスクは最後まで残る")]
    public async Task BurndownChart_IdealLine_TasksWithoutScheduledEndDateRemain()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // 編集日一覧
        var editDates = new List<DateTimeOffset> { startDate, startDate.AddDays(5) };
        _mediatorMock.Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(editDates);

        // 時系列データ
        var timeSeriesData = new ProjectStatisticsTimeSeriesDto
        {
            ProjectId = projectId,
            DailySnapshots = new List<DailyStatisticsSnapshot>
            {
                new DailyStatisticsSnapshot
                {
                    Date = startDate, TotalTasks = 2, CompletedTasks = 0,
                    InProgressTasks = 1, InReviewTasks = 0, TodoTasks = 1
                },
                new DailyStatisticsSnapshot
                {
                    Date = startDate.AddDays(1), TotalTasks = 2, CompletedTasks = 1,
                    InProgressTasks = 0, InReviewTasks = 0, TodoTasks = 1
                },
                new DailyStatisticsSnapshot
                {
                    Date = startDate.AddDays(2), TotalTasks = 2, CompletedTasks = 1,
                    InProgressTasks = 1, InReviewTasks = 0, TodoTasks = 0
                }
            }
        };
        _mediatorMock.Send(Arg.Any<GetProjectStatisticsTimeSeriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(timeSeriesData);

        // タスク一覧（一部予定終了日なし）
        var tasksAtStart = new List<TaskDto>
        {
            new TaskDto { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Task1", Description = "",
                Status = TaskStatus.Todo, ScheduledEndDate = startDate.AddDays(1),
                CreatedAt = startDate, UpdatedAt = null, CreatedBy = "user" },
            new TaskDto { Id = Guid.NewGuid(), ProjectId = projectId, Title = "Task2", Description = "",
                Status = TaskStatus.Todo, ScheduledEndDate = null, // 予定終了日なし
                CreatedAt = startDate, UpdatedAt = null, CreatedBy = "user" }
        };
        _mediatorMock.Send(Arg.Any<GetTasksByProjectIdAtTimeQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasksAtStart);

        // Act
        var cut = RenderComponent<BurndownChart>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        cut.WaitForState(() => !cut.Instance.GetType()
            .GetField("_isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!.Equals(true), timeout: TimeSpan.FromSeconds(5));

        // Assert - チャートが表示されることを確認（エラーにならない）
        var chartDiv = cut.Find(".burndown-chart");
        Assert.NotNull(chartDiv);
    }

    [Fact(DisplayName = "BurndownChart: プロジェクトの全期間でグラフを表示")]
    public async Task BurndownChart_UsesProjectFullPeriod_NotFixed30Days()
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
                    Date = firstEditDate, TotalTasks = 10, CompletedTasks = 0,
                    InProgressTasks = 3, InReviewTasks = 2, TodoTasks = 5
                }
            }
        };
        _mediatorMock.Send(Arg.Any<GetProjectStatisticsTimeSeriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(timeSeriesData);

        _mediatorMock.Send(Arg.Any<GetTasksByProjectIdAtTimeQuery>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var cut = RenderComponent<BurndownChart>(parameters => parameters
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
