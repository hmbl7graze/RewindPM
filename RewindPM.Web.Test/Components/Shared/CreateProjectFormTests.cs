using Bunit;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Web.Components.Shared;

namespace RewindPM.Web.Test.Components.Shared;

public class CreateProjectFormTests : Bunit.TestContext
{
    private readonly IMediator _mediatorMock;

    public CreateProjectFormTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        Services.AddSingleton(_mediatorMock);
    }

    [Fact(DisplayName = "プロジェクト作成フォームにフォームフィールドが表示される")]
    public void CreateProjectForm_RendersFormFields()
    {
        // Arrange & Act
        var cut = RenderComponent<CreateProjectForm>();

        // Assert
        var titleInput = cut.Find("input#title");
        var descriptionInput = cut.Find("textarea#description");

        Assert.NotNull(titleInput);
        Assert.NotNull(descriptionInput);
    }

    [Fact(DisplayName = "プロジェクト作成フォームに作成とキャンセルボタンが表示される")]
    public void CreateProjectForm_DisplaysSubmitAndCancelButtons()
    {
        // Arrange & Act
        var cut = RenderComponent<CreateProjectForm>();

        // Assert
        var buttons = cut.FindAll("button");
        Assert.Contains(buttons, b => b.TextContent.Contains("作成"));
        Assert.Contains(buttons, b => b.TextContent.Contains("キャンセル"));
    }

    [Fact(DisplayName = "キャンセルボタンクリック時にOnCancelイベントが発火する")]
    public void CreateProjectForm_InvokesOnCancel_WhenCancelButtonClicked()
    {
        // Arrange
        var onCancelInvoked = false;
        var cut = RenderComponent<CreateProjectForm>(parameters => parameters
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => onCancelInvoked = true)));

        // Act
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("キャンセル"));
        cancelButton.Click();

        // Assert
        Assert.True(onCancelInvoked);
    }

    [Fact(DisplayName = "フォーム送信成功時にOnSuccessイベントが発火する")]
    public async Task CreateProjectForm_InvokesOnSuccess_WhenFormSubmittedSuccessfully()
    {
        // Arrange
        var expectedProjectId = Guid.NewGuid();
        _mediatorMock
            .Send(Arg.Any<CreateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedProjectId);

        var onSuccessInvoked = false;
        var cut = RenderComponent<CreateProjectForm>(parameters => parameters
            .Add(p => p.OnSuccess, EventCallback.Factory.Create(this, () => onSuccessInvoked = true)));

        // Act
        var form = cut.Find("form");
        var titleInput = cut.Find("input#title");
        var descriptionInput = cut.Find("textarea#description");

        // 入力値を設定
        await cut.InvokeAsync(() => titleInput.Change("Test Project"));
        await cut.InvokeAsync(() => descriptionInput.Change("Test Description"));

        // フォーム送信
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        Assert.True(onSuccessInvoked);
        await _mediatorMock.Received(1).Send(
            Arg.Is<CreateProjectCommand>(cmd =>
                cmd.Title == "Test Project" &&
                cmd.Description == "Test Description"),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "フォーム送信失敗時にエラーメッセージが表示される")]
    public async Task CreateProjectForm_DisplaysErrorMessage_WhenSubmissionFails()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<CreateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns<Guid>(_ => throw new Exception("Test error"));

        var cut = RenderComponent<CreateProjectForm>();

        // Act
        var form = cut.Find("form");
        var titleInput = cut.Find("input#title");

        await cut.InvokeAsync(() => titleInput.Change("Test Project"));
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        var errorMessage = cut.Find(".alert-danger");
        Assert.Contains("Test error", errorMessage.TextContent);
    }

    [Fact(DisplayName = "最大文字数のヒントが表示される")]
    public void CreateProjectForm_DisplaysMaxLengthHints()
    {
        // Arrange & Act
        var cut = RenderComponent<CreateProjectForm>();

        // Assert
        var hints = cut.FindAll(".form-text");
        Assert.Contains(hints, h => h.TextContent.Contains("200文字"));
        Assert.Contains(hints, h => h.TextContent.Contains("5000文字"));
    }
}
