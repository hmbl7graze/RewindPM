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
        var titleInput = cut.Find(".task-title-input");
        var descriptionInput = cut.Find(".description-textarea");
        var statusSelect = cut.Find(".form-select");
        var scheduledDateInputs = cut.FindAll("input[type='date']");
        Assert.True(scheduledDateInputs.Count >= 2);
        var estimatedHoursInput = cut.Find("input[type='number']");

        Assert.NotNull(titleInput);
        Assert.NotNull(descriptionInput);
        Assert.NotNull(statusSelect);
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
        var actualDateInputs = cut.FindAll("input[type='date']");
        Assert.True(actualDateInputs.Count >= 4); // 予定2つ + 実績2つ
        var actualHoursInputs = cut.FindAll("input[type='number']");
        Assert.True(actualHoursInputs.Count >= 2); // 予定工数 + 実績工数
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
        var titleInput = cut.Find(".task-title-input");
        var descriptionInput = cut.Find(".description-textarea");
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
        var titleInput = cut.Find(".task-title-input");
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
        var statusSelect = cut.Find(".form-select");
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
        var titleInput = cut.Find(".task-title-input");
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
        var titleInput = cut.Find(".task-title-input");
        await cut.InvokeAsync(() => titleInput.Change("Test Task"));
        
        // 再レンダリング後に要素を再取得
        var dateInputs = cut.FindAll("input[type='date']");
        var startDateInput = dateInputs[0];
        await cut.InvokeAsync(() => startDateInput.Change(DateTime.Today.AddDays(5).ToString("yyyy-MM-dd")));
        
        // 再度取得
        dateInputs = cut.FindAll("input[type='date']");
        var endDateInput = dateInputs[1];
        await cut.InvokeAsync(() => endDateInput.Change(DateTime.Today.ToString("yyyy-MM-dd")));
        
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        var errors = cut.FindAll(".text-danger");
        Assert.Contains(errors, e => e.TextContent.Contains("予定終了日は予定開始日より後の日付を指定してください"));

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

    [Fact(DisplayName = "タスク作成時に予定・実績がnullで送信される")]
    public async Task TaskFormModal_SendsNullValues_WhenCreatingTaskWithoutScheduleAndActual()
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
        var titleInput = cut.Find(".task-title-input");
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));

        await cut.InvokeAsync(() => titleInput.Change("New Task"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert - null値が送信されていることを確認
        await _mediatorMock.Received(1).Send(
            Arg.Is<CreateTaskCommand>(cmd =>
                cmd.ScheduledStartDate == null &&
                cmd.ScheduledEndDate == null &&
                cmd.EstimatedHours == null &&
                cmd.ActualStartDate == null &&
                cmd.ActualEndDate == null &&
                cmd.ActualHours == null),
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
        var titleInput = cut.Find(".task-title-input");
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
        var titleInput = cut.Find(".task-title-input");
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));

        await cut.InvokeAsync(() => titleInput.Change("Test Task"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        var errorMessage = cut.Find(".alert-danger");
        Assert.Contains("保存に失敗しました", errorMessage.TextContent);
    }

    [Fact(DisplayName = "新規作成モードで実績期間フィールドが表示される")]
    public void TaskFormModal_DisplaysActualPeriodFields_InCreateMode()
    {
        // Arrange & Act
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId));

        // Assert - 予定2つ + 実績2つ = 4つのdate入力フィールド
        var dateInputs = cut.FindAll("input[type='date']");
        Assert.True(dateInputs.Count >= 4);
        
        // 予定工数 + 実績工数 = 2つのnumber入力フィールド
        var numberInputs = cut.FindAll("input[type='number']");
        Assert.True(numberInputs.Count >= 2);
    }

    [Fact(DisplayName = "新規作成時に実績データを入力してタスクを作成できる")]
    public async Task TaskFormModal_CanCreateTaskWithActualData_InCreateMode()
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
        var scheduledStartDate = DateTime.Today;
        var scheduledEndDate = DateTime.Today.AddDays(7);
        var actualStartDate = DateTime.Today;
        var actualEndDate = DateTime.Today.AddDays(5);

        await cut.InvokeAsync(() => cut.Find(".task-title-input").Change("Task with Actuals"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='date']")[0].Change(scheduledStartDate.ToString("yyyy-MM-dd")));
        await cut.InvokeAsync(() => cut.FindAll("input[type='date']")[1].Change(scheduledEndDate.ToString("yyyy-MM-dd")));
        await cut.InvokeAsync(() => cut.FindAll("input[type='number']")[0].Change("40"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='date']")[2].Change(actualStartDate.ToString("yyyy-MM-dd")));
        await cut.InvokeAsync(() => cut.FindAll("input[type='date']")[3].Change(actualEndDate.ToString("yyyy-MM-dd")));
        await cut.InvokeAsync(() => cut.FindAll("input[type='number']")[1].Change("30"));
        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("保存")).Click());

        // Assert - 実績データが含まれたコマンドが送信される
        await _mediatorMock.Received(1).Send(
            Arg.Is<CreateTaskCommand>(cmd =>
                cmd.Title == "Task with Actuals" &&
                cmd.ActualStartDate == actualStartDate &&
                cmd.ActualEndDate == actualEndDate &&
                cmd.ActualHours == 30),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "編集モードで開始日のみ入力した場合、バリデーションエラーが表示される")]
    public async Task TaskFormModal_DisplaysValidationError_WhenOnlyStartDateEntered()
    {
        // Arrange
        var existingTask = new TaskDto
        {
            Id = _testTaskId,
            ProjectId = _testProjectId,
            Title = "Test Task",
            Description = "Test Description",
            Status = TaskStatus.Todo,
            ScheduledStartDate = null,
            ScheduledEndDate = null,
            EstimatedHours = null,
            ActualStartDate = null,
            ActualEndDate = null,
            ActualHours = null,
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            CreatedBy = "admin"
        };

        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ExistingTask, existingTask));

        // Act
        var dateInputs = cut.FindAll("input[type='date']");
        var startDateInput = dateInputs[0];
        await cut.InvokeAsync(() => startDateInput.Change(DateTime.Today.ToString("yyyy-MM-dd")));

        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        var errors = cut.FindAll(".text-danger");
        Assert.Contains(errors, e => e.TextContent.Contains("予定期間を変更する場合は、開始日と終了日の両方を入力してください"));

        // コマンドが送信されていないことを確認
        await _mediatorMock.DidNotReceive().Send(
            Arg.Any<ChangeTaskScheduleCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "編集モードで終了日のみ入力した場合、バリデーションエラーが表示される")]
    public async Task TaskFormModal_DisplaysValidationError_WhenOnlyEndDateEntered()
    {
        // Arrange
        var existingTask = new TaskDto
        {
            Id = _testTaskId,
            ProjectId = _testProjectId,
            Title = "Test Task",
            Description = "Test Description",
            Status = TaskStatus.Todo,
            ScheduledStartDate = null,
            ScheduledEndDate = null,
            EstimatedHours = null,
            ActualStartDate = null,
            ActualEndDate = null,
            ActualHours = null,
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            CreatedBy = "admin"
        };

        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ExistingTask, existingTask));

        // Act
        var dateInputs = cut.FindAll("input[type='date']");
        var endDateInput = dateInputs[1];
        await cut.InvokeAsync(() => endDateInput.Change(DateTime.Today.AddDays(5).ToString("yyyy-MM-dd")));

        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        var errors = cut.FindAll(".text-danger");
        Assert.Contains(errors, e => e.TextContent.Contains("予定期間を変更する場合は、開始日と終了日の両方を入力してください"));

        // コマンドが送信されていないことを確認
        await _mediatorMock.DidNotReceive().Send(
            Arg.Any<ChangeTaskScheduleCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "編集モードで工数なしで予定期間を変更できる")]
    public async Task TaskFormModal_CanUpdateScheduleWithoutEstimatedHours()
    {
        // Arrange
        var existingTask = CreateTestTask();
        _mediatorMock
            .Send(Arg.Any<ChangeTaskScheduleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ExistingTask, existingTask)
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => { })));

        // Act
        // 開始日を変更
        var dateInputs = cut.FindAll("input[type='date']");
        var startDateInput = dateInputs[0];
        await cut.InvokeAsync(() => startDateInput.Change(DateTime.Today.AddDays(1).ToString("yyyy-MM-dd")));

        // 再レンダリング後に終了日を変更
        dateInputs = cut.FindAll("input[type='date']");
        var endDateInput = dateInputs[1];
        await cut.InvokeAsync(() => endDateInput.Change(DateTime.Today.AddDays(8).ToString("yyyy-MM-dd")));

        // 再レンダリング後に工数をクリア
        var estimatedHoursInputs = cut.FindAll("input[type='number']");
        var estimatedHoursInput = estimatedHoursInputs[0];
        await cut.InvokeAsync(() => estimatedHoursInput.Change(""));

        // 再レンダリング後に保存ボタンをクリック
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert - ChangeTaskScheduleCommandがEstimatedHours=nullで送信される
        await _mediatorMock.Received(1).Send(
            Arg.Is<ChangeTaskScheduleCommand>(cmd =>
                cmd.TaskId == _testTaskId &&
                cmd.EstimatedHours == null),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "工数が0の場合、バリデーションエラーが表示される")]
    public async Task TaskFormModal_DisplaysValidationError_WhenEstimatedHoursIsZero()
    {
        // Arrange
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId));

        // Act
        var titleInput = cut.Find(".task-title-input");
        await cut.InvokeAsync(() => titleInput.Change("Test Task"));

        var estimatedHoursInputs = cut.FindAll("input[type='number']");
        var estimatedHoursInput = estimatedHoursInputs[0];
        await cut.InvokeAsync(() => estimatedHoursInput.Change("0"));

        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        var errors = cut.FindAll(".invalid-feedback");
        Assert.Contains(errors, e => e.TextContent.Contains("見積工数は正の数でなければなりません"));

        // コマンドが送信されていないことを確認
        await _mediatorMock.DidNotReceive().Send(
            Arg.Any<CreateTaskCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "工数が負の値の場合、バリデーションエラーが表示される")]
    public async Task TaskFormModal_DisplaysValidationError_WhenEstimatedHoursIsNegative()
    {
        // Arrange
        var cut = RenderComponent<TaskFormModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId));

        // Act
        var titleInput = cut.Find(".task-title-input");
        await cut.InvokeAsync(() => titleInput.Change("Test Task"));

        var estimatedHoursInputs = cut.FindAll("input[type='number']");
        var estimatedHoursInput = estimatedHoursInputs[0];
        await cut.InvokeAsync(() => estimatedHoursInput.Change("-5"));

        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("保存"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        var errors = cut.FindAll(".invalid-feedback");
        Assert.Contains(errors, e => e.TextContent.Contains("見積工数は正の数でなければなりません"));

        // コマンドが送信されていないことを確認
        await _mediatorMock.DidNotReceive().Send(
            Arg.Any<CreateTaskCommand>(),
            Arg.Any<CancellationToken>());
    }
}
