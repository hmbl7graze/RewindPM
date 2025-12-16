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
    }
};
