using Bunit;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Web.Components.Pages.Projects;

namespace RewindPM.Web.Test.Components.Pages.Projects;

public class CreateTests : Bunit.TestContext
{
    private readonly IMediator _mediatorMock;

    public CreateTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        Services.AddSingleton(_mediatorMock);
    }

    [Fact(DisplayName = "初期表示時にフォームが表示される")]
    public void Create_DisplaysForm_OnInitialLoad()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        var titleInput = cut.Find("input#title");
        var descriptionInput = cut.Find("textarea#description");
        var createdByInput = cut.Find("input#createdBy");
        var submitButton = cut.Find("button[type='submit']");

        Assert.NotNull(titleInput);
        Assert.NotNull(descriptionInput);
        Assert.NotNull(createdByInput);
        Assert.NotNull(submitButton);
        Assert.Contains("作成", submitButton.TextContent);
    }

    [Fact(DisplayName = "キャンセルボタンがプロジェクト一覧へのリンクである")]
    public void Create_CancelButton_LinksToProjectsList()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        var cancelLink = cut.FindAll("a").First(a => a.TextContent.Contains("キャンセル"));
        Assert.Equal("/projects", cancelLink.GetAttribute("href"));
    }

    [Fact(DisplayName = "プロジェクト作成成功時にプロジェクト一覧にリダイレクトされる")]
    public async Task Create_RedirectsToProjectsList_OnSuccessfulSubmit()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _mediatorMock
            .Send(Arg.Any<CreateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(projectId);

        var cut = RenderComponent<Create>();

        // Act
        var titleInput = cut.Find("input#title");
        var descriptionInput = cut.Find("textarea#description");
        var submitButton = cut.Find("button[type='submit']");

        await cut.InvokeAsync(() => titleInput.Change("New Project"));
        await cut.InvokeAsync(() => descriptionInput.Change("Project Description"));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        Assert.Equal($"{navigationManager.BaseUri}projects", navigationManager.Uri);
    }

    [Fact(DisplayName = "プロジェクト作成失敗時にエラーメッセージが表示される")]
    public async Task Create_DisplaysErrorMessage_OnFailedSubmit()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<CreateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns<Guid>(_ => throw new Exception("Test error"));

        var cut = RenderComponent<Create>();

        // Act
        var titleInput = cut.Find("input#title");
        var submitButton = cut.Find("button[type='submit']");

        await cut.InvokeAsync(() => titleInput.Change("New Project"));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        var errorMessage = cut.Find(".alert-danger");
        Assert.Contains("Test error", errorMessage.TextContent);
    }

    [Fact(DisplayName = "送信中は送信ボタンが無効化される")]
    public async Task Create_DisablesSubmitButton_WhileSubmitting()
    {
        // Arrange
        var tcs = new TaskCompletionSource<Guid>();
        _mediatorMock
            .Send(Arg.Any<CreateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderComponent<Create>();

        // Act
        var titleInput = cut.Find("input#title");
        var submitButton = cut.Find("button[type='submit']");

        await cut.InvokeAsync(() => titleInput.Change("New Project"));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert - 送信中はボタンが無効化されている
        var disabledButton = cut.Find("button[disabled]");
        Assert.Contains("作成中", disabledButton.TextContent);

        // Cleanup
        tcs.SetResult(Guid.NewGuid());
    }

    [Fact(DisplayName = "タイトルが正しく設定される")]
    public void Create_SetsCorrectPageTitle()
    {
        // Act
        var cut = RenderComponent<Create>();

        // Assert
        var pageTitle = cut.Find("h1");
        Assert.Equal("プロジェクト作成", pageTitle.TextContent);
    }

    [Fact(DisplayName = "CreateProjectCommandに正しいパラメータが渡される")]
    public async Task Create_PassesCorrectParameters_ToCreateProjectCommand()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _mediatorMock
            .Send(Arg.Any<CreateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(projectId);

        var cut = RenderComponent<Create>();

        // Act
        var titleInput = cut.Find("input#title");
        var descriptionInput = cut.Find("textarea#description");
        var submitButton = cut.Find("button[type='submit']");

        await cut.InvokeAsync(() => titleInput.Change("Test Project"));
        await cut.InvokeAsync(() => descriptionInput.Change("Test Description"));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        await _mediatorMock.Received(1).Send(
            Arg.Is<CreateProjectCommand>(cmd =>
                cmd.Title == "Test Project" &&
                cmd.Description == "Test Description" &&
                !string.IsNullOrEmpty(cmd.CreatedBy)),
            Arg.Any<CancellationToken>());
    }
}
