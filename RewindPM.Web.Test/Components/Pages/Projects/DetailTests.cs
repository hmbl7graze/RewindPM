using Bunit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Application.Read.Queries.Tasks;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;
using ProjectsDetail = RewindPM.Web.Components.Pages.Projects.Detail;

namespace RewindPM.Web.Test.Components.Pages.Projects;

public class DetailTests : Bunit.TestContext
{
    private readonly IMediator _mediatorMock;
    private readonly Guid _testProjectId = Guid.NewGuid();

    public DetailTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        Services.AddSingleton(_mediatorMock);
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
        Assert.Contains("Back to Projects", backLink.TextContent);

        var title = cut.Find(".project-title");
        Assert.Equal("Test Project", title.TextContent);

        var buttons = cut.FindAll("button, a.btn");
        Assert.Contains(buttons, b => b.TextContent.Contains("Edit Project"));
        Assert.Contains(buttons, b => b.TextContent.Contains("Add Task"));
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

        // Assert
        var description = cut.Find(".project-description");
        Assert.Contains("Test Description", description.TextContent);
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

        // Assert - Gantt chart rows
        var ganttRows = cut.FindAll(".gantt-task-name-cell");
        Assert.Equal(2, ganttRows.Count);

        // Task 1の検証
        var task1 = ganttRows[0];
        Assert.Contains("Task 1", task1.TextContent);
        Assert.Contains("TODO", task1.TextContent);

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

        // Assert - Gantt chart status badges
        var statusBadges = cut.FindAll(".gantt-task-status");
        Assert.Contains(statusBadges, b => b.ClassList.Contains("gantt-status-todo"));
        Assert.Contains(statusBadges, b => b.ClassList.Contains("gantt-status-inprogress"));
    }

    [Fact(DisplayName = "Add Taskボタンクリック時にタスク作成モーダルが開く")]
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
        var addTaskButton = cut.FindAll("button").First(b => b.TextContent.Contains("Add Task"));
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
}
