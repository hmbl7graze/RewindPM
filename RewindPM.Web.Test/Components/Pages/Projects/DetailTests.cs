using Bunit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Application.Read.Queries.Statistics;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;
using ProjectsDetail = RewindPM.Web.Components.Pages.Projects.Detail;
using ProjectInfoModal = RewindPM.Web.Components.Pages.Projects.ProjectInfoModal;
using Microsoft.AspNetCore.Components;

namespace RewindPM.Web.Test.Components.Pages.Projects;

public class DetailTests : Bunit.TestContext
{
    private readonly IMediator _mediatorMock;
    private readonly Guid _testProjectId = Guid.NewGuid();

    public DetailTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        Services.AddSingleton(_mediatorMock);

        // 統計クエリのデフォルトモック設定
        _mediatorMock
            .Send(Arg.Any<GetProjectStatisticsDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestStatistics());
    }

    private ProjectDto CreateTestProject()
    {
        return new ProjectDto
        {
            Id = _testProjectId,
            Title = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            CreatedBy = "admin"
        };
    }

    private List<TaskDto> CreateTestTasks()
    {
        return new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = _testProjectId,
                Title = "Task 1",
                Description = "Task 1 Description",
                Status = TaskStatus.Todo,
                ScheduledStartDate = DateTime.Today,
                ScheduledEndDate = DateTime.Today.AddDays(5),
                EstimatedHours = 10,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "admin"
            },
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = _testProjectId,
                Title = "Task 2",
                Description = "Task 2 Description",
                Status = TaskStatus.InProgress,
                ScheduledStartDate = DateTime.Today,
                ScheduledEndDate = DateTime.Today.AddDays(3),
                EstimatedHours = 8,
                ActualStartDate = DateTime.Today,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "admin"
            }
        };
    }

    private ProjectStatisticsDetailDto CreateTestStatistics()
    {
        return new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 80,
            RemainingEstimatedHours = 20,
            OnTimeTasks = 4,
            DelayedTasks = 1,
            AverageDelayDays = 2,
            AccurateEstimateTasks = 0,
            OverEstimateTasks = 0,
            UnderEstimateTasks = 0,
            AverageEstimateErrorDays = 0,
            AsOfDate = DateTimeOffset.UtcNow
        };
    }

    [Fact(DisplayName = "初期表示時に読み込み中メッセージが表示される")]
    public async Task Detail_DisplaysLoadingMessage_Initially()
    {
        // Arrange
        var projectTcs = new TaskCompletionSource<ProjectDto?>();
        var tasksTcs = new TaskCompletionSource<List<TaskDto>>();

        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(projectTcs.Task);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasksTcs.Task);

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert
        var loadingState = cut.Find(".loading-state");
        Assert.Contains("読み込み中", loadingState.TextContent);

        // Cleanup
        projectTcs.SetResult(CreateTestProject());
        tasksTcs.SetResult(new List<TaskDto>());
    }

    [Fact(DisplayName = "プロジェクトが見つからない場合、エラーメッセージが表示される")]
    public void Detail_DisplaysErrorMessage_WhenProjectNotFound()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((ProjectDto?)null);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.Contains("プロジェクトが見つかりません", alert.TextContent);
    }

    [Fact(DisplayName = "プロジェクト詳細ヘッダーが正しく表示される")]
    public void Detail_DisplaysProjectHeader_Correctly()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert
        var backLink = cut.Find(".back-link");
        Assert.Contains("プロジェクト一覧に戻る", backLink.TextContent);

        var title = cut.Find(".project-title");
        Assert.Contains("Test Project", title.TextContent.Trim());

        var buttons = cut.FindAll("button, a.btn, a.btn-icon");
        Assert.Contains(buttons, b => b.TextContent.Contains("情報"));
        Assert.Contains(buttons, b => b.TextContent.Contains("新規タスク"));
    }

    [Fact(DisplayName = "プロジェクト情報が正しく表示される")]
    public void Detail_DisplaysProjectInfo_Correctly()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert - プロジェクト説明はProjectInfoModalに移動されたため、タイトルのみ検証
        var title = cut.Find(".project-title");
        Assert.Contains("Test Project", title.TextContent);
    }

    [Fact(DisplayName = "タスクが存在しない場合、空のメッセージが表示される")]
    public void Detail_DisplaysEmptyTasksMessage_WhenNoTasks()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // デフォルトはKanbanなので、Ganttビューに切り替える
        var ganttTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("ガントチャート"));
        ganttTab.Click();

        // Assert
        var emptyGantt = cut.Find(".gantt-empty");
        Assert.Contains("タスクがありません", emptyGantt.TextContent);
    }

    [Fact(DisplayName = "タスクが存在する場合、タスク一覧が表示される")]
    public void Detail_DisplaysTaskList_WhenTasksExist()
    {
        // Arrange
        var project = CreateTestProject();
        var tasks = CreateTestTasks();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasks);

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // デフォルトはKanbanなので、Ganttビューに切り替える
        var ganttTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("ガントチャート"));
        ganttTab.Click();

        // Assert - Gantt chart rows
        var ganttRows = cut.FindAll(".gantt-task-name-cell");
        Assert.Equal(2, ganttRows.Count);

        // Task 1の検証
        var task1 = ganttRows[0];
        Assert.Contains("Task 1", task1.TextContent);
        Assert.Contains("未着手", task1.TextContent);

        // Task 2の検証
        var task2 = ganttRows[1];
        Assert.Contains("Task 2", task2.TextContent);
        Assert.Contains("進行中", task2.TextContent);
    }

    [Fact(DisplayName = "タスクのステータスバッジが正しい色で表示される")]
    public void Detail_DisplaysTaskStatusBadges_WithCorrectColors()
    {
        // Arrange
        var project = CreateTestProject();
        var tasks = CreateTestTasks();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasks);

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // デフォルトはKanbanなので、Ganttビューに切り替える
        var ganttTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("ガントチャート"));
        ganttTab.Click();

        // Assert - Gantt chart status badges
        var statusBadges = cut.FindAll(".gantt-task-status");
        Assert.Contains(statusBadges, b => b.ClassList.Contains("gantt-status-todo"));
        Assert.Contains(statusBadges, b => b.ClassList.Contains("gantt-status-inprogress"));
    }

    [Fact(DisplayName = "新規タスクボタンクリック時にタスク作成モーダルが開く")]
    public void Detail_OpensTaskCreateModal_WhenAddTaskButtonClicked()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act
        var addTaskButton = cut.FindAll("button").First(b => b.TextContent.Contains("新規タスク"));
        addTaskButton.Click();

        // Assert - モーダルが表示されているか確認
        var modal = cut.Find(".modal-overlay");
        Assert.NotNull(modal);
    }

    [Fact(DisplayName = "タスククリック時にタスク編集モーダルが開く")]
    public void Detail_OpensTaskEditModal_WhenTaskItemClicked()
    {
        // Arrange
        var project = CreateTestProject();
        var tasks = CreateTestTasks();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasks);

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // デフォルトはKanbanなので、Ganttビューに切り替える
        var ganttTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("ガントチャート"));
        ganttTab.Click();

        // Act - Click on Gantt task name cell
        var taskNameCell = cut.Find(".gantt-task-name-cell");
        taskNameCell.Click();

        // Assert - モーダルが表示されているか確認
        var modal = cut.Find(".modal-overlay");
        Assert.NotNull(modal);
    }

    [Fact(DisplayName = "タスク作成成功時にタスク一覧が再読み込みされる")]
    public async Task Detail_ReloadsTasks_WhenTaskCreatedSuccessfully()
    {
        // Arrange
        var project = CreateTestProject();
        var initialTasks = new List<TaskDto>();
        var updatedTasks = CreateTestTasks();

        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);

        var callCount = 0;
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1 ? initialTasks : updatedTasks;
            });

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - タスク作成成功をシミュレート
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            await (instance.GetType()
                .GetMethod("HandleTaskModalSuccess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(instance, null) as Task ?? Task.CompletedTask);
        });

        // Assert
        await _mediatorMock.Received(2).Send(
            Arg.Any<GetTasksByProjectIdQuery>(),
            Arg.Any<CancellationToken>()); // 初期読み込み + 再読み込み
    }

    [Fact(DisplayName = "タスク読み込み失敗時にエラーを処理する")]
    public void Detail_HandlesError_WhenLoadingTasksFails()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns<List<TaskDto>>(_ => throw new Exception("Test error"));

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert - エラーが発生してもクラッシュせず、エラーメッセージが表示される
        var errorMessage = cut.Find(".alert-danger");
        Assert.Contains("Test error", errorMessage.TextContent);
    }

    // ========== リワインド機能のテスト ==========

    [Fact(DisplayName = "初期表示時に編集日一覧が取得されること")]
    public void Detail_LoadsEditDates_OnInitialization()
    {
        // Arrange
        var project = CreateTestProject();
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };

        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(editDates);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert - GetProjectEditDatesQueryが呼ばれたことを確認
        _mediatorMock.Received(1).Send(
            Arg.Any<GetProjectEditDatesQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "TimelineControlが表示されること")]
    public void Detail_DisplaysTimelineControl()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset> { new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-1), TimeSpan.Zero) });
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Rewindモードを有効化
        var RewindButton = cut.FindAll("button").First(b => b.TextContent.Contains("過去に戻る"));
        RewindButton.Click();

        // Assert - TimelineControlが表示されている
        var timelineControl = cut.Find(".timeline-toolbar");
        Assert.NotNull(timelineControl);
    }

    [Fact(DisplayName = "初期表示時（最新）は過去表示バナーが表示されないこと")]
    public void Detail_DoesNotDisplayPastBanner_Initially()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert - 過去表示バナーが表示されていない
        var banners = cut.FindAll(".past-view-banner");
        Assert.Empty(banners);
    }

    [Fact(DisplayName = "初期表示時（最新）は新規タスクボタンが有効化されていること")]
    public void Detail_AddTaskButtonEnabled_Initially()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert - 新規タスクボタンが有効
        var addTaskButton = cut.FindAll("button").First(b => b.TextContent.Contains("新規タスク"));
        Assert.False(addTaskButton.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "日付変更時にタスクが再読み込みされること")]
    public async Task Detail_ReloadsTasks_WhenDateChanged()
    {
        // Arrange
        var project = CreateTestProject();
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero)
        };
        var currentTasks = CreateTestTasks();
        var pastTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = _testProjectId,
                Title = "Past Task",
                Description = "Past Task Description",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
                EstimatedHours = 5,
                CreatedAt = new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
                CreatedBy = "admin",
                UpdatedAt = null
            }
        };

        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(editDates);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(currentTasks);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdAtTimeQuery>(), Arg.Any<CancellationToken>())
            .Returns(pastTasks);

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - 日付変更をシミュレート
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            var method = instance.GetType()
                .GetMethod("HandleDateChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (method!.Invoke(instance, new object?[] { new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero) }) as Task ?? Task.CompletedTask);
        });

        // Assert - GetTasksByProjectIdAtTimeQueryが呼ばれたことを確認
        await _mediatorMock.Received(1).Send(
            Arg.Any<GetTasksByProjectIdAtTimeQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "タスク作成成功時に編集日一覧も更新されること")]
    public async Task Detail_ReloadsEditDates_WhenTaskCreatedSuccessfully()
    {
        // Arrange
        var project = CreateTestProject();
        var initialEditDates = new List<DateTimeOffset> { new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero) };
        var updatedEditDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero)
        };

        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);

        var editDatesCallCount = 0;
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                editDatesCallCount++;
                return editDatesCallCount == 1 ? initialEditDates : updatedEditDates;
            });

        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - タスク作成成功をシミュレート
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            await (instance.GetType()
                .GetMethod("HandleTaskModalSuccess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(instance, null) as Task ?? Task.CompletedTask);
        });

        // Assert - GetProjectEditDatesQueryが2回呼ばれたことを確認（初期 + 更新）
        await _mediatorMock.Received(2).Send(
            Arg.Any<GetProjectEditDatesQuery>(),
            Arg.Any<CancellationToken>());
    }

    // ========== タブ切り替え機能のテスト ==========

    [Fact(DisplayName = "初期表示時にデフォルトでKanbanタブが選択されること")]
    public void Detail_DisplaysKanbanTab_ByDefault()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert - Kanbanタブがアクティブ
        var kanbanTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("カンバン"));
        Assert.Contains("active", kanbanTab.ClassName);

        // Kanbanボードが表示されている
        var kanbanBoard = cut.FindAll(".kanban-board, .kanban-empty");
        Assert.NotEmpty(kanbanBoard);
    }

    [Fact(DisplayName = "クエリパラメータ tab=kanban でKanbanタブが選択されること")]
    public void Detail_DisplaysKanbanTab_WhenQueryParameterIsKanban()
    {
        // Arrange
        var project = CreateTestProject();
        var tasks = CreateTestTasks();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasks);
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        // NavigationManagerのURIを設定
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/projects/{_testProjectId}?tab=kanban");

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert - Kanbanタブがアクティブ
        var kanbanTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("カンバン"));
        Assert.Contains("active", kanbanTab.ClassName);

        // Kanbanボードが表示されている
        var kanbanBoard = cut.Find(".kanban-board");
        Assert.NotNull(kanbanBoard);
    }

    [Fact(DisplayName = "クエリパラメータ tab=statistics でStatisticsタブが選択されること")]
    public void Detail_DisplaysStatisticsTab_WhenQueryParameterIsStatistics()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        // NavigationManagerのURIを設定
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/projects/{_testProjectId}?tab=statistics");

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert - Statisticsタブがアクティブ
        var statisticsTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("統計"));
        Assert.Contains("active", statisticsTab.ClassName);

        // Statisticsダッシュボードが表示されている（レンダリング完了を待つ）
        cut.WaitForAssertion(() =>
        {
            var statisticsDashboard = cut.Find(".statistics-dashboard");
            Assert.NotNull(statisticsDashboard);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "無効なクエリパラメータではデフォルトのKanbanタブが選択されること")]
    public void Detail_DisplaysKanbanTab_WhenQueryParameterIsInvalid()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        // NavigationManagerのURIを設定（無効なタブ値）
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/projects/{_testProjectId}?tab=invalid");

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert - デフォルトのKanbanタブがアクティブ
        var kanbanTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("カンバン"));
        Assert.Contains("active", kanbanTab.ClassName);

        // Kanbanボードが表示されている
        var kanbanBoard = cut.Find(".kanban-board");
        Assert.NotNull(kanbanBoard);
    }

    [Fact(DisplayName = "Kanbanタブボタンクリック時にKanbanビューが表示されること")]
    public void Detail_DisplaysKanbanView_WhenKanbanTabClicked()
    {
        // Arrange
        var project = CreateTestProject();
        var tasks = CreateTestTasks();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasks);
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - Kanbanタブをクリック
        var kanbanTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("カンバン"));
        kanbanTab.Click();

        // Assert - Kanbanボードが表示されている（レンダリング完了を待つ）
        cut.WaitForAssertion(() =>
        {
            var kanbanBoard = cut.Find(".kanban-board");
            Assert.NotNull(kanbanBoard);
        });

        // Kanbanタブがアクティブ
        var updatedKanbanTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("カンバン"));
        Assert.Contains("active", updatedKanbanTab.ClassName);
    }

    [Fact(DisplayName = "Statisticsタブボタンクリック時にStatisticsビューが表示されること")]
    public void Detail_DisplaysStatisticsView_WhenStatisticsTabClicked()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - Statisticsタブをクリック
        var statisticsTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("統計"));
        statisticsTab.Click();

        // Assert - Statisticsダッシュボードが表示されている（レンダリング完了を待つ）
        cut.WaitForAssertion(() =>
        {
            var statisticsDashboard = cut.Find(".statistics-dashboard");
            Assert.NotNull(statisticsDashboard);
        }, timeout: TimeSpan.FromSeconds(5));

        // Statisticsタブがアクティブ
        var updatedStatisticsTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("統計"));
        Assert.Contains("active", updatedStatisticsTab.ClassName);
    }

    [Fact(DisplayName = "タブ切り替え後に再度Ganttタブに戻れること")]
    public void Detail_CanSwitchBackToGanttTab_AfterChangingTab()
    {
        // Arrange
        var project = CreateTestProject();
        var tasks = CreateTestTasks();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasks);
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - Kanbanタブをクリック
        var kanbanTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("カンバン"));
        kanbanTab.Click();

        // Kanbanボードが表示されるのを待つ
        cut.WaitForAssertion(() =>
        {
            var kanbanBoard = cut.Find(".kanban-board");
            Assert.NotNull(kanbanBoard);
        });

        // 再度Ganttタブをクリック
        var ganttTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("ガントチャート"));
        ganttTab.Click();

        // Assert - Ganttチャートが表示されている（レンダリング完了を待つ）
        cut.WaitForAssertion(() =>
        {
            var ganttChart = cut.FindAll(".gantt-chart, .gantt-empty");
            Assert.NotEmpty(ganttChart);
        });

        // Ganttタブがアクティブ
        var updatedGanttTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("ガントチャート"));
        Assert.Contains("active", updatedGanttTab.ClassName);
    }

    // ========== ビュー切り替え時のモーダル状態管理テスト ==========

    [Fact(DisplayName = "ビュー切り替え時にモーダルが閉じられること")]
    public void Detail_ClosesModal_WhenViewIsToggled()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - モーダルを開く
        var addTaskButton = cut.FindAll("button").First(b => b.TextContent.Contains("新規タスク"));
        addTaskButton.Click();

        // モーダルが表示されていることを確認
        var modalBeforeToggle = cut.FindAll(".modal-overlay");
        Assert.NotEmpty(modalBeforeToggle);

        // ビューを切り替える（Gantt → Kanban）
        var kanbanButton = cut.FindAll("button").First(b => b.TextContent.Contains("カンバン") && b.ClassList.Contains("view-tab"));
        kanbanButton.Click();

        // Assert - モーダルが閉じられていることを確認
        var modalAfterToggle = cut.FindAll(".modal-overlay");
        Assert.Empty(modalAfterToggle);
    }

    [Fact(DisplayName = "タスク選択後にビューを切り替えるとモーダルが閉じられること")]
    public void Detail_ClosesTaskModal_WhenViewIsToggledAfterSelectingTask()
    {
        // Arrange
        var project = CreateTestProject();
        var tasks = CreateTestTasks();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasks);

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // デフォルトはKanbanなので、Ganttビューに切り替える
        var ganttTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("ガントチャート"));
        ganttTab.Click();

        // Act - タスクをクリックしてモーダルを開く
        var taskNameCell = cut.Find(".gantt-task-name-cell");
        taskNameCell.Click();

        // モーダルが表示されていることを確認
        var modalBeforeToggle = cut.FindAll(".modal-overlay");
        Assert.NotEmpty(modalBeforeToggle);

        // ビューを切り替える（Gantt → Statistics）
        var statisticsButton = cut.FindAll("button").First(b => b.TextContent.Contains("統計") && b.ClassList.Contains("view-tab"));
        statisticsButton.Click();

        // Assert - モーダルが閉じられていることを確認
        var modalAfterToggle = cut.FindAll(".modal-overlay");
        Assert.Empty(modalAfterToggle);
    }

    [Fact(DisplayName = "複数のビュー間を切り替えてもモーダル状態が正しく管理されること")]
    public void Detail_MaintainsCorrectModalState_WhenTogglingBetweenMultipleViews()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - モーダルを開く
        var addTaskButton = cut.FindAll("button").First(b => b.TextContent.Contains("新規タスク"));
        addTaskButton.Click();

        // Gantt → Kanban
        var kanbanButton = cut.FindAll("button").First(b => b.TextContent.Contains("カンバン") && b.ClassList.Contains("view-tab"));
        kanbanButton.Click();

        var modalAfterKanban = cut.FindAll(".modal-overlay");
        Assert.Empty(modalAfterKanban);

        // Kanban → Statistics
        var statisticsButton = cut.FindAll("button").First(b => b.TextContent.Contains("統計") && b.ClassList.Contains("view-tab"));
        statisticsButton.Click();

        var modalAfterStatistics = cut.FindAll(".modal-overlay");
        Assert.Empty(modalAfterStatistics);

        // Statistics → Gantt
        var ganttButton = cut.FindAll("button").First(b => b.TextContent.Contains("ガント") && b.ClassList.Contains("view-tab"));
        ganttButton.Click();

        var modalAfterGantt = cut.FindAll(".modal-overlay");
        Assert.Empty(modalAfterGantt);

        // Assert - 各ビュー切り替え後にモーダルが閉じていることを確認
        // 上記のアサートで既に検証済み
    }

    [Fact(DisplayName = "Infoボタンをクリックするとプロジェクト情報モーダルが表示される")]
    public void Detail_ShowsInfoModal_WhenInfoButtonClicked()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act
        var infoButton = cut.FindAll("button").First(b => b.TextContent.Contains("情報"));
        infoButton.Click();

        // Assert
        Assert.Contains("プロジェクト情報", cut.Markup);
    }

    [Fact(DisplayName = "プロジェクト情報更新後にプロジェクトデータが再読み込みされる")]
    public async Task Detail_ReloadsProjectData_AfterInfoUpdate()
    {
        // Arrange
        var project = CreateTestProject();
        var updatedProject = new ProjectDto
        {
            Id = _testProjectId,
            Title = "Updated Project",
            Description = "Updated Description",
            CreatedAt = project.CreatedAt,
            UpdatedAt = DateTime.Now,
            CreatedBy = project.CreatedBy,
            UpdatedBy = "test-user"
        };

        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project, updatedProject);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - プロジェクト情報モーダルのOnSuccessコールバックを直接呼び出す
        var modalComponent = cut.FindComponent<ProjectInfoModal>();
        await cut.InvokeAsync(async () => await modalComponent.Instance.OnSuccess.InvokeAsync());

        // Assert - 2回目のクエリが送信されたことを確認
        await _mediatorMock.Received(2).Send(
            Arg.Any<GetProjectByIdQuery>(),
            Arg.Any<CancellationToken>());
    }

    // ========== モーダル再表示テスト ==========

    [Fact(DisplayName = "タスクモーダルを閉じた後、再度開くことができる")]
    public void Detail_CanReopenTaskModal_AfterClosing()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - 1回目: モーダルを開く
        var addTaskButton = cut.FindAll("button").First(b => b.TextContent.Contains("新規タスク"));
        addTaskButton.Click();

        // モーダルが表示されていることを確認
        var modalFirstOpen = cut.FindAll(".modal-overlay");
        Assert.NotEmpty(modalFirstOpen);

        // モーダルを閉じる
        var closeButton = cut.FindAll("button").First(b => b.TextContent.Contains("キャンセル"));
        closeButton.Click();

        // モーダルが閉じられていることを確認
        var modalAfterClose = cut.FindAll(".modal-overlay");
        Assert.Empty(modalAfterClose);

        // Act - 2回目: 再度モーダルを開く
        addTaskButton = cut.FindAll("button").First(b => b.TextContent.Contains("新規タスク"));
        addTaskButton.Click();

        // Assert - モーダルが再度表示されていることを確認
        var modalSecondOpen = cut.FindAll(".modal-overlay");
        Assert.NotEmpty(modalSecondOpen);
    }

    [Fact(DisplayName = "プロジェクト情報モーダルを閉じた後、再度開くことができる")]
    public void Detail_CanReopenProjectInfoModal_AfterClosing()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - 1回目: プロジェクト情報モーダルを開く
        var infoButton = cut.FindAll("button").First(b => b.TextContent.Contains("情報"));
        infoButton.Click();

        // モーダルが表示されていることを確認
        var modalFirstOpen = cut.FindAll(".modal-overlay");
        Assert.NotEmpty(modalFirstOpen);

        // モーダルを閉じる (×ボタンをクリック)
        var closeButton = cut.Find(".modal-close-btn");
        closeButton.Click();

        // モーダルが閉じられていることを確認
        var modalAfterClose = cut.FindAll(".modal-overlay");
        Assert.Empty(modalAfterClose);

        // Act - 2回目: 再度プロジェクト情報モーダルを開く
        infoButton = cut.FindAll("button").First(b => b.TextContent.Contains("情報"));
        infoButton.Click();

        // Assert - モーダルが再度表示されていることを確認
        var modalSecondOpen = cut.FindAll(".modal-overlay");
        Assert.NotEmpty(modalSecondOpen);
    }

    [Fact(DisplayName = "タスク編集モーダルを閉じた後、再度開くことができる")]
    public void Detail_CanReopenTaskEditModal_AfterClosing()
    {
        // Arrange
        var project = CreateTestProject();
        var tasks = CreateTestTasks();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasks);

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // デフォルトはKanbanなので、Ganttビューに切り替える
        var ganttTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("ガントチャート"));
        ganttTab.Click();

        // Act - 1回目: タスクをクリックしてモーダルを開く
        var taskNameCell = cut.Find(".gantt-task-name-cell");
        taskNameCell.Click();

        // モーダルが表示されていることを確認
        var modalFirstOpen = cut.FindAll(".modal-overlay");
        Assert.NotEmpty(modalFirstOpen);

        // モーダルを閉じる
        var closeButton = cut.FindAll("button").First(b => b.TextContent.Contains("キャンセル"));
        closeButton.Click();

        // モーダルが閉じられていることを確認
        var modalAfterClose = cut.FindAll(".modal-overlay");
        Assert.Empty(modalAfterClose);

        // Act - 2回目: 再度タスクをクリックしてモーダルを開く
        taskNameCell = cut.Find(".gantt-task-name-cell");
        taskNameCell.Click();

        // Assert - モーダルが再度表示されていることを確認
        var modalSecondOpen = cut.FindAll(".modal-overlay");
        Assert.NotEmpty(modalSecondOpen);
    }

    [Fact(DisplayName = "リワインドモードで日付を変更すると統計ダッシュボードが再レンダリングされる")]
    public void Detail_ReRendersStatisticsDashboard_WhenDateChangesInRewindMode()
    {
        // Arrange
        var project = CreateTestProject();
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-2), TimeSpan.Zero),
            new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-1), TimeSpan.Zero)
        };

        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(editDates);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Rewindモードを有効化
        var rewindButton = cut.FindAll("button").First(b => b.TextContent.Contains("過去に戻る"));
        rewindButton.Click();

        // Statisticsタブに切り替え
        var statisticsTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("統計"));
        statisticsTab.Click();

        // 初回レンダリング時のコンポーネントを確認
        cut.WaitForAssertion(() =>
        {
            var statisticsDashboard = cut.Find(".statistics-dashboard");
            Assert.NotNull(statisticsDashboard);
        }, timeout: TimeSpan.FromSeconds(5));

        // Act - タイムラインで日付を変更
        var prevButton = cut.FindAll("button.timeline-btn-prev").First();
        prevButton.Click();

        // Assert - 統計ダッシュボードが再レンダリングされることを確認
        // （日付変更によりコンポーネントの@keyが変わり、新しいインスタンスが作成される）
        cut.WaitForAssertion(() =>
        {
            var statisticsDashboard = cut.Find(".statistics-dashboard");
            Assert.NotNull(statisticsDashboard);
        }, timeout: TimeSpan.FromSeconds(5));

        // GetProjectStatisticsDetailQueryが複数回呼ばれることを確認（初回+日付変更後）
        // 最低2回は呼ばれている
        _mediatorMock.Received(2)
            .Send(Arg.Any<GetProjectStatisticsDetailQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "リワインドモードでない場合、統計ダッシュボードのキーは常に'latest'である")]
    public void Detail_StatisticsDashboardKey_IsLatest_WhenNotInRewindMode()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        // Statisticsタブを表示
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/projects/{_testProjectId}?tab=statistics");

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert - 統計ダッシュボードが表示される
        cut.WaitForAssertion(() =>
        {
            var statisticsDashboard = cut.Find(".statistics-dashboard");
            Assert.NotNull(statisticsDashboard);
        }, timeout: TimeSpan.FromSeconds(5));

        // リワインドモードではないので、キーは "latest" となる
        // これにより、通常モードでは不要な再レンダリングが発生しないことを確認
        _mediatorMock.Received(1)
            .Send(Arg.Any<GetProjectStatisticsDetailQuery>(), Arg.Any<CancellationToken>());
    }

    // ========== 新規タスクボタンの表示制御テスト ==========

    [Fact(DisplayName = "統計タブ表示時に新規タスクボタンが非表示になること")]
    public void Detail_HidesNewTaskButton_WhenStatisticsTabIsActive()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - Statisticsタブをクリック
        var statisticsTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("統計"));
        statisticsTab.Click();

        // Assert - 新規タスクボタンが存在しないことを確認
        var newTaskButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("新規タスク"));
        Assert.Empty(newTaskButtons);
    }

    [Fact(DisplayName = "Kanbanタブ表示時に新規タスクボタンが表示されること")]
    public void Detail_ShowsNewTaskButton_WhenKanbanTabIsActive()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - Kanbanタブをクリック（デフォルトはKanbanだが、明示的にクリック）
        var kanbanTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("カンバン"));
        kanbanTab.Click();

        // Assert - 新規タスクボタンが表示されていることを確認
        var newTaskButton = cut.FindAll("button").First(b => b.TextContent.Contains("新規タスク"));
        Assert.NotNull(newTaskButton);
        Assert.False(newTaskButton.HasAttribute("disabled")); // 過去表示中でないので有効
    }

    [Fact(DisplayName = "Ganttタブ表示時に新規タスクボタンが表示されること")]
    public void Detail_ShowsNewTaskButton_WhenGanttTabIsActive()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - Ganttタブをクリック
        var ganttTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("ガントチャート"));
        ganttTab.Click();

        // Assert - 新規タスクボタンが表示されていることを確認
        var newTaskButton = cut.FindAll("button").First(b => b.TextContent.Contains("新規タスク"));
        Assert.NotNull(newTaskButton);
        Assert.False(newTaskButton.HasAttribute("disabled")); // 過去表示中でないので有効
    }

    [Fact(DisplayName = "統計タブから他のタブに切り替えた際に新規タスクボタンが再表示されること")]
    public void Detail_ShowsNewTaskButton_WhenSwitchingFromStatisticsToOtherTab()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act - Statisticsタブをクリック
        var statisticsTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("統計"));
        statisticsTab.Click();

        // 統計タブでボタンが非表示であることを確認
        var buttonsOnStatistics = cut.FindAll("button").Where(b => b.TextContent.Contains("新規タスク"));
        Assert.Empty(buttonsOnStatistics);

        // Kanbanタブに切り替え
        var kanbanTab = cut.FindAll(".view-tab").First(t => t.TextContent.Contains("カンバン"));
        kanbanTab.Click();

        // Assert - 新規タスクボタンが再表示されていることを確認
        var newTaskButton = cut.FindAll("button").First(b => b.TextContent.Contains("新規タスク"));
        Assert.NotNull(newTaskButton);
    }

    [Fact(DisplayName = "統計タブ表示時にクエリパラメータで直接アクセスした場合も新規タスクボタンが非表示になること")]
    public void Detail_HidesNewTaskButton_WhenAccessingStatisticsTabDirectlyViaQueryParameter()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());
        _mediatorMock
            .Send(Arg.Any<GetProjectEditDatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());

        // NavigationManagerのURIを設定（統計タブを直接指定）
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/projects/{_testProjectId}?tab=statistics");

        // Act
        var cut = RenderComponent<ProjectsDetail>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert - 新規タスクボタンが存在しないことを確認
        var newTaskButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("新規タスク"));
        Assert.Empty(newTaskButtons);
    }
}

