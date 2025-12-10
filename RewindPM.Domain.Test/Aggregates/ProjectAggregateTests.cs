using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Domain.Test.TestHelpers;

namespace RewindPM.Domain.Test.Aggregates;

public class ProjectAggregateTests
{
    private readonly IDateTimeProvider _dateTimeProvider = new TestDateTimeProvider();
    [Fact(DisplayName = "有効な値でプロジェクトを作成できる")]
    public void Create_WithValidValues_ShouldCreateProject()
    {
        // Arrange
        var id = Guid.NewGuid();
        var title = "新しいプロジェクト";
        var description = "プロジェクトの説明";
        var createdBy = "user123";

        // Act
        var project = ProjectAggregate.Create(id, title, description, createdBy, _dateTimeProvider);

        // Assert
        Assert.Equal(id, project.Id);
        Assert.Equal(title, project.Title);
        Assert.Equal(description, project.Description);
        Assert.Equal(createdBy, project.CreatedBy);
        Assert.Equal(createdBy, project.UpdatedBy);
    }

    [Fact(DisplayName = "プロジェクト作成時にProjectCreatedイベントが発生する")]
    public void Create_ShouldRaiseProjectCreatedEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var title = "新しいプロジェクト";
        var description = "プロジェクトの説明";
        var createdBy = "user123";

        // Act
        var project = ProjectAggregate.Create(id, title, description, createdBy, _dateTimeProvider);

        // Assert
        Assert.Single(project.UncommittedEvents);
        var @event = project.UncommittedEvents.First();
        Assert.IsType<ProjectCreated>(@event);

