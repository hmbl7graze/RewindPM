using RewindPM.Web.Components.Statistics;

namespace RewindPM.Web.Test.Components.Statistics;

public class StatisticsTitlesTests
{
    [Fact(DisplayName = "StatisticsTitles: すべてのタスク統計の説明文が定義されている")]
    public void StatisticsTitles_AllTaskStatistics_AreDefined()
    {
        // Assert - タスク統計
        Assert.NotNull(StatisticsTitles.CompletionRate);
        Assert.NotEmpty(StatisticsTitles.CompletionRate);

        Assert.NotNull(StatisticsTitles.TotalTasks);
        Assert.NotEmpty(StatisticsTitles.TotalTasks);

        Assert.NotNull(StatisticsTitles.CompletedTasks);
        Assert.NotEmpty(StatisticsTitles.CompletedTasks);

        Assert.NotNull(StatisticsTitles.InProgressTasks);
        Assert.NotEmpty(StatisticsTitles.InProgressTasks);

        Assert.NotNull(StatisticsTitles.InReviewTasks);
        Assert.NotEmpty(StatisticsTitles.InReviewTasks);

        Assert.NotNull(StatisticsTitles.TodoTasks);
        Assert.NotEmpty(StatisticsTitles.TodoTasks);
    }

    [Fact(DisplayName = "StatisticsTitles: すべての工数統計の説明文が定義されている")]
    public void StatisticsTitles_AllHoursStatistics_AreDefined()
    {
        // Assert - 工数統計
        Assert.NotNull(StatisticsTitles.HoursConsumptionRate);
        Assert.NotEmpty(StatisticsTitles.HoursConsumptionRate);

        Assert.NotNull(StatisticsTitles.TotalEstimatedHours);
        Assert.NotEmpty(StatisticsTitles.TotalEstimatedHours);

        Assert.NotNull(StatisticsTitles.TotalActualHours);
        Assert.NotEmpty(StatisticsTitles.TotalActualHours);

        Assert.NotNull(StatisticsTitles.HoursOverrun);
        Assert.NotEmpty(StatisticsTitles.HoursOverrun);

        Assert.NotNull(StatisticsTitles.RemainingEstimatedHours);
        Assert.NotEmpty(StatisticsTitles.RemainingEstimatedHours);
    }

    [Fact(DisplayName = "StatisticsTitles: すべてのスケジュール統計の説明文が定義されている")]
    public void StatisticsTitles_AllScheduleStatistics_AreDefined()
    {
        // Assert - スケジュール統計
        Assert.NotNull(StatisticsTitles.OnTimeRate);
        Assert.NotEmpty(StatisticsTitles.OnTimeRate);

        Assert.NotNull(StatisticsTitles.OnTimeTasks);
        Assert.NotEmpty(StatisticsTitles.OnTimeTasks);

        Assert.NotNull(StatisticsTitles.DelayedTasks);
        Assert.NotEmpty(StatisticsTitles.DelayedTasks);

        Assert.NotNull(StatisticsTitles.AverageDelayDays);
        Assert.NotEmpty(StatisticsTitles.AverageDelayDays);
    }

    [Fact(DisplayName = "StatisticsTitles: すべての見積もり精度統計の説明文が定義されている")]
    public void StatisticsTitles_AllEstimateAccuracyStatistics_AreDefined()
    {
        // Assert - 見積もり精度統計
        Assert.NotNull(StatisticsTitles.EstimateAccuracyRate);
        Assert.NotEmpty(StatisticsTitles.EstimateAccuracyRate);

        Assert.NotNull(StatisticsTitles.AccurateEstimateTasks);
        Assert.NotEmpty(StatisticsTitles.AccurateEstimateTasks);

        Assert.NotNull(StatisticsTitles.OverEstimateTasks);
        Assert.NotEmpty(StatisticsTitles.OverEstimateTasks);

        Assert.NotNull(StatisticsTitles.UnderEstimateTasks);
        Assert.NotEmpty(StatisticsTitles.UnderEstimateTasks);

        Assert.NotNull(StatisticsTitles.AverageEstimateErrorDays);
        Assert.NotEmpty(StatisticsTitles.AverageEstimateErrorDays);
    }

    [Fact(DisplayName = "StatisticsTitles: CompletionRateの説明文に計算式が含まれている")]
    public void StatisticsTitles_CompletionRate_ContainsFormula()
    {
        // Assert
        Assert.Contains("完了率", StatisticsTitles.CompletionRate);
        Assert.Contains("完了したタスク数", StatisticsTitles.CompletionRate);
        Assert.Contains("全タスク数", StatisticsTitles.CompletionRate);
        Assert.Contains("100%", StatisticsTitles.CompletionRate);
    }

    [Fact(DisplayName = "StatisticsTitles: HoursConsumptionRateの説明文に計算式が含まれている")]
    public void StatisticsTitles_HoursConsumptionRate_ContainsFormula()
    {
        // Assert
        Assert.Contains("工数消費率", StatisticsTitles.HoursConsumptionRate);
        Assert.Contains("実績工数", StatisticsTitles.HoursConsumptionRate);
        Assert.Contains("予定工数", StatisticsTitles.HoursConsumptionRate);
        Assert.Contains("100%", StatisticsTitles.HoursConsumptionRate);
    }

    [Fact(DisplayName = "StatisticsTitles: OnTimeRateの説明文に計算式が含まれている")]
    public void StatisticsTitles_OnTimeRate_ContainsFormula()
    {
        // Assert
        Assert.Contains("期限内完了率", StatisticsTitles.OnTimeRate);
        Assert.Contains("期限内完了数", StatisticsTitles.OnTimeRate);
        Assert.Contains("完了済みタスク数", StatisticsTitles.OnTimeRate);
        Assert.Contains("100%", StatisticsTitles.OnTimeRate);
    }

    [Fact(DisplayName = "StatisticsTitles: EstimateAccuracyRateの説明文に精度判定基準が含まれている")]
    public void StatisticsTitles_EstimateAccuracyRate_ContainsAccuracyCriteria()
    {
        // Assert
        Assert.Contains("見積もり精度率", StatisticsTitles.EstimateAccuracyRate);
        Assert.Contains("正確な見積り数", StatisticsTitles.EstimateAccuracyRate);
        Assert.Contains("±10%", StatisticsTitles.EstimateAccuracyRate);
        Assert.Contains("±1日", StatisticsTitles.EstimateAccuracyRate);
    }

    [Fact(DisplayName = "StatisticsTitles: すべての定数が100文字以内である")]
    public void StatisticsTitles_AllConstants_AreWithin100Characters()
    {
        // Arrange - すべての定数値を取得
        var fields = typeof(StatisticsTitles).GetFields();

        // Assert
        foreach (var field in fields)
        {
            var value = field.GetValue(null) as string;
            Assert.NotNull(value);
            Assert.True(value.Length <= 100,
                $"{field.Name} の説明文が100文字を超えています: {value.Length}文字");
        }
    }
}
