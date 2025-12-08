using Bunit;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Write.Commands.Tasks;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;
using RewindPM.Web.Components.Pages.Tasks;

namespace RewindPM.Web.Test.Components.Pages.Tasks;

public class TaskFormModalTests : Bunit.TestContext
{
    private readonly IMediator _mediatorMock;
    private readonly Guid _testProjectId = Guid.NewGuid();
    private readonly Guid _testTaskId = Guid.NewGuid();

    public TaskFormModalTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        Services.AddSingleton(_mediatorMock);
    }

    private TaskDto CreateTestTask()
    {
        return new TaskDto
        {
            Id = _testTaskId,
            ProjectId = _testProjectId,
            Title = "Test Task",
            Description = "Test Description",
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
        };
    }

    [Fact(DisplayName = "新規作成モードで正しいタイトルが表示される")]
    public void TaskFormModal_DisplaysCreateTitle_InCreateMode()
    {
        // Arrange & Act
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ExistingTask, null));

        // Assert
        var title = cut.Find(".modal-title");
        Assert.Contains("タスク作成", title.TextContent);
    }

    [Fact(DisplayName = "編集モードで正しいタイトルが表示される")]
    public void TaskFormModal_DisplaysEditTitle_InEditMode()
    {
        // Arrange
        var existingTask = CreateTestTask();

        // Act
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ExistingTask, existingTask));

        // Assert
        var title = cut.Find(".modal-title");
        Assert.Contains("タスク編集", title.TextContent);
    }

    [Fact(DisplayName = "新規作成モードでフォームフィールドが表示される")]
    public void TaskFormModal_RendersFormFields_InCreateMode()
    {
        // Arrange & Act
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId));

        // Assert
        var titleInput = cut.Find("input#taskTitle");
        var descriptionInput = cut.Find("textarea#taskDescription");
        var statusSelect = cut.Find("select#taskStatus");
        var scheduledStartDateInput = cut.Find("input#scheduledStartDate");
        var scheduledEndDateInput = cut.Find("input#scheduledEndDate");
        var estimatedHoursInput = cut.Find("input#estimatedHours");

        Assert.NotNull(titleInput);
        Assert.NotNull(descriptionInput);
        Assert.NotNull(statusSelect);
        Assert.NotNull(scheduledStartDateInput);
        Assert.NotNull(scheduledEndDateInput);
        Assert.NotNull(estimatedHoursInput);
    }

    [Fact(DisplayName = "編集モードで実績期間フィールドが表示される")]
    public void TaskFormModal_DisplaysActualPeriodFields_InEditMode()
    {
        // Arrange
        var existingTask = CreateTestTask();

        // Act
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ExistingTask, existingTask));

        // Assert
        var actualStartDateInput = cut.Find("input#actualStartDate");
        var actualEndDateInput = cut.Find("input#actualEndDate");
        var actualHoursInput = cut.Find("input#actualHours");

        Assert.NotNull(actualStartDateInput);
        Assert.NotNull(actualEndDateInput);
        Assert.NotNull(actualHoursInput);
    }

    [Fact(DisplayName = "編集モードで削除ボタンが表示される")]
    public void TaskFormModal_DisplaysDeleteButton_InEditMode()
    {
        // Arrange
        var existingTask = CreateTestTask();

        // Act
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ExistingTask, existingTask));

        // Assert
        var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("削除"));
        Assert.NotNull(deleteButton);
    }

    [Fact(DisplayName = "新規作成モードで削除ボタンが表示されない")]
    public void TaskFormModal_DoesNotDisplayDeleteButton_InCreateMode()
    {
        // Arrange & Act
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId));

        // Assert
        var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("削除"));
        Assert.Null(deleteButton);
    }

    [Fact(DisplayName = "キャンセルボタンクリック時にOnCancelイベントが発火する")]
    public void TaskFormModal_InvokesOnCancel_WhenCancelButtonClicked()
    {
        // Arrange
        var onCancelInvoked = false;
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => onCancelInvoked = true)));

        // Act
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("キャンセル"));
        cancelButton.Click();

        // Assert
        Assert.True(onCancelInvoked);
    }

    [Fact(DisplayName = "タスク作成成功時にOnSuccessイベントが発火する")]
    public async Task TaskFormModal_InvokesOnSuccess_WhenTaskCreatedSuccessfully()
    {
        // Arrange
        var expectedTaskId = Guid.NewGuid();
        _mediatorMock
            .Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedTaskId);

        var onSuccessInvoked = false;
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => onSuccessInvoked = true)));

        // Act
        var titleInput = cut.Find("input#taskTitle");
        var descriptionInput = cut.Find("textarea#taskDescription");
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));

        await cut.InvokeAsync(() => titleInput.Change("New Task"));
        await cut.InvokeAsync(() => descriptionInput.Change("New Task Description"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        Assert.True(onSuccessInvoked);
        await _mediatorMock.Received(1).Send(
            Arg.Is<CreateTaskCommand>(cmd =>
                cmd.Title == "New Task" &&
                cmd.Description == "New Task Description" &&
                cmd.ProjectId == _testProjectId),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "タスク更新成功時にOnSuccessイベントが発火する")]
    public async Task TaskFormModal_InvokesOnSuccess_WhenTaskUpdatedSuccessfully()
    {
        // Arrange
        var existingTask = CreateTestTask();
        _mediatorMock
            .Send(Arg.Any<UpdateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var onSuccessInvoked = false;
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ExistingTask, existingTask)
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => onSuccessInvoked = true)));

        // Act
        var titleInput = cut.Find("input#taskTitle");
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));

        await cut.InvokeAsync(() => titleInput.Change("Updated Task"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        Assert.True(onSuccessInvoked);
        await _mediatorMock.Received(1).Send(
            Arg.Is<UpdateTaskCommand>(cmd =>
                cmd.TaskId == _testTaskId &&
                cmd.Title == "Updated Task"),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "ステータス変更時にChangeTaskStatusCommandが送信される")]
    public async Task TaskFormModal_SendsChangeStatusCommand_WhenStatusChanged()
    {
        // Arrange
        var existingTask = CreateTestTask();
        _mediatorMock
            .Send(Arg.Any<ChangeTaskStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ExistingTask, existingTask)
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => { })));

        // Act
        var statusSelect = cut.Find("select#taskStatus");
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));

        await cut.InvokeAsync(() => statusSelect.Change(((int)TaskStatus.InProgress).ToString()));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        await _mediatorMock.Received(1).Send(
            Arg.Is<ChangeTaskStatusCommand>(cmd =>
                cmd.TaskId == _testTaskId &&
                cmd.NewStatus == TaskStatus.InProgress),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "タスク名が空の場合、バリデーションエラーが表示される")]
    public async Task TaskFormModal_DisplaysValidationError_WhenTitleIsEmpty()
    {
        // Arrange
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId));

        // Act
        var titleInput = cut.Find("input#taskTitle");
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));

        await cut.InvokeAsync(() => titleInput.Change(""));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        var error = cut.Find(".invalid-feedback");
        Assert.Contains("タスク名は必須です", error.TextContent);

        // コマンドが送信されていないことを確認
        await _mediatorMock.DidNotReceive().Send(
            Arg.Any<CreateTaskCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "予定終了日が開始日より前の場合、バリデーションエラーが表示される")]
    public async Task TaskFormModal_DisplaysValidationError_WhenScheduledEndDateBeforeStartDate()
    {
        // Arrange
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId));

        // Act
        var titleInput = cut.Find("input#taskTitle");
        var startDateInput = cut.Find("input#scheduledStartDate");
        var endDateInput = cut.Find("input#scheduledEndDate");
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));

        await cut.InvokeAsync(() => titleInput.Change("Test Task"));
        await cut.InvokeAsync(() => startDateInput.Change(DateTime.Today.AddDays(5).ToString("yyyy-MM-dd")));
        await cut.InvokeAsync(() => endDateInput.Change(DateTime.Today.ToString("yyyy-MM-dd")));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        var errors = cut.FindAll(".text-danger");
        Assert.Contains(errors, e => e.TextContent.Contains("終了日は開始日より後の日付を指定してください"));

        // コマンドが送信されていないことを確認
        await _mediatorMock.DidNotReceive().Send(
            Arg.Any<CreateTaskCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "編集モードで既存タスクのデータが正しく読み込まれる")]
    public async Task TaskFormModal_LoadsExistingTaskData_InEditMode()
    {
        // Arrange
        var existingTask = CreateTestTask();
        _mediatorMock
            .Send(Arg.Any<UpdateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ExistingTask, existingTask)
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => { })));

        // Act - タイトルを変更せずに保存（既存の値が保持されていることを確認）
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert - UpdateTaskCommandが送信されないこと（変更がないため）
        await _mediatorMock.DidNotReceive().Send(
            Arg.Any<UpdateTaskCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "タスク作成時にデフォルト値が使用される")]
    public async Task TaskFormModal_UsesDefaultValues_WhenCreatingTaskWithoutSchedule()
    {
        // Arrange
        var expectedTaskId = Guid.NewGuid();
        _mediatorMock
            .Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedTaskId);

        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => { })));

        // Act
        var titleInput = cut.Find("input#taskTitle");
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));

        await cut.InvokeAsync(() => titleInput.Change("New Task"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert - デフォルト値が使用されていることを確認
        await _mediatorMock.Received(1).Send(
            Arg.Is<CreateTaskCommand>(cmd =>
                cmd.ScheduledStartDate == DateTime.Today &&
                cmd.ScheduledEndDate == DateTime.Today.AddDays(1) &&
                cmd.EstimatedHours == 8),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "保存中は保存ボタンが無効化される")]
    public async Task TaskFormModal_DisablesSaveButton_WhileSaving()
    {
        // Arrange
        var tcs = new TaskCompletionSource<Guid>();
        _mediatorMock
            .Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId));

        // Act
        var titleInput = cut.Find("input#taskTitle");
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));

        await cut.InvokeAsync(() => titleInput.Change("Test Task"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert - 保存中はボタンが無効化されている
        var disabledButton = cut.Find("button[disabled]");
        Assert.Contains("保存中", disabledButton.TextContent);

        // Cleanup
        tcs.SetResult(Guid.NewGuid());
    }

    [Fact(DisplayName = "保存失敗時にエラーメッセージが表示される")]
    public async Task TaskFormModal_DisplaysErrorMessage_WhenSaveFails()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns<Guid>(_ => throw new Exception("Test error"));

        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId));

        // Act
        var titleInput = cut.Find("input#taskTitle");
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));

        await cut.InvokeAsync(() => titleInput.Change("Test Task"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        var errorMessage = cut.Find(".alert-danger");
        Assert.Contains("Test error", errorMessage.TextContent);
    }
}
