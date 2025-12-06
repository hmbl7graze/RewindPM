using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RewindPM.Application.Read.DTOs;
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

        // Act
        var cut = RenderComponent<ProjectCard>(parameters => parameters
            .Add(p => p.Project, project));

        // Assert
        var stat = cut.Find(".project-card-stat");
        Assert.Contains("TODO", stat.TextContent);
        Assert.Contains("タスク統計", stat.TextContent);
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

        var navMan = Services.GetRequiredService<NavigationManager>();

        // Act
        var cut = RenderComponent<ProjectCard>(parameters => parameters
            .Add(p => p.Project, project));

        var card = cut.Find(".project-card");
        card.Click();

        // Assert
        Assert.Equal($"http://localhost/projects/{projectId}", navMan.Uri);
    }
}
