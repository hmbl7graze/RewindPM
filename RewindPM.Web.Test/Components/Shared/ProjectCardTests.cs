using Bunit;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Statistics;
using RewindPM.Web.Components.Shared;

namespace RewindPM.Web.Test.Components.Shared;

public class ProjectCardTests : Bunit.TestContext
{
    [Fact(DisplayName = "プロジェクトカードがプロジェクトタイトルを表示する")]
    public void ProjectCard_RendersProjectTitle()
    {
        // Arrange
        var project = new ProjectDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            CreatedBy = "test-user"
        };

        var mockMediator = Substitute.For<IMediator>();
        Services.AddSingleton(mockMediator);

        // Act
        var cut = RenderComponent<ProjectCard>(parameters => parameters
            .Add(p => p.Project, project));

        // Assert
        var title = cut.Find(".project-card-title");
        Assert.Equal("Test Project", title.TextContent);
    }

    [Fact(DisplayName = "プロジェクトカードにタスク統計のTODOメッセージを表示する")]
    public void ProjectCard_DisplaysTodoMessageForStatistics()
    {
        // Arrange
        var project = new ProjectDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            CreatedBy = "test-user"
        };

        var mockMediator = Substitute.For<IMediator>();
        Services.AddSingleton(mockMediator);

        // Act
        var cut = RenderComponent<ProjectCard>(parameters => parameters
            .Add(p => p.Project, project));

        // Assert
        // 統計情報が読み込まれるまでは「統計を読み込み中...」が表示される
        var stat = cut.Find(".project-card-stat");
        Assert.Contains("統計を読み込み中", stat.TextContent);
    }

    [Fact(DisplayName = "長い説明文を切り詰めて表示する")]
    public void ProjectCard_TruncatesLongDescription()
    {
        // Arrange
        var longDescription = string.Join("\n", Enumerable.Repeat("This is a line of text that will be truncated", 5));
        var project = new ProjectDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = longDescription,
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            CreatedBy = "test-user"
        };

        var mockMediator = Substitute.For<IMediator>();
        Services.AddSingleton(mockMediator);

        // Act
        var cut = RenderComponent<ProjectCard>(parameters => parameters
            .Add(p => p.Project, project));

        // Assert
        var description = cut.Find(".project-card-description");
        Assert.Contains("...", description.TextContent);
    }

    [Fact(DisplayName = "説明が空の場合に「説明なし」と表示する")]
    public void ProjectCard_DisplaysEmptyDescriptionMessage_WhenDescriptionIsNull()
    {
        // Arrange
        var project = new ProjectDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = string.Empty,
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            CreatedBy = "test-user"
        };

        var mockMediator = Substitute.For<IMediator>();
        Services.AddSingleton(mockMediator);

        // Act
        var cut = RenderComponent<ProjectCard>(parameters => parameters
            .Add(p => p.Project, project));

        // Assert
        var description = cut.Find(".project-card-description");
        Assert.Equal("説明なし", description.TextContent);
    }

    [Fact(DisplayName = "プロジェクトカードクリック時に詳細ページに遷移する")]
    public void ProjectCard_NavigatesToDetailPage_WhenClicked()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new ProjectDto
        {
            Id = projectId,
            Title = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            CreatedBy = "test-user"
        };

        var mockMediator = Substitute.For<IMediator>();
        Services.AddSingleton(mockMediator);

        // Act
        var cut = RenderComponent<ProjectCard>(parameters => parameters
            .Add(p => p.Project, project));

        // Assert - アンカータグのhref属性を検証
        var link = cut.Find(".project-card-link");
        Assert.Equal($"/projects/{projectId}", link.GetAttribute("href"));
    }
}
