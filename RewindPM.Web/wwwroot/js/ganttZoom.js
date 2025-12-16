// ガントチャートのズーム機能
window.ganttZoom = {
    getAvailableWidth: function() {
        const ganttScrollContainer = document.querySelector('.gantt-scroll-container');
        if (!ganttScrollContainer) {
            console.warn('gantt-scroll-container not found, using fallback width');
            return 800; // フォールバック
        }

        const chartWidth = ganttScrollContainer.clientWidth;
        const taskNameWidth = 280; // .gantt-sticky-left の幅
        const scrollbarWidth = 15; // スクロールバーの概算幅
        const margin = 20; // マージン

        const availableWidth = chartWidth - taskNameWidth - scrollbarWidth - margin;

        // 最小幅を保証
        return Math.max(100, availableWidth);
    },

    getAvailableHeight: function() {
        const ganttScrollContainer = document.querySelector('.gantt-scroll-container');
        if (!ganttScrollContainer) {
            console.warn('gantt-scroll-container not found, using fallback height');
            return 600; // フォールバック
        }

        const containerHeight = ganttScrollContainer.clientHeight;
        const headerHeight = 64; // ヘッダー行の高さ（月行32px + 日付行32px）
        const scrollbarHeight = 15; // スクロールバーの概算高さ
        const margin = 20; // マージン

        const availableHeight = containerHeight - headerHeight - scrollbarHeight - margin;

        // 最小高さを保証
        return Math.max(100, availableHeight);
    }
};
