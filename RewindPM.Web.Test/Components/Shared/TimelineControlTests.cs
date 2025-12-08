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
            .Add(p => p.EditDates, new List<DateTime>()));

        // Assert
        var dateDisplay = cut.Find(".timeline-date");
        Assert.Equal("最新", dateDisplay.TextContent);
    }

    [Fact(DisplayName = "過去表示時に日付が表示される")]
    public void TimelineControl_DisplaysDate_WhenCurrentDateIsSet()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, date)
            .Add(p => p.EditDates, new List<DateTime> { date }));

        // Assert
        var dateDisplay = cut.Find(".timeline-date");
        Assert.Equal("2025年01月15日", dateDisplay.TextContent);
    }

    [Fact(DisplayName = "過去表示時にviewing-pastクラスが付与される")]
    public void TimelineControl_HasViewingPastClass_WhenCurrentDateIsSet()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, date)
            .Add(p => p.EditDates, new List<DateTime> { date }));

        // Assert
        var control = cut.Find(".timeline-control");
        Assert.Contains("viewing-past", control.ClassName);
    }

    [Fact(DisplayName = "最新表示時にviewing-pastクラスが付与されない")]
    public void TimelineControl_DoesNotHaveViewingPastClass_WhenCurrentDateIsNull()
    {
        // Arrange & Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTime>()));

        // Assert
        var control = cut.Find(".timeline-control");
        Assert.DoesNotContain("viewing-past", control.ClassName);
    }

    [Fact(DisplayName = "編集日がない場合、前ボタンが無効化される")]
    public void TimelineControl_PreviousButtonDisabled_WhenNoEditDates()
    {
        // Arrange & Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTime>()));

        // Assert
        var prevButton = cut.Find(".timeline-button-prev");
        Assert.True(prevButton.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "編集日がある場合、前ボタンが有効化される")]
    public void TimelineControl_PreviousButtonEnabled_WhenEditDatesExist()
    {
        // Arrange & Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTime> { new DateTime(2025, 1, 15) }));

        // Assert
        var prevButton = cut.Find(".timeline-button-prev");
        Assert.False(prevButton.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "最新表示時、次ボタンが無効化される")]
    public void TimelineControl_NextButtonDisabled_WhenCurrentDateIsNull()
    {
        // Arrange & Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTime> { new DateTime(2025, 1, 15) }));

        // Assert
        var nextButton = cut.Find(".timeline-button-next");
        Assert.True(nextButton.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "前ボタンクリック時にOnDateChangedイベントが発火する")]
    public void TimelineControl_InvokesOnDateChanged_WhenPreviousButtonClicked()
    {
        // Arrange
        var editDates = new List<DateTime>
        {
            new DateTime(2025, 1, 15),
            new DateTime(2025, 1, 10)
        };
        DateTime? capturedDate = null;

        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates)
            .Add(p => p.OnDateChanged, EventCallback.Factory.Create<DateTime?>(this, (date) =>
            {
                capturedDate = date;
            })));

        // Act
        var prevButton = cut.Find(".timeline-button-prev");
        prevButton.Click();

        // Assert
        Assert.NotNull(capturedDate);
        Assert.Equal(new DateTime(2025, 1, 15), capturedDate.Value);
    }

    [Fact(DisplayName = "次ボタンクリック時にOnDateChangedイベントが発火する")]
    public void TimelineControl_InvokesOnDateChanged_WhenNextButtonClicked()
    {
        // Arrange
        var editDates = new List<DateTime>
        {
            new DateTime(2025, 1, 15),
            new DateTime(2025, 1, 10),
            new DateTime(2025, 1, 5)
        };
        DateTime? capturedDate = null;

        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTime(2025, 1, 5))
            .Add(p => p.EditDates, editDates)
            .Add(p => p.OnDateChanged, EventCallback.Factory.Create<DateTime?>(this, (date) =>
            {
                capturedDate = date;
            })));

        // Act
        var nextButton = cut.Find(".timeline-button-next");
        nextButton.Click();

        // Assert
        Assert.NotNull(capturedDate);
        Assert.Equal(new DateTime(2025, 1, 10), capturedDate.Value);
    }

    [Fact(DisplayName = "最新に戻るボタンは過去表示時のみ表示される")]
    public void TimelineControl_TodayButtonVisible_OnlyWhenViewingPast()
    {
        // Arrange - 過去表示
        var cutPast = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTime(2025, 1, 15))
            .Add(p => p.EditDates, new List<DateTime> { new DateTime(2025, 1, 15) }));

        // Assert - 過去表示時はボタンが存在
        var todayButtons = cutPast.FindAll(".timeline-button-today");
        Assert.Single(todayButtons);

        // Arrange - 最新表示
        var cutLatest = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTime> { new DateTime(2025, 1, 15) }));

        // Assert - 最新表示時はボタンが存在しない
        var todayButtonsLatest = cutLatest.FindAll(".timeline-button-today");
        Assert.Empty(todayButtonsLatest);
    }

    [Fact(DisplayName = "最新に戻るボタンクリック時にnullが渡される")]
    public void TimelineControl_InvokesOnDateChangedWithNull_WhenTodayButtonClicked()
    {
        // Arrange
        var editDates = new List<DateTime> { new DateTime(2025, 1, 15) };
        DateTime? capturedDate = new DateTime(2025, 1, 15); // 初期値

        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTime(2025, 1, 15))
            .Add(p => p.EditDates, editDates)
            .Add(p => p.OnDateChanged, EventCallback.Factory.Create<DateTime?>(this, (date) =>
            {
                capturedDate = date;
            })));

        // Act
        var todayButton = cut.Find(".timeline-button-today");
        todayButton.Click();

        // Assert
        Assert.Null(capturedDate);
    }

    [Fact(DisplayName = "前ボタンで古い日付へ移動する")]
    public void TimelineControl_MovesToOlderDate_WhenPreviousButtonClicked()
    {
        // Arrange
        var editDates = new List<DateTime>
        {
            new DateTime(2025, 1, 15),
            new DateTime(2025, 1, 10),
            new DateTime(2025, 1, 5)
        };
        DateTime? capturedDate = null;

        // 現在1月10日を表示中
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTime(2025, 1, 10))
            .Add(p => p.EditDates, editDates)
            .Add(p => p.OnDateChanged, EventCallback.Factory.Create<DateTime?>(this, (date) =>
            {
                capturedDate = date;
            })));

        // Act
        var prevButton = cut.Find(".timeline-button-prev");
        prevButton.Click();

        // Assert - 1月5日（より古い日付）に移動
        Assert.NotNull(capturedDate);
        Assert.Equal(new DateTime(2025, 1, 5), capturedDate.Value);
    }

    [Fact(DisplayName = "次ボタンで新しい日付へ移動する")]
    public void TimelineControl_MovesToNewerDate_WhenNextButtonClicked()
    {
        // Arrange
        var editDates = new List<DateTime>
        {
            new DateTime(2025, 1, 15),
            new DateTime(2025, 1, 10),
            new DateTime(2025, 1, 5)
        };
        DateTime? capturedDate = null;

        // 現在1月10日を表示中
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTime(2025, 1, 10))
            .Add(p => p.EditDates, editDates)
            .Add(p => p.OnDateChanged, EventCallback.Factory.Create<DateTime?>(this, (date) =>
            {
                capturedDate = date;
            })));

        // Act
        var nextButton = cut.Find(".timeline-button-next");
        nextButton.Click();

        // Assert - 1月15日（より新しい日付）に移動
        Assert.NotNull(capturedDate);
        Assert.Equal(new DateTime(2025, 1, 15), capturedDate.Value);
    }

    [Fact(DisplayName = "最古の日付表示時、前ボタンが無効化される")]
    public void TimelineControl_PreviousButtonDisabled_WhenAtOldestDate()
    {
        // Arrange
        var editDates = new List<DateTime>
        {
            new DateTime(2025, 1, 15),
            new DateTime(2025, 1, 10),
            new DateTime(2025, 1, 5)
        };

        // 最古の日付（1月5日）を表示中
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTime(2025, 1, 5))
            .Add(p => p.EditDates, editDates));

        // Assert
        var prevButton = cut.Find(".timeline-button-prev");
        Assert.True(prevButton.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "最新の日付表示時、次ボタンが無効化される")]
    public void TimelineControl_NextButtonDisabled_WhenAtNewestDate()
    {
        // Arrange
        var editDates = new List<DateTime>
        {
            new DateTime(2025, 1, 15),
            new DateTime(2025, 1, 10),
            new DateTime(2025, 1, 5)
        };

        // 最新の日付（1月15日）を表示中
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTime(2025, 1, 15))
            .Add(p => p.EditDates, editDates));

        // Assert
        var nextButton = cut.Find(".timeline-button-next");
        Assert.True(nextButton.HasAttribute("disabled"));
    }
}
