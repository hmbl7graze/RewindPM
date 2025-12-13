using Bunit;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Web.Components.Pages.Projects;

namespace RewindPM.Web.Test.Components.Pages.Projects;

public class EditTests : Bunit.TestContext
{
    private readonly IMediator _mediatorMock;
    private readonly Guid _testProjectId = Guid.NewGuid();

    public EditTests()
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
            CreatedAt = DateTime.Now.AddDays(-7),
            UpdatedAt = null,
            CreatedBy = "test-user",
            UpdatedBy = null
        };
    }

    [Fact(DisplayName = "初期表示時に読み込み中メッセージが表示される")]
    public async Task Edit_DisplaysLoadingMessage_Initially()
    {
        // Arrange
        var tcs = new TaskCompletionSource<ProjectDto?>();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert
        Assert.Contains("読み込み中", cut.Markup);

        // Cleanup
        tcs.SetResult(CreateTestProject());
    }

    [Fact(DisplayName = "プロジェクトが見つからない場合、エラーメッセージが表示される")]
    public void Edit_DisplaysErrorMessage_WhenProjectNotFound()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((ProjectDto?)null);

        // Act
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.Contains("プロジェクトが見つかりません", alert.TextContent);
        
        var backLink = cut.FindAll("a").First(a => a.TextContent.Contains("プロジェクト一覧に戻る"));
        Assert.Equal("/projects", backLink.GetAttribute("href"));
    }

    [Fact(DisplayName = "プロジェクト読み込み成功時にフォームが表示される")]
    public void Edit_DisplaysForm_WhenProjectLoaded()
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
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert
        var titleInput = cut.Find("input#title");
        var descriptionInput = cut.Find("textarea#description");
        var updatedByInput = cut.Find("input#updatedBy");

        Assert.Equal(project.Title, titleInput.GetAttribute("value"));
        // InputTextAreaコンポーネントは初期値をvalue属性で保持する
        Assert.Contains(project.Description, cut.Markup);
    }

    [Fact(DisplayName = "更新ボタンをクリックするとUpdateProjectCommandが送信される")]
    public async Task Edit_SendsUpdateProjectCommand_OnSubmit()
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
            .Send(Arg.Any<UpdateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act
        var titleInput = cut.Find("input#title");
        var submitButton = cut.Find("button[type='submit']");

        await cut.InvokeAsync(() => titleInput.Change("Updated Title"));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        await _mediatorMock.Received(1).Send(
            Arg.Is<UpdateProjectCommand>(cmd =>
                cmd.ProjectId == _testProjectId &&
                cmd.Title == "Updated Title"),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "更新成功時にプロジェクト詳細ページにリダイレクトされる")]
    public async Task Edit_RedirectsToDetailPage_OnSuccessfulUpdate()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());
        _mediatorMock
            .Send(Arg.Any<UpdateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        Assert.Equal($"{navigationManager.BaseUri}projects/{_testProjectId}", navigationManager.Uri);
    }

    [Fact(DisplayName = "更新失敗時にエラーメッセージが表示される")]
    public async Task Edit_DisplaysErrorMessage_OnFailedUpdate()
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
            .Send(Arg.Any<UpdateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new Exception("Update failed"));

        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        var errorMessage = cut.Find(".alert-danger");
        Assert.Contains("Update failed", errorMessage.TextContent);
    }

    [Fact(DisplayName = "送信中は更新ボタンが無効化される")]
    public async Task Edit_DisablesSubmitButton_WhileSubmitting()
    {
        // Arrange
        var project = CreateTestProject();
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _mediatorMock
            .Send(Arg.Any<GetTasksByProjectIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaskDto>());

        var tcs = new TaskCompletionSource();
        _mediatorMock
            .Send(Arg.Any<UpdateProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert - 送信中はボタンが無効化されている
        var disabledButton = cut.Find("button[disabled]");
        Assert.Contains("更新中", disabledButton.TextContent);

        // Cleanup
        tcs.SetResult();
    }

    [Fact(DisplayName = "キャンセルボタンがプロジェクト詳細ページへのリンクである")]
    public void Edit_CancelButton_LinksToDetailPage()
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
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert
        var cancelLink = cut.FindAll("a").First(a => a.TextContent.Contains("キャンセル"));
        Assert.Equal($"/projects/{_testProjectId}", cancelLink.GetAttribute("href"));
    }

    [Fact(DisplayName = "タイトルが正しく設定される")]
    public void Edit_SetsCorrectPageTitle()
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
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert
        var pageTitle = cut.Find("h1");
        Assert.Equal("プロジェクト編集", pageTitle.TextContent);
    }

    [Fact(DisplayName = "プロジェクト読み込み失敗時にエラーメッセージが表示される")]
    public void Edit_DisplaysErrorMessage_WhenLoadFails()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns<ProjectDto?>(_ => throw new Exception("Load failed"));

        // Act
        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Assert
        // Edit.razorでは例外がスローされてもprojectがnullになり、
        // "プロジェクトが見つかりません"が表示される
        // (errorMessageは設定されるが、project == nullなので表示されない)
        Assert.Contains("プロジェクトが見つかりません", cut.Markup);
    }
    [Fact(DisplayName = "UpdatedByが空の場合も削除コマンドが正しく送信される")]
    public async Task Edit_SendsDeleteCommand_EvenIfUpdatedByIsEmpty()
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
            .Send(Arg.Any<DeleteProjectCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Edit>(parameters => parameters
            .Add(p => p.Id, _testProjectId));

        // Act
        // UpdatedByを空にする
        var updatedByInput = cut.Find("input#updatedBy");
        await cut.InvokeAsync(() => updatedByInput.Change(""));

        // 削除ボタンをクリックしてモーダルを表示
        var deleteButton = cut.Find("button.btn-outline-danger");
        await cut.InvokeAsync(() => deleteButton.Click());

        // モーダルの削除ボタンをクリック
        var confirmDeleteButton = cut.FindAll("button.btn-danger").Last(); // 最後のボタンが確認ボタン
        await cut.InvokeAsync(() => confirmDeleteButton.Click());

        // Assert
        // 現状のバグを再現するため、本来は失敗するはずだが、修正後は成功すべき。
        // ここでは「成功すること」を期待するテストを書くことで、修正前の失敗を確認する（TDD的アプローチ）
        await _mediatorMock.Received(1).Send(
            Arg.Is<DeleteProjectCommand>(cmd =>
                cmd.ProjectId == _testProjectId &&
                !string.IsNullOrEmpty(cmd.DeletedBy)), // DeletedByが空でないことを確認
            Arg.Any<CancellationToken>());
    }
}
