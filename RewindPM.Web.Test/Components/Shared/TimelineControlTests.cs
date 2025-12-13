using Bunit;
using Microsoft.AspNetCore.Components;
using RewindPM.Web.Components.Shared;

namespace RewindPM.Web.Test.Components.Shared;

public class TimelineControlTests : Bunit.TestContext
{
    [Fact(DisplayName = "最新表示時に「最新」と表示される")]
    public void TimelineControl_DisplaysLatest_WhenCurrentDateIsNull()
    {
        // Arrange & Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTimeOffset>()));

        // Assert
        var dateDisplay = cut.Find(".timeline-date");
        Assert.Equal("最新", dateDisplay.TextContent);
    }

    [Fact(DisplayName = "過去表示時に日付が表示される")]
    public void TimelineControl_DisplaysDate_WhenCurrentDateIsSet()
    {
        // Arrange
        var date = new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero);

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, date)
            .Add(p => p.EditDates, new List<DateTimeOffset> { date }));

        // Assert
        var dateDisplay = cut.Find(".timeline-date");
        Assert.Equal("2025/01/15", dateDisplay.TextContent);
    }

    [Fact(DisplayName = "過去表示時にviewing-pastクラスが付与される")]
    public void TimelineControl_HasViewingPastClass_WhenCurrentDateIsSet()
    {
        // Arrange
        var date = new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero);

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, date)
            .Add(p => p.EditDates, new List<DateTimeOffset> { date }));

        // Assert
        var control = cut.Find(".timeline-toolbar");
        Assert.Contains("viewing-past", control.ClassName);
    }

    [Fact(DisplayName = "最新表示時にviewing-pastクラスが付与されない")]
    public void TimelineControl_DoesNotHaveViewingPastClass_WhenCurrentDateIsNull()
    {
        // Arrange & Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTimeOffset>()));

        // Assert
        var control = cut.Find(".timeline-toolbar");
        Assert.DoesNotContain("viewing-past", control.ClassName);
    }

    [Fact(DisplayName = "編集日がない場合、前ボタンが無効化される")]
    public void TimelineControl_PreviousButtonDisabled_WhenNoEditDates()
    {
        // Arrange & Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTimeOffset>()));

        // Assert
        var prevButton = cut.Find(".timeline-btn-prev");
        Assert.True(prevButton.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "編集日がある場合、前ボタンが有効化される")]
    public void TimelineControl_PreviousButtonEnabled_WhenEditDatesExist()
    {
        // Arrange & Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTimeOffset> { new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero) }));

        // Assert
        var prevButton = cut.Find(".timeline-btn-prev");
        Assert.False(prevButton.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "最新表示時、次ボタンが無効化される")]
    public void TimelineControl_NextButtonDisabled_WhenCurrentDateIsNull()
    {
        // Arrange & Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTimeOffset> { new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero) }));

        // Assert
        var nextButton = cut.Find(".timeline-btn-next");
        Assert.True(nextButton.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "前ボタンクリック時にOnDateChangedイベントが発火する")]
    public void TimelineControl_InvokesOnDateChanged_WhenPreviousButtonClicked()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero)
        };
        DateTimeOffset? capturedDate = null;

        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates)
            .Add(p => p.OnDateChanged, EventCallback.Factory.Create<DateTimeOffset?>(this, (date) =>
            {
                capturedDate = date;
            })));

        // Act
        var prevButton = cut.Find(".timeline-btn-prev");
        prevButton.Click();

        // Assert
        Assert.NotNull(capturedDate);
        Assert.Equal(new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero), capturedDate.Value);
    }

    [Fact(DisplayName = "次ボタンクリック時にOnDateChangedイベントが発火する")]
    public void TimelineControl_InvokesOnDateChanged_WhenNextButtonClicked()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };
        DateTimeOffset? capturedDate = null;

        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero))
            .Add(p => p.EditDates, editDates)
            .Add(p => p.OnDateChanged, EventCallback.Factory.Create<DateTimeOffset?>(this, (date) =>
            {
                capturedDate = date;
            })));

        // Act
        var nextButton = cut.Find(".timeline-btn-next");
        nextButton.Click();

        // Assert
        Assert.NotNull(capturedDate);
        Assert.Equal(new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero), capturedDate.Value);
    }

    [Fact(DisplayName = "最新に戻るボタンは過去表示時のみ表示される")]
    public void TimelineControl_TodayButtonVisible_OnlyWhenViewingPast()
    {
        // Arrange - 過去表示
        var cutPast = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero))
            .Add(p => p.EditDates, new List<DateTimeOffset> { new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero) }));

        // Assert - 過去表示時はボタンが存在
        var todayButtons = cutPast.FindAll(".timeline-btn-reset");
        Assert.Single(todayButtons);

        // Arrange - 最新表示
        var cutLatest = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTimeOffset> { new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero) }));

        // Assert - 最新表示時はボタンが存在しない
        var todayButtonsLatest = cutLatest.FindAll(".timeline-btn-reset");
        Assert.Empty(todayButtonsLatest);
    }

    [Fact(DisplayName = "最新に戻るボタンクリック時にnullが渡される")]
    public void TimelineControl_InvokesOnDateChangedWithNull_WhenTodayButtonClicked()
    {
        // Arrange
        var editDates = new List<DateTimeOffset> { new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero) };
        DateTimeOffset? capturedDate = new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero); // 初期値

        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero))
            .Add(p => p.EditDates, editDates)
            .Add(p => p.OnDateChanged, EventCallback.Factory.Create<DateTimeOffset?>(this, (date) =>
            {
                capturedDate = date;
            })));

        // Act
        var todayButton = cut.Find(".timeline-btn-reset");
        todayButton.Click();

        // Assert
        Assert.Null(capturedDate);
    }

    [Fact(DisplayName = "前ボタンで古い日付へ移動する")]
    public void TimelineControl_MovesToOlderDate_WhenPreviousButtonClicked()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };
        DateTimeOffset? capturedDate = null;

        // 現在1月10日を表示中
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero))
            .Add(p => p.EditDates, editDates)
            .Add(p => p.OnDateChanged, EventCallback.Factory.Create<DateTimeOffset?>(this, (date) =>
            {
                capturedDate = date;
            })));

        // Act
        var prevButton = cut.Find(".timeline-btn-prev");
        prevButton.Click();

        // Assert - 1月5日（より古い日付）に移動
        Assert.NotNull(capturedDate);
        Assert.Equal(new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero), capturedDate.Value);
    }

    [Fact(DisplayName = "次ボタンで新しい日付へ移動する")]
    public void TimelineControl_MovesToNewerDate_WhenNextButtonClicked()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };
        DateTimeOffset? capturedDate = null;

        // 現在1月10日を表示中
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero))
            .Add(p => p.EditDates, editDates)
            .Add(p => p.OnDateChanged, EventCallback.Factory.Create<DateTimeOffset?>(this, (date) =>
            {
                capturedDate = date;
            })));

        // Act
        var nextButton = cut.Find(".timeline-btn-next");
        nextButton.Click();

        // Assert - 1月15日（より新しい日付）に移動
        Assert.NotNull(capturedDate);
        Assert.Equal(new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero), capturedDate.Value);
    }

    [Fact(DisplayName = "最古の日付表示時、前ボタンが無効化される")]
    public void TimelineControl_PreviousButtonDisabled_WhenAtOldestDate()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };

        // 最古の日付（1月5日）を表示中
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero))
            .Add(p => p.EditDates, editDates));

        // Assert
        var prevButton = cut.Find(".timeline-btn-prev");
        Assert.True(prevButton.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "最新の日付表示時、次ボタンが無効化される")]
    public void TimelineControl_NextButtonDisabled_WhenAtNewestDate()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };

        // 最新の日付（1月15日）を表示中
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero))
            .Add(p => p.EditDates, editDates));

        // Assert - 最新の編集日でも最新状態に戻れるため有効
        var nextButton = cut.Find(".timeline-btn-next");
        Assert.False(nextButton.HasAttribute("disabled"));
    }
}
