using Bunit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Statistics;
using RewindPM.Web.Components.Statistics;
using BunitTestContext = Bunit.TestContext;

namespace RewindPM.Web.Test.Components.Statistics;

public class ProjectStatisticsDashboardTests : BunitTestContext
{
    private readonly IMediator _mediator;

    public ProjectStatisticsDashboardTests()
    {
        _mediator = Substitute.For<IMediator>();
        Services.AddSingleton(_mediator);

        // ApexChartsのJSInterop呼び出しをLooseモードで許可
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "統計ダッシュボード: 初期化時に統計情報を読み込む")]
    public void ProjectStatisticsDashboard_OnInitialized_LoadsStatistics()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statisticsDto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 80,
            RemainingEstimatedHours = 60,
            OnTimeTasks = 4,
            DelayedTasks = 1,
            AverageDelayDays = 2.5,
            AccurateEstimateTasks = 0,
            OverEstimateTasks = 0,
            UnderEstimateTasks = 0,
            AverageEstimateErrorDays = 0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        _mediator.Send(Arg.Any<GetProjectStatisticsDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns(statisticsDto);

        // Act
        var cut = RenderComponent<ProjectStatisticsDashboard>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("プロジェクト統計", markup);
            Assert.Contains("50%", markup); // CompletionRate
            Assert.Contains("10", markup); // TotalTasks
            Assert.Contains("5", markup); // CompletedTasks
        });

        // パラメータ変更チェックにより、初期化時は1回のみ呼ばれる
        _mediator.Received(1).Send(
            Arg.Is<GetProjectStatisticsDetailQuery>(q => q.ProjectId == projectId),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "統計ダッシュボード: 基準日指定時、クエリに日付を渡す")]
    public void ProjectStatisticsDashboard_WithAsOfDate_PassesDateToQuery()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var statisticsDto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 5,
            CompletedTasks = 2,
            InProgressTasks = 2,
            InReviewTasks = 1,
            TodoTasks = 0,
            TotalEstimatedHours = 50,
            TotalActualHours = 40,
            RemainingEstimatedHours = 30,
            OnTimeTasks = 2,
            DelayedTasks = 0,
            AverageDelayDays = 0,
            AccurateEstimateTasks = 0,
            OverEstimateTasks = 0,
            UnderEstimateTasks = 0,
            AverageEstimateErrorDays = 0,
            AsOfDate = asOfDate
        };

        _mediator.Send(Arg.Any<GetProjectStatisticsDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns(statisticsDto);

        // Act
        var cut = RenderComponent<ProjectStatisticsDashboard>(parameters => parameters
            .Add(p => p.ProjectId, projectId)
            .Add(p => p.AsOfDate, asOfDate));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("リワインド表示中", markup);
            Assert.Contains("2024年01月01日", markup);
        });

        // パラメータ変更チェックにより、初期化時は1回のみ呼ばれる
        _mediator.Received(1).Send(
            Arg.Is<GetProjectStatisticsDetailQuery>(q => 
                q.ProjectId == projectId && q.AsOfDate == asOfDate),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "統計ダッシュボード: 統計情報がnullの場合、エラーを表示")]
    public void ProjectStatisticsDashboard_WithNullStatistics_ShowsError()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetProjectStatisticsDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns((ProjectStatisticsDetailDto?)null);

        // Act
        var cut = RenderComponent<ProjectStatisticsDashboard>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("統計情報を取得できませんでした", markup);
        });
    }

    [Fact(DisplayName = "統計ダッシュボード: タスク統計を表示する")]
    public void ProjectStatisticsDashboard_DisplaysTaskStatistics()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statisticsDto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 20,
            CompletedTasks = 10,
            InProgressTasks = 5,
            InReviewTasks = 3,
            TodoTasks = 2,
            TotalEstimatedHours = 200,
            TotalActualHours = 150,
            RemainingEstimatedHours = 100,
            OnTimeTasks = 8,
            DelayedTasks = 2,
            AverageDelayDays = 1.5,
            AccurateEstimateTasks = 0,
            OverEstimateTasks = 0,
            UnderEstimateTasks = 0,
            AverageEstimateErrorDays = 0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        _mediator.Send(Arg.Any<GetProjectStatisticsDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns(statisticsDto);

        // Act
        var cut = RenderComponent<ProjectStatisticsDashboard>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            
            // タスク進捗カード
            Assert.Contains("タスク進捗", markup);
            Assert.Contains("合計:", markup);
            Assert.Contains("20", markup);
            Assert.Contains("完了:", markup);
            Assert.Contains("10", markup);
            Assert.Contains("進行中:", markup);
            Assert.Contains("5", markup);
            Assert.Contains("レビュー中:", markup);
            Assert.Contains("3", markup);
            Assert.Contains("未着手:", markup);
            Assert.Contains("2", markup);
        });
    }

    [Fact(DisplayName = "統計ダッシュボード: 工数統計を表示する")]
    public void ProjectStatisticsDashboard_DisplaysHoursStatistics()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statisticsDto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 120,
            RemainingEstimatedHours = 50,
            OnTimeTasks = 4,
            DelayedTasks = 1,
            AverageDelayDays = 2.0,
            AccurateEstimateTasks = 0,
            OverEstimateTasks = 0,
            UnderEstimateTasks = 0,
            AverageEstimateErrorDays = 0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        _mediator.Send(Arg.Any<GetProjectStatisticsDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns(statisticsDto);

        // Act
        var cut = RenderComponent<ProjectStatisticsDashboard>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            
            // 工数統計カード
            Assert.Contains("工数統計", markup);
            Assert.Contains("予定工数:", markup);
            Assert.Contains("100 h", markup);
            Assert.Contains("実績工数:", markup);
            Assert.Contains("120 h", markup);
            Assert.Contains("残予定工数:", markup);
            Assert.Contains("50 h", markup);
            Assert.Contains("工数差分:", markup);
            Assert.Contains("+20 h", markup); // Overrun
        });
    }

    [Fact(DisplayName = "統計ダッシュボード: スケジュール統計を表示する")]
    public void ProjectStatisticsDashboard_DisplaysScheduleStatistics()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var statisticsDto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 80,
            RemainingEstimatedHours = 60,
            OnTimeTasks = 4,
            DelayedTasks = 1,
            AverageDelayDays = 2.5,
            AccurateEstimateTasks = 0,
            OverEstimateTasks = 0,
            UnderEstimateTasks = 0,
            AverageEstimateErrorDays = 0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        _mediator.Send(Arg.Any<GetProjectStatisticsDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns(statisticsDto);

        // Act
        var cut = RenderComponent<ProjectStatisticsDashboard>(parameters => parameters
            .Add(p => p.ProjectId, projectId));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            
            // スケジュール統計カード
            Assert.Contains("スケジュール統計", markup);
            Assert.Contains("期限内:", markup);
            Assert.Contains("4", markup);
            Assert.Contains("遅延:", markup);
            Assert.Contains("1", markup);
            Assert.Contains("平均遅延:", markup);
            Assert.Contains("2.5 日", markup);
        });
    }

    [Fact(DisplayName = "統計ダッシュボード: パラメータ変更時、統計情報を再読み込みする")]
    public void ProjectStatisticsDashboard_OnParametersChanged_ReloadsStatistics()
    {
        // Arrange
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();
        var statisticsDto1 = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 80,
            RemainingEstimatedHours = 60,
            OnTimeTasks = 4,
            DelayedTasks = 1,
            AverageDelayDays = 2.5,
            AccurateEstimateTasks = 0,
            OverEstimateTasks = 0,
            UnderEstimateTasks = 0,
            AverageEstimateErrorDays = 0,
            AsOfDate = DateTimeOffset.UtcNow
        };
        var statisticsDto2 = new ProjectStatisticsDetailDto
        {
            TotalTasks = 20,
            CompletedTasks = 10,
            InProgressTasks = 6,
            InReviewTasks = 2,
            TodoTasks = 2,
            TotalEstimatedHours = 200,
            TotalActualHours = 160,
            RemainingEstimatedHours = 120,
            OnTimeTasks = 8,
            DelayedTasks = 2,
            AverageDelayDays = 3.0,
            AccurateEstimateTasks = 0,
            OverEstimateTasks = 0,
            UnderEstimateTasks = 0,
            AverageEstimateErrorDays = 0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        _mediator.Send(Arg.Is<GetProjectStatisticsDetailQuery>(q => q.ProjectId == projectId1), Arg.Any<CancellationToken>())
            .Returns(statisticsDto1);
        _mediator.Send(Arg.Is<GetProjectStatisticsDetailQuery>(q => q.ProjectId == projectId2), Arg.Any<CancellationToken>())
            .Returns(statisticsDto2);

        // Act
        var cut = RenderComponent<ProjectStatisticsDashboard>(parameters => parameters
            .Add(p => p.ProjectId, projectId1));

        cut.WaitForAssertion(() => Assert.Contains("10", cut.Markup));

        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.ProjectId, projectId2));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            Assert.Contains("20", markup); // 新しいプロジェクトのTotalTasks
        });

        // 初期レンダリング: projectId1でOnInitializedAsyncが実行されて1回
        // パラメータ変更: projectId2でOnParametersSetAsyncが実行されて1回
        _mediator.Received(1).Send(
            Arg.Is<GetProjectStatisticsDetailQuery>(q => q.ProjectId == projectId1),
            Arg.Any<CancellationToken>());
        _mediator.Received(1).Send(
            Arg.Is<GetProjectStatisticsDetailQuery>(q => q.ProjectId == projectId2),
            Arg.Any<CancellationToken>());
    }
}
