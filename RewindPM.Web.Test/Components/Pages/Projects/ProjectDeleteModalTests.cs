using Bunit;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Web.Components.Pages.Projects;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Web.Test.Components.Pages.Projects;

public class ProjectDeleteModalTests : Bunit.TestContext
{
    private readonly IMediator _mediatorMock;
    private readonly Guid _testProjectId = Guid.NewGuid();

    public ProjectDeleteModalTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        Services.AddSingleton(_mediatorMock);
    }

    [Fact(DisplayName = "モーダルが表示される")]
    public void ProjectDeleteModal_IsVisible_WhenIsVisibleIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 0));

        // Assert
        var modalTitle = cut.Find(".modal-title");
        Assert.Contains("プロジェクトの削除", modalTitle.TextContent);
    }

    [Fact(DisplayName = "タスク数が0の場合、タスクなしのメッセージが表示される")]
    public void ProjectDeleteModal_DisplaysMessageWithoutTasks_WhenTaskCountIsZero()
    {
        // Arrange & Act
        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 0));

        // Assert
        var warningContent = cut.Find(".warning-content");
        Assert.Contains("このプロジェクトが完全に削除されます", warningContent.TextContent);
        Assert.DoesNotContain("task-count-badge", cut.Markup);
    }

    [Fact(DisplayName = "タスク数がある場合、タスク数を含むメッセージが表示される")]
    public void ProjectDeleteModal_DisplaysMessageWithTasks_WhenTaskCountIsGreaterThanZero()
    {
        // Arrange & Act
        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 5));

        // Assert
        var warningContent = cut.Find(".warning-content");
        Assert.Contains("このプロジェクトと、関連する", warningContent.TextContent);

        var taskBadge = cut.Find(".task-count-badge");
        Assert.Contains("5 個", taskBadge.TextContent);
    }

    [Fact(DisplayName = "警告アイコンが表示される")]
    public void ProjectDeleteModal_DisplaysWarningIcon()
    {
        // Arrange & Act
        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 0));

        // Assert
        var warningIcon = cut.Find(".warning-icon");
        Assert.Contains("⚠", warningIcon.TextContent);
    }

    [Fact(DisplayName = "危険通知セクションが表示される")]
    public void ProjectDeleteModal_DisplaysDangerNotice()
    {
        // Arrange & Act
        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 0));

        // Assert
        var dangerNotice = cut.Find(".danger-notice");
        Assert.Contains("削除したデータは復元できません", dangerNotice.TextContent);

        var dangerIcon = cut.Find(".danger-notice-icon");
        Assert.Contains("🗑️", dangerIcon.TextContent);
    }

    [Fact(DisplayName = "キャンセルボタンクリック時にOnCancelイベントが発火する")]
    public void ProjectDeleteModal_InvokesOnCancel_WhenCancelButtonClicked()
    {
        // Arrange
        var onCancelInvoked = false;
        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 0)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => onCancelInvoked = true)));

        // Act
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("キャンセル"));
        cancelButton.Click();

        // Assert
        Assert.True(onCancelInvoked);
    }

    [Fact(DisplayName = "タスクがない場合、プロジェクト削除成功時にOnSuccessイベントが発火する")]
    public async Task ProjectDeleteModal_InvokesOnSuccess_WhenProjectDeletedSuccessfully_WithoutTasks()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<DeleteProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var onSuccessInvoked = false;
        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 0)
            .Add(p => p.DeletedBy, "test-user")
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => onSuccessInvoked = true)));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        Assert.True(onSuccessInvoked);
        await _mediatorMock.Received(1).Send(
            Arg.Is<DeleteProjectCommand>(cmd =>
                cmd.ProjectId == _testProjectId &&
                cmd.DeletedBy == "test-user"),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "タスクがある場合、カスケード削除が実行される")]
    public async Task ProjectDeleteModal_PerformsCascadeDelete_WhenTasksExist()
    {
        // Arrange
        var task1Id = Guid.NewGuid();
        var task2Id = Guid.NewGuid();
        var tasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = task1Id,
                ProjectId = _testProjectId,
                Title = "Task 1",
                Description = "Description 1",
                Status = TaskStatus.Todo,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "admin"
            },
            new TaskDto
            {
                Id = task2Id,
                ProjectId = _testProjectId,
                Title = "Task 2",
                Description = "Description 2",
                Status = TaskStatus.Done,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "admin"
            }
        };

        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasks);
        _mediatorMock
            .Send(Arg.Any<DeleteTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _mediatorMock
            .Send(Arg.Any<DeleteProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var onSuccessInvoked = false;
        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 2)
            .Add(p => p.DeletedBy, "test-user")
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => onSuccessInvoked = true)));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        Assert.True(onSuccessInvoked);

        // タスク削除コマンドが2回送信されたことを確認
        await _mediatorMock.Received(1).Send(
            Arg.Is<DeleteTaskCommand>(cmd => cmd.TaskId == task1Id),
            Arg.Any<CancellationToken>());
        await _mediatorMock.Received(1).Send(
            Arg.Is<DeleteTaskCommand>(cmd => cmd.TaskId == task2Id),
            Arg.Any<CancellationToken>());

        // プロジェクト削除コマンドが送信されたことを確認
        await _mediatorMock.Received(1).Send(
            Arg.Is<DeleteProjectCommand>(cmd => cmd.ProjectId == _testProjectId),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "削除中は削除ボタンが無効化される")]
    public async Task ProjectDeleteModal_DisablesDeleteButton_WhileDeleting()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        _mediatorMock
            .Send(Arg.Any<DeleteProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 0)
            .Add(p => p.DeletedBy, "test-user"));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert - 削除中はボタンが無効化されている
        var disabledButton = cut.Find("button[disabled].btn-danger");
        Assert.Contains("削除中", disabledButton.TextContent);

        // Cleanup
        tcs.SetResult();
    }

    [Fact(DisplayName = "削除中はキャンセルボタンが無効化される")]
    public async Task ProjectDeleteModal_DisablesCancelButton_WhileDeleting()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        _mediatorMock
            .Send(Arg.Any<DeleteProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 0)
            .Add(p => p.DeletedBy, "test-user"));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert - 削除中はキャンセルボタンも無効化されている
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("キャンセル"));
        Assert.True(cancelButton.HasAttribute("disabled"));

        // Cleanup
        tcs.SetResult();
    }

    [Fact(DisplayName = "削除失敗時にエラーメッセージが表示される")]
    public async Task ProjectDeleteModal_DisplaysErrorMessage_WhenDeleteFails()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<DeleteProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new Exception("Test error"));

        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 0)
            .Add(p => p.DeletedBy, "test-user"));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        var errorMessage = cut.Find(".alert-danger");
        Assert.Contains("Test error", errorMessage.TextContent);
    }

    [Fact(DisplayName = "タスク削除中のエラーが適切に処理される")]
    public async Task ProjectDeleteModal_HandlesTaskDeleteError_Properly()
    {
        // Arrange
        var task1Id = Guid.NewGuid();
        var tasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = task1Id,
                ProjectId = _testProjectId,
                Title = "Task 1",
                Description = "Description 1",
                Status = TaskStatus.Todo,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "admin"
            }
        };

        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tasks);
        _mediatorMock
            .Send(Arg.Any<DeleteTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new Exception("Task delete error"));

        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 1)
            .Add(p => p.DeletedBy, "test-user"));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        var errorMessage = cut.Find(".alert-danger");
        Assert.Contains("Task delete error", errorMessage.TextContent);

        // プロジェクト削除コマンドが送信されていないことを確認
        await _mediatorMock.DidNotReceive().Send(
            Arg.Any<DeleteProjectCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "削除成功後にモーダルが閉じられる")]
    public async Task ProjectDeleteModal_ClosesModal_AfterSuccessfulDelete()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<DeleteProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var isVisibleChanged = false;
        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 0)
            .Add(p => p.DeletedBy, "test-user")
            .Add(p => p.IsVisibleChanged, EventCallback.Factory.Create<bool>(this, (visible) => isVisibleChanged = !visible))
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => { })));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        Assert.True(isVisibleChanged);
    }

    [Fact(DisplayName = "DeletedByパラメータが正しく使用される")]
    public async Task ProjectDeleteModal_UsesDeletedByParameter_Correctly()
    {
        // Arrange
        var customDeletedBy = "custom-user";
        _mediatorMock
            .Send(Arg.Any<DeleteProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 0)
            .Add(p => p.DeletedBy, customDeletedBy)
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => { })));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        await _mediatorMock.Received(1).Send(
            Arg.Is<DeleteProjectCommand>(cmd => cmd.DeletedBy == customDeletedBy),
            Arg.Any<CancellationToken>());
    }
}
