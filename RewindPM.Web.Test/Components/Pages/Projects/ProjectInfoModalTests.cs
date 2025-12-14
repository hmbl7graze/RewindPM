using Bunit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Web.Components.Pages.Projects;

namespace RewindPM.Web.Test.Components.Pages.Projects;

public class ProjectInfoModalTests : Bunit.TestContext
{
    private readonly IMediator _mediatorMock;
    private readonly ILogger<ProjectInfoModal> _loggerMock;
    private readonly Guid _testProjectId = Guid.NewGuid();

    public ProjectInfoModalTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        _loggerMock = Substitute.For<ILogger<ProjectInfoModal>>();
        Services.AddSingleton(_mediatorMock);
        Services.AddSingleton(_loggerMock);
    }

    [Fact(DisplayName = "モーダルが非表示の場合、表示されない")]
    public void ProjectInfoModal_NotDisplayed_WhenNotVisible()
    {
        // Arrange & Act
        var cut = RenderComponent<ProjectInfoModal>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ProjectTitle, "Test Project")
            .Add(p => p.ProjectDescription, "Test Description"));

        // Assert
        Assert.DoesNotContain("プロジェクト情報", cut.Markup);
    }

    [Fact(DisplayName = "モーダルが表示される場合、プロジェクト情報が表示される")]
    public void ProjectInfoModal_DisplaysProjectInfo_WhenVisible()
    {
        // Arrange & Act
        var cut = RenderComponent<ProjectInfoModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ProjectTitle, "Test Project")
            .Add(p => p.ProjectDescription, "Test Description"));

        // Assert
        Assert.Contains("プロジェクト情報", cut.Markup);
        Assert.Contains("Test Project", cut.Markup);
        Assert.Contains("Test Description", cut.Markup);
    }

    [Fact(DisplayName = "説明が空の場合、デフォルトメッセージが表示される")]
    public void ProjectInfoModal_DisplaysDefaultMessage_WhenDescriptionIsEmpty()
    {
        // Arrange & Act
        var cut = RenderComponent<ProjectInfoModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ProjectTitle, "Test Project")
            .Add(p => p.ProjectDescription, ""));

        // Assert
        Assert.Contains("説明がありません", cut.Markup);
    }

    [Fact(DisplayName = "編集ボタンをクリックすると編集モードに移行する")]
    public void ProjectInfoModal_EntersEditMode_WhenEditButtonClicked()
    {
        // Arrange
        var cut = RenderComponent<ProjectInfoModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ProjectTitle, "Test Project")
            .Add(p => p.ProjectDescription, "Test Description"));

        // Act
        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("編集"));
        editButton.Click();

        // Assert
        Assert.Contains("プロジェクト編集", cut.Markup);
        var titleInput = cut.Find("input#title");
        Assert.Equal("Test Project", titleInput.GetAttribute("value"));
    }

    [Fact(DisplayName = "編集モードでキャンセルボタンをクリックすると表示モードに戻る")]
    public void ProjectInfoModal_ReturnsToDisplayMode_WhenCancelButtonClicked()
    {
        // Arrange
        var cut = RenderComponent<ProjectInfoModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ProjectTitle, "Test Project")
            .Add(p => p.ProjectDescription, "Test Description"));

        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("編集"));
        editButton.Click();

        // Act
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("キャンセル"));
        cancelButton.Click();

        // Assert
        Assert.Contains("プロジェクト情報", cut.Markup);
        Assert.DoesNotContain("プロジェクト編集", cut.Markup);
    }

    [Fact(DisplayName = "フォーム送信時にUpdateProjectCommandが送信される")]
    public async Task ProjectInfoModal_SendsUpdateProjectCommand_OnFormSubmit()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<UpdateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<ProjectInfoModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ProjectTitle, "Test Project")
            .Add(p => p.ProjectDescription, "Test Description"));

        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("編集"));
        editButton.Click();

        // Act
        var titleInput = cut.Find("input#title");
        var descriptionInput = cut.Find("textarea#description");
        await cut.InvokeAsync(() => titleInput.Change("Updated Title"));
        await cut.InvokeAsync(() => descriptionInput.Change("Updated Description"));

        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        await _mediatorMock.Received(1).Send(
            Arg.Is<UpdateProjectCommand>(cmd =>
                cmd.ProjectId == _testProjectId &&
                cmd.Title == "Updated Title" &&
                cmd.Description == "Updated Description"),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "保存成功時にOnSuccessコールバックが呼ばれる")]
    public async Task ProjectInfoModal_InvokesOnSuccess_WhenSaveSucceeds()
    {
        // Arrange
        var onSuccessCalled = false;
        _mediatorMock
            .Send(Arg.Any<UpdateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<ProjectInfoModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ProjectTitle, "Test Project")
            .Add(p => p.ProjectDescription, "Test Description")
            .Add(p => p.OnSuccess, () => { onSuccessCalled = true; return Task.CompletedTask; }));

        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("編集"));
        editButton.Click();

        // Act
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        Assert.True(onSuccessCalled);
    }

    [Fact(DisplayName = "保存失敗時にエラーメッセージが表示される")]
    public async Task ProjectInfoModal_DisplaysErrorMessage_WhenSaveFails()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<UpdateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new Exception("Update failed"));

        var cut = RenderComponent<ProjectInfoModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ProjectTitle, "Test Project")
            .Add(p => p.ProjectDescription, "Test Description"));

        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("編集"));
        editButton.Click();

        // Act
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        var errorAlert = cut.Find(".alert-danger");
        Assert.Contains("プロジェクトの更新に失敗しました", errorAlert.TextContent);
        // セキュリティのため、詳細なエラーメッセージは表示されないことを確認
        Assert.DoesNotContain("Update failed", errorAlert.TextContent);
    }

    [Fact(DisplayName = "保存中はボタンが無効化される")]
    public async Task ProjectInfoModal_DisablesButtons_WhileSaving()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        _mediatorMock
            .Send(Arg.Any<UpdateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderComponent<ProjectInfoModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ProjectTitle, "Test Project")
            .Add(p => p.ProjectDescription, "Test Description"));

        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("編集"));
        editButton.Click();

        // Act
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        var disabledButtons = cut.FindAll("button[disabled]");
        Assert.NotEmpty(disabledButtons);
        Assert.Contains(disabledButtons, b => b.TextContent.Contains("保存中"));

        // Cleanup
        tcs.SetResult();
    }

    [Fact(DisplayName = "バリデーションエラーがある場合、送信できない")]
    public async Task ProjectInfoModal_DoesNotSubmit_WhenValidationFails()
    {
        // Arrange
        var cut = RenderComponent<ProjectInfoModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ProjectId, _testProjectId)
            .Add(p => p.ProjectTitle, "Test Project")
            .Add(p => p.ProjectDescription, "Test Description"));

        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("編集"));
        editButton.Click();

        // Act - タイトルを空にする
        var titleInput = cut.Find("input#title");
        await cut.InvokeAsync(() => titleInput.Change(""));

        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert - コマンドが送信されないことを確認
        await _mediatorMock.DidNotReceive().Send(
            Arg.Any<UpdateProjectCommand>(),
            Arg.Any<CancellationToken>());

        // バリデーションメッセージが表示されることを確認
        Assert.Contains("プロジェクト名は必須です", cut.Markup);
    }
}
