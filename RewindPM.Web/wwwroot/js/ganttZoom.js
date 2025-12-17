// ガントチャートのズーム機能
window.ganttZoom = {
    // 定数定義（GanttConstants.cs と一致させる）
    CONSTANTS: {
        TASK_NAME_WIDTH: 280,
        SCROLLBAR_WIDTH: 15,
        HEADER_HEIGHT: 64,
        MARGIN: 20,
        MIN_AVAILABLE_WIDTH: 100,
        MIN_AVAILABLE_HEIGHT: 100,
        FALLBACK_WIDTH: 800,
        FALLBACK_HEIGHT: 600
    },

    dragState: null,

    getAvailableWidth: function() {
        const ganttScrollContainer = document.querySelector('.gantt-scroll-container');
        if (!ganttScrollContainer) {
            console.warn('gantt-scroll-container not found, using fallback width');
            return this.CONSTANTS.FALLBACK_WIDTH;
        }

        const chartWidth = ganttScrollContainer.clientWidth;
        const availableWidth = chartWidth
            - this.CONSTANTS.TASK_NAME_WIDTH
            - this.CONSTANTS.SCROLLBAR_WIDTH
            - this.CONSTANTS.MARGIN;

        return Math.max(this.CONSTANTS.MIN_AVAILABLE_WIDTH, availableWidth);
    },

    getAvailableHeight: function() {
        const ganttScrollContainer = document.querySelector('.gantt-scroll-container');
        if (!ganttScrollContainer) {
            console.warn('gantt-scroll-container not found, using fallback height');
            return this.CONSTANTS.FALLBACK_HEIGHT;
        }

        const containerHeight = ganttScrollContainer.clientHeight;
        const availableHeight = containerHeight
            - this.CONSTANTS.HEADER_HEIGHT
            - this.CONSTANTS.SCROLLBAR_WIDTH
            - this.CONSTANTS.MARGIN;

        return Math.max(this.CONSTANTS.MIN_AVAILABLE_HEIGHT, availableHeight);
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
