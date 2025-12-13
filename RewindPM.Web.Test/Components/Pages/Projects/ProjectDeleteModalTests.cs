using Bunit;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Web.Components.Pages.Projects;

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
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => onSuccessInvoked = true)));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        Assert.True(onSuccessInvoked);
        await _mediatorMock.Received(1).Send(
            Arg.Is<DeleteProjectCommand>(cmd =>
                cmd.ProjectId == _testProjectId &&
                cmd.DeletedBy == "system"), // サーバー側で設定される
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "タスクがある場合もプロジェクト削除コマンドが送信される")]
    public async Task ProjectDeleteModal_SendsDeleteCommand_WhenTasksExist()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<DeleteProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var onSuccessInvoked = false;
        var cut = RenderComponent<ProjectDeleteModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.TaskCount, 2)
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => onSuccessInvoked = true)));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        Assert.True(onSuccessInvoked);

        // プロジェクト削除コマンドが送信されたことを確認（カスケード削除はCommandHandlerで処理）
        await _mediatorMock.Received(1).Send(
            Arg.Is<DeleteProjectCommand>(cmd => cmd.ProjectId == _testProjectId && cmd.DeletedBy == "system"),
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
            .Add(p => p.TaskCount, 0));

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
            .Add(p => p.TaskCount, 0));

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
            .Add(p => p.TaskCount, 0));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        var errorMessage = cut.Find(".alert-danger");
        Assert.Contains("Test error", errorMessage.TextContent);
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
            .Add(p => p.IsVisibleChanged, EventCallback.Factory.Create<bool>(this, (visible) => isVisibleChanged = !visible))
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => { })));

        // Act
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("削除を実行"));
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        Assert.True(isVisibleChanged);
    }
}
