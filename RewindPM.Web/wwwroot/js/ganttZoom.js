// ガントチャートのズーム機能
window.ganttZoom = {
    dragState: null,

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
    },

    initializeDraggableToolbar: function() {
        const toolbar = document.querySelector('.gantt-zoom-toolbar');
        if (!toolbar) {
            console.warn('gantt-zoom-toolbar not found');
            return;
        }

        // イベントリスナーの重複登録を防ぐ
        if (toolbar.dataset.draggableInitialized) {
            return;
        }

        toolbar.addEventListener('mousedown', this.onToolbarMouseDown.bind(this));
        toolbar.dataset.draggableInitialized = 'true';
    },

    onToolbarMouseDown: function(e) {
        // ボタンクリック時はドラッグを開始しない
        if (e.target.tagName === 'BUTTON' || e.target.closest('button')) {
            return;
        }

        const toolbar = e.currentTarget;
        const rect = toolbar.getBoundingClientRect();

        // bottom/rightからtop/leftへの切り替え
        // 現在の位置を取得してtop/leftに設定
        toolbar.style.top = `${rect.top}px`;
        toolbar.style.left = `${rect.left}px`;
        toolbar.style.bottom = 'auto';
        toolbar.style.right = 'auto';

        // クリック位置のツールバー内でのオフセットを計算
        const offsetX = e.clientX - rect.left;
        const offsetY = e.clientY - rect.top;

        this.dragState = {
            toolbar: toolbar,
            offsetX: offsetX,
            offsetY: offsetY
        };

        document.addEventListener('mousemove', this.onMouseMove);
        document.addEventListener('mouseup', this.onMouseUp);

        e.preventDefault();
    },

    onMouseMove: (e) => {
        if (!window.ganttZoom.dragState) return;

        const state = window.ganttZoom.dragState;

        // マウスの位置からツールバー内のオフセットを引いた位置にツールバーを配置
        const newLeft = e.clientX - state.offsetX;
        const newTop = e.clientY - state.offsetY;

        // ツールバーの位置を更新
        state.toolbar.style.left = `${newLeft}px`;
        state.toolbar.style.top = `${newTop}px`;
        state.toolbar.style.right = 'auto'; // rightを無効化
    },

    onMouseUp: () => {
        if (!window.ganttZoom.dragState) return;

        document.removeEventListener('mousemove', window.ganttZoom.onMouseMove);
        document.removeEventListener('mouseup', window.ganttZoom.onMouseUp);

        window.ganttZoom.dragState = null;
    }
};