        var projectCreatedEvent = (ProjectCreated)@event;
        Assert.Equal(id, projectCreatedEvent.AggregateId);
        Assert.Equal(title, projectCreatedEvent.Title);
        Assert.Equal(description, projectCreatedEvent.Description);
        Assert.Equal(createdBy, projectCreatedEvent.CreatedBy);
    }

    [Fact(DisplayName = "タイトルがnullの場合、DomainExceptionをスローする")]
    public void Create_WhenTitleIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var id = Guid.NewGuid();
        string title = null!;
        var description = "プロジェクトの説明";
        var createdBy = "user123";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            ProjectAggregate.Create(id, title, description, createdBy, _dateTimeProvider));
        Assert.Equal("プロジェクトのタイトルは必須です", exception.Message);
    }

    [Fact(DisplayName = "タイトルが空文字列の場合、DomainExceptionをスローする")]
    public void Create_WhenTitleIsEmpty_ShouldThrowDomainException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var title = "";
        var description = "プロジェクトの説明";
        var createdBy = "user123";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            ProjectAggregate.Create(id, title, description, createdBy, _dateTimeProvider));
        Assert.Equal("プロジェクトのタイトルは必須です", exception.Message);
    }

    [Fact(DisplayName = "作成者がnullの場合、DomainExceptionをスローする")]
    public void Create_WhenCreatedByIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var title = "新しいプロジェクト";
        var description = "プロジェクトの説明";
        string createdBy = null!;

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            ProjectAggregate.Create(id, title, description, createdBy, _dateTimeProvider));
        Assert.Equal("作成者のユーザーIDは必須です", exception.Message);
    }

    [Fact(DisplayName = "説明がnullの場合、空文字列として扱われる")]
    public void Create_WhenDescriptionIsNull_ShouldUseEmptyString()
    {
        // Arrange
        var id = Guid.NewGuid();
        var title = "新しいプロジェクト";
        string description = null!;
        var createdBy = "user123";

        // Act
        var project = ProjectAggregate.Create(id, title, description, createdBy, _dateTimeProvider);

        // Assert
        Assert.Equal(string.Empty, project.Description);
    }

    [Fact(DisplayName = "プロジェクトの情報を更新できる")]
    public void Update_WithValidValues_ShouldUpdateProject()
    {
        // Arrange
        var project = ProjectAggregate.Create(Guid.NewGuid(), "旧タイトル", "旧説明", "user123", _dateTimeProvider);
        project.ClearUncommittedEvents();

        var newTitle = "新タイトル";
        var newDescription = "新説明";
        var updatedBy = "user456";

        // Act
        project.Update(newTitle, newDescription, updatedBy, _dateTimeProvider);

        // Assert
        Assert.Equal(newTitle, project.Title);
        Assert.Equal(newDescription, project.Description);
        Assert.Equal(updatedBy, project.UpdatedBy);
        Assert.Equal("user123", project.CreatedBy); // 作成者は変わらない
    }

    [Fact(DisplayName = "プロジェクト更新時にProjectUpdatedイベントが発生する")]
    public void Update_ShouldRaiseProjectUpdatedEvent()
    {
        // Arrange
        var project = ProjectAggregate.Create(Guid.NewGuid(), "旧タイトル", "旧説明", "user123", _dateTimeProvider);
        project.ClearUncommittedEvents();

        var newTitle = "新タイトル";
        var newDescription = "新説明";
        var updatedBy = "user456";

        // Act
        project.Update(newTitle, newDescription, updatedBy, _dateTimeProvider);

        // Assert
        Assert.Single(project.UncommittedEvents);
        var @event = project.UncommittedEvents.First();
        Assert.IsType<ProjectUpdated>(@event);

        var projectUpdatedEvent = (ProjectUpdated)@event;
        Assert.Equal(project.Id, projectUpdatedEvent.AggregateId);
        Assert.Equal(newTitle, projectUpdatedEvent.Title);
        Assert.Equal(newDescription, projectUpdatedEvent.Description);
        Assert.Equal(updatedBy, projectUpdatedEvent.UpdatedBy);
    }

    [Fact(DisplayName = "更新時にタイトルがnullの場合、DomainExceptionをスローする")]
    public void Update_WhenTitleIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var project = ProjectAggregate.Create(Guid.NewGuid(), "旧タイトル", "旧説明", "user123", _dateTimeProvider);
        string newTitle = null!;
        var newDescription = "新説明";
        var updatedBy = "user456";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            project.Update(newTitle, newDescription, updatedBy, _dateTimeProvider));
        Assert.Equal("プロジェクトのタイトルは必須です", exception.Message);
    }

    [Fact(DisplayName = "更新時に更新者がnullの場合、DomainExceptionをスローする")]
    public void Update_WhenUpdatedByIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var project = ProjectAggregate.Create(Guid.NewGuid(), "旧タイトル", "旧説明", "user123", _dateTimeProvider);
        var newTitle = "新タイトル";
        var newDescription = "新説明";
        string updatedBy = null!;

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            project.Update(newTitle, newDescription, updatedBy, _dateTimeProvider));
        Assert.Equal("更新者のユーザーIDは必須です", exception.Message);
    }

    [Fact(DisplayName = "イベントリプレイでプロジェクトの状態を復元できる")]
    public void ReplayEvents_ShouldRestoreProjectState()
    {
        // Arrange
        var id = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new ProjectCreated
            {
                AggregateId = id,
                Title = "元のタイトル",
                Description = "元の説明",
                CreatedBy = "user123"
            },
            new ProjectUpdated
            {
                AggregateId = id,
                Title = "更新されたタイトル",
                Description = "更新された説明",
                UpdatedBy = "user456"
            }
        };

        // Act
        var project = new ProjectAggregate();
        project.ReplayEvents(events);

        // Assert
        Assert.Equal(id, project.Id);
        Assert.Equal("更新されたタイトル", project.Title);
        Assert.Equal("更新された説明", project.Description);
        Assert.Equal("user123", project.CreatedBy);
        Assert.Equal("user456", project.UpdatedBy);
        Assert.Equal(1, project.Version); // 2つのイベント = Version 1 (0-indexed)
        Assert.Empty(project.UncommittedEvents); // リプレイ時は未コミットイベントなし
    }
}
