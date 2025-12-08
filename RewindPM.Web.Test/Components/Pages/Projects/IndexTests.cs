using Bunit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Projects;
using ProjectsIndex = RewindPM.Web.Components.Pages.Projects.Index;

namespace RewindPM.Web.Test.Components.Pages.Projects;

public class IndexTests : Bunit.TestContext
{
    private readonly IMediator _mediatorMock;

    public IndexTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        Services.AddSingleton(_mediatorMock);
    }

    [Fact(DisplayName = "初期表示時に読み込み中メッセージが表示される")]
    public async Task Index_DisplaysLoadingMessage_Initially()
    {
        // Arrange
        var tcs = new TaskCompletionSource<List<ProjectDto>>();
        _mediatorMock
            .Send(Arg.Any<GetAllProjectsQuery>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<ProjectsIndex>();

        // Assert
        var loadingState = cut.Find(".loading-state");
        Assert.Contains("読み込み中", loadingState.TextContent);

        // Cleanup
        tcs.SetResult(new List<ProjectDto>());
    }

    [Fact(DisplayName = "プロジェクトが存在しない場合、空のメッセージが表示される")]
    public void Index_DisplaysEmptyMessage_WhenNoProjects()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<GetAllProjectsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProjectDto>());

        // Act
        var cut = RenderComponent<ProjectsIndex>();

        // Assert
        var emptyState = cut.Find(".empty-state");
        Assert.Contains("プロジェクトがありません", emptyState.TextContent);
    }

    [Fact(DisplayName = "プロジェクトが存在する場合、プロジェクトカードが表示される")]
    public void Index_DisplaysProjectCards_WhenProjectsExist()
    {
        // Arrange
        var projects = new List<ProjectDto>
        {
            new ProjectDto
            {
                Id = Guid.NewGuid(),
                Title = "Project 1",
                Description = "Description 1",
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "user1"
            },
            new ProjectDto
            {
                Id = Guid.NewGuid(),
                Title = "Project 2",
                Description = "Description 2",
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "user2"
            }
        };

        _mediatorMock
            .Send(Arg.Any<GetAllProjectsQuery>(), Arg.Any<CancellationToken>())
            .Returns(projects);

        // Act
        var cut = RenderComponent<ProjectsIndex>();

        // Assert
        var projectCards = cut.FindAll(".project-card");
        Assert.Equal(2, projectCards.Count);
    }

    [Fact(DisplayName = "ヘッダーにタイトルと新規プロジェクトボタンが表示される")]
    public void Index_DisplaysHeader_WithTitleAndButton()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<GetAllProjectsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProjectDto>());

        // Act
        var cut = RenderComponent<ProjectsIndex>();

        // Assert
        var title = cut.Find(".projects-title");
        Assert.Equal("RewindPM", title.TextContent);

        var newButton = cut.Find("button");
        Assert.Contains("New Project", newButton.TextContent);
    }

    [Fact(DisplayName = "新規プロジェクトボタンクリック時にモーダルが開く")]
    public void Index_OpensModal_WhenNewProjectButtonClicked()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<GetAllProjectsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProjectDto>());

        var cut = RenderComponent<ProjectsIndex>();

        // Act
        var newButton = cut.Find("button");
        newButton.Click();

        // Assert - モーダルが表示されているか確認
        var modal = cut.Find(".modal-overlay");
        Assert.NotNull(modal);
    }

    [Fact(DisplayName = "プロジェクト作成成功時にプロジェクト一覧が再読み込みされる")]
    public async Task Index_ReloadsProjects_WhenProjectCreatedSuccessfully()
    {
        // Arrange
        var initialProjects = new List<ProjectDto>();
        var updatedProjects = new List<ProjectDto>
        {
            new ProjectDto
            {
                Id = Guid.NewGuid(),
                Title = "New Project",
                Description = "New Description",
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "user"
            }
        };

        var callCount = 0;
        _mediatorMock
            .Send(Arg.Any<GetAllProjectsQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1 ? initialProjects : updatedProjects;
            });

        var cut = RenderComponent<ProjectsIndex>();

        // Act - プロジェクト作成成功をシミュレート
        await cut.InvokeAsync(async () =>
        {
            var instance = cut.Instance;
            await (instance.GetType()
                .GetMethod("HandleCreateSuccess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(instance, null) as Task ?? Task.CompletedTask);
        });

        // Assert
        await _mediatorMock.Received(2).Send(
            Arg.Any<GetAllProjectsQuery>(),
            Arg.Any<CancellationToken>()); // 初期読み込み + 再読み込み
    }

    [Fact(DisplayName = "プロジェクト読み込み失敗時にエラーを処理する")]
    public void Index_HandlesError_WhenLoadingProjectsFails()
    {
        // Arrange
        _mediatorMock
            .Send(Arg.Any<GetAllProjectsQuery>(), Arg.Any<CancellationToken>())
            .Returns<List<ProjectDto>>(_ => throw new Exception("Test error"));

        // Act
        var cut = RenderComponent<ProjectsIndex>();

        // Assert - エラーが発生してもクラッシュせず、空のリストが表示される
        var emptyState = cut.Find(".empty-state");
        Assert.Contains("プロジェクトがありません", emptyState.TextContent);
    }
}
