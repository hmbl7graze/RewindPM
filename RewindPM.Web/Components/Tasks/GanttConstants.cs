namespace RewindPM.Web.Components.Tasks;

/// <summary>
/// ガントチャートで使用する定数
/// </summary>
public static class GanttConstants
{
    /// <summary>
    /// ズームレベルの配列（内部用）
    /// </summary>
    private static readonly double[] ZoomLevelsInternal = { 1.0, 1.5, 2.0, 3.0, 4.0 };

    /// <summary>
    /// 公開用の読み取り専用ズームレベル
    /// </summary>
    public static System.Collections.Generic.IReadOnlyList<double> ZoomLevels { get; } = ZoomLevelsInternal;

    /// <summary>
    /// セル幅の設定
    /// </summary>
    public static class CellWidth
    {
        /// <summary>
        /// デフォルトの基準セル幅（横方向）
        /// </summary>
        public const double DefaultBase = 40.0;

        /// <summary>
        /// 最小セル幅
        /// </summary>
        public const double Min = 8.0;

        /// <summary>
        /// 最大セル幅
        /// </summary>
        public const double Max = 120.0;
    }

    /// <summary>
    /// 行の高さの設定
    /// </summary>
    public static class RowHeight
    {
        /// <summary>
        /// デフォルトの基準行高さ（縦方向）
        /// </summary>
        public const double DefaultBase = 48.0;

        /// <summary>
        /// 最小行高さ
        /// </summary>
        public const double Min = 24.0;

        /// <summary>
        /// 最大行高さ
        /// </summary>
        public const double Max = 60.0;
    }

    /// <summary>
    /// バーの高さの設定
    /// </summary>
    public static class Bar
    {
        /// <summary>
        /// バーの高さの行高さに対する比率
        /// </summary>
        public const double HeightRatio = 0.35;

        /// <summary>
        /// バーの最小高さ
        /// </summary>
        public const double MinHeight = 6.0;

        /// <summary>
        /// バーの最大高さ
        /// </summary>
        public const double MaxHeight = 16.0;

        /// <summary>
        /// バー間のギャップの行高さに対する比率
        /// </summary>
        public const double GapRatio = 0.02;

        /// <summary>
        /// バー間の最小ギャップ
        /// </summary>
        public const double MinGap = 0.0;

        /// <summary>
        /// バー間の最大ギャップ
        /// </summary>
        public const double MaxGap = 4.0;
    }

    /// <summary>
    /// ステータスバッジの設定
    /// </summary>
    public static class StatusBadge
    {
        /// <summary>
        /// フォントサイズの行高さに対する比率
        /// </summary>
        public const double FontSizeRatio = 0.25;

        /// <summary>
        /// 最小フォントサイズ（rem）
        /// </summary>
        public const double MinFontSize = 0.6;

        /// <summary>
        /// 最大フォントサイズ（rem）
        /// </summary>
        public const double MaxFontSize = 0.75;

        /// <summary>
        /// 縦方向パディングの行高さに対する比率
        /// </summary>
        public const double PaddingVerticalRatio = 0.02;

        /// <summary>
        /// 最小縦方向パディング（px）
        /// </summary>
        public const double MinPaddingVertical = 1.0;

        /// <summary>
        /// 最大縦方向パディング（px）
        /// </summary>
        public const double MaxPaddingVertical = 3.0;

        /// <summary>
        /// 横方向パディングの行高さに対する比率
        /// </summary>
        public const double PaddingHorizontalRatio = 0.12;

        /// <summary>
        /// 最小横方向パディング（px）
        /// </summary>
        public const double MinPaddingHorizontal = 4.0;

        /// <summary>
        /// 最大横方向パディング（px）
        /// </summary>
        public const double MaxPaddingHorizontal = 8.0;
    }

    /// <summary>
    /// 日付ラベルの表示制御のための設定
    /// </summary>
    public static class DateLabel
    {
        /// <summary>
        /// すべての日付を表示する最小セル幅
        /// </summary>
        public const double ShowAllThreshold = 30.0;

        /// <summary>
        /// 2日おきに表示する最小セル幅
        /// </summary>
        public const double ShowEvery2DaysThreshold = 18.0;

    }

    /// <summary>
    /// レイアウトの設定
    /// </summary>
    public static class Layout
    {
        /// <summary>
        /// タスク名列の幅
        /// </summary>
        public const double TaskNameWidth = 280.0;

        /// <summary>
        /// スクロールバーの概算幅
        /// </summary>
        public const double ScrollbarWidth = 15.0;

        /// <summary>
        /// ヘッダー行の高さ（月行 + 日付行）
        /// </summary>
        public const double HeaderHeight = 64.0;

        /// <summary>
        /// マージン
        /// </summary>
        public const double Margin = 20.0;

        /// <summary>
        /// 最小利用可能幅
        /// </summary>
        public const double MinAvailableWidth = 100.0;

        /// <summary>
        /// 最小利用可能高さ
        /// </summary>
        public const double MinAvailableHeight = 100.0;

        /// <summary>
        /// フォールバック幅（要素が見つからない場合）
        /// </summary>
        public const double FallbackWidth = 800.0;

        /// <summary>
        /// フォールバック高さ（要素が見つからない場合）
        /// </summary>
        public const double FallbackHeight = 600.0;
    }

    /// <summary>
    /// ローカルストレージのキー
    /// </summary>
    public static class StorageKeys
    {
        /// <summary>
        /// 横方向ズームレベルのキー
        /// </summary>
        public const string HorizontalZoom = "gantt_hZoom";

        /// <summary>
        /// 縦方向ズームレベルのキー
        /// </summary>
        public const string VerticalZoom = "gantt_vZoom";
    }
}
