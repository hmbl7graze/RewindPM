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

    [Fact(DisplayName = "最新に戻るボタンは常に存在し、過去表示時のみ可視化される")]
    public void TimelineControl_TodayButtonVisible_OnlyWhenViewingPast()
    {
        // Arrange - 過去表示
        var cutPast = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero))
            .Add(p => p.EditDates, new List<DateTimeOffset> { new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero) }));

        // Assert - 過去表示時はボタンが存在し、btn-hiddenクラスを持たない
        var todayButton = cutPast.Find(".timeline-btn-reset");
        Assert.NotNull(todayButton);
        Assert.DoesNotContain("btn-hidden", todayButton.ClassName);

        // Arrange - 最新表示
        var cutLatest = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, new List<DateTimeOffset> { new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero) }));

        // Assert - 最新表示時はボタンが存在するが、btn-hiddenクラスを持つ
        var todayButtonLatest = cutLatest.Find(".timeline-btn-reset");
        Assert.NotNull(todayButtonLatest);
        Assert.Contains("btn-hidden", todayButtonLatest.ClassName);
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

    #region スライドバーのテスト

    [Fact(DisplayName = "編集日が存在する場合、スライドバーが表示される")]
    public void TimelineControl_SliderVisible_WhenEditDatesExist()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates));

        // Assert
        var sliderContainer = cut.FindAll(".timeline-slider-container");
        Assert.NotEmpty(sliderContainer);

        var slider = cut.Find("input.timeline-slider");
        Assert.NotNull(slider);
    }

    [Fact(DisplayName = "編集日が存在しない場合、スライドバーが非表示")]
    public void TimelineControl_SliderHidden_WhenNoEditDates()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>();

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates));

        // Assert
        var sliderContainer = cut.FindAll(".timeline-slider-container");
        Assert.Empty(sliderContainer);
    }

    [Fact(DisplayName = "スライドバーの最大値が編集日数と一致する")]
    public void TimelineControl_SliderMaxValue_EqualsEditDatesCount()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates));

        // Assert
        var slider = cut.Find("input.timeline-slider");
        var maxValue = slider.GetAttribute("max");
        Assert.Equal("3", maxValue); // 編集日が3つ
    }

    [Fact(DisplayName = "最新状態でスライドバーの値が最大値")]
    public void TimelineControl_SliderValue_IsMaxValue_WhenAtLatest()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates));

        // Assert
        var slider = cut.Find("input.timeline-slider");
        var value = slider.GetAttribute("value");
        Assert.Equal("3", value); // MaxSliderValue = editDates.Count = 3（右端=最新）
    }

    [Fact(DisplayName = "過去日付表示時、スライドバーの値が正しい")]
    public void TimelineControl_SliderValue_IsCorrect_WhenViewingPast()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero), // index 0
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero), // index 1
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)   // index 2
        };
        var currentDate = new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero);

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, currentDate)
            .Add(p => p.EditDates, editDates));

        // Assert
        var slider = cut.Find("input.timeline-slider");
        var value = slider.GetAttribute("value");
        // MaxSliderValue=3, index=1 → 3-1-1=1
        Assert.Equal("1", value);
    }

    [Fact(DisplayName = "スライドバーにARIAラベルが設定されている")]
    public void TimelineControl_Slider_HasAriaLabel()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero)
        };

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates));

        // Assert
        var slider = cut.Find("input.timeline-slider");
        var ariaLabel = slider.GetAttribute("aria-label");
        Assert.NotNull(ariaLabel);
        Assert.Equal("日付スライダー", ariaLabel);
    }

    [Fact(DisplayName = "スライドバーの目盛りが編集日数+1個表示される")]
    public void TimelineControl_SliderTicks_CountMatchesEditDates()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates));

        // Assert
        var ticks = cut.FindAll(".slider-tick");
        Assert.Equal(4, ticks.Count); // 編集日3つ + 最新(0) = 4つ
    }

    [Fact(DisplayName = "現在位置の目盛りがactiveクラスを持つ")]
    public void TimelineControl_ActiveTick_HasActiveClass()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };
        var currentDate = new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero);

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, currentDate)
            .Add(p => p.EditDates, editDates));

        // Assert
        var activeTicks = cut.FindAll(".slider-tick.active");
        Assert.Single(activeTicks); // 1つだけactiveのはず
    }

    [Fact(DisplayName = "スライドバーラベルが日付形式で表示される")]
    public void TimelineControl_SliderLabels_DisplayDatesInCorrectFormat()
    {
        // Arrange
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 15), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 10), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates));

        // Assert
        var labels = cut.FindAll(".slider-label");
        Assert.True(labels.Count >= 2); // 最低2つのラベル（最古と最新）が表示される

        // すべてのラベルが日付形式（MM/dd）であることを確認
        Assert.All(labels, label =>
        {
            // MM/dd形式（例: "01/05"）
            Assert.Matches(@"^\d{2}/\d{2}$", label.TextContent);
        });
    }

    [Fact(DisplayName = "編集日が多い場合、ラベル数が最大7個に制限される")]
    public void TimelineControl_SliderLabels_LimitedToMaximum()
    {
        // Arrange - 20個の編集日を作成
        var editDates = Enumerable.Range(1, 20)
            .Select(i => new DateTimeOffset(new DateTime(2025, 1, i), TimeSpan.Zero))
            .OrderByDescending(d => d)
            .ToList();

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates));

        // Assert
        var labels = cut.FindAll(".slider-label");
        Assert.True(labels.Count <= 7, $"ラベル数が7個を超えています: {labels.Count}個");
    }

    [Fact(DisplayName = "最初と最後のラベルが常に表示される")]
    public void TimelineControl_SliderLabels_FirstAndLastAlwaysDisplayed()
    {
        // Arrange - 15個の編集日を作成
        var editDates = Enumerable.Range(1, 15)
            .Select(i => new DateTimeOffset(new DateTime(2025, 1, i), TimeSpan.Zero))
            .OrderByDescending(d => d)
            .ToList();

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates));

        // Assert
        var labels = cut.FindAll(".slider-label");
        Assert.NotEmpty(labels);

        var firstLabel = labels.First();
        var lastLabel = labels.Last();

        // 最初のラベルは最古の日付（01/01）
        Assert.Equal("01/01", firstLabel.TextContent);

        // 最後のラベルは最新の日付（01/15）
        Assert.Equal("01/15", lastLabel.TextContent);
    }

    [Fact(DisplayName = "編集日が少ない場合、すべての日付がラベルとして表示される")]
    public void TimelineControl_SliderLabels_AllDatesDisplayedWhenFew()
    {
        // Arrange - 3個の編集日を作成
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 3), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 2), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 1), TimeSpan.Zero)
        };

        // Act
        var cut = RenderComponent<TimelineControl>(parameters => parameters
            .Add(p => p.CurrentDate, null)
            .Add(p => p.EditDates, editDates));

        // Assert
        var labels = cut.FindAll(".slider-label");
        // 編集日3個 + 最新状態1個 = 4個のラベル
        Assert.Equal(4, labels.Count);
    }

    #endregion
}

