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
        const container = toolbar.closest('.gantt-scroll-container');
        if (!container) return;

        const toolbarRect = toolbar.getBoundingClientRect();
        const containerRect = container.getBoundingClientRect();

        // コンテナ内の相対位置を計算
        const currentTop = toolbarRect.top - containerRect.top + container.scrollTop;
        const currentLeft = toolbarRect.left - containerRect.left + container.scrollLeft;

        // absolute位置を設定
        toolbar.style.top = `${currentTop}px`;
        toolbar.style.left = `${currentLeft}px`;
        toolbar.style.bottom = 'auto';
        toolbar.style.right = 'auto';

        // クリック位置のツールバー内でのオフセットを計算
        const offsetX = e.clientX - toolbarRect.left;
        const offsetY = e.clientY - toolbarRect.top;

        this.dragState = {
            toolbar: toolbar,
            container: container,
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
        const containerRect = state.container.getBoundingClientRect();

        // マウスの位置からコンテナの位置とオフセットを引いた相対位置を計算
        const newLeft = e.clientX - containerRect.left - state.offsetX + state.container.scrollLeft;
        const newTop = e.clientY - containerRect.top - state.offsetY + state.container.scrollTop;

        // ツールバーの位置を更新
        state.toolbar.style.left = `${newLeft}px`;
        state.toolbar.style.top = `${newTop}px`;
        state.toolbar.style.right = 'auto';
        state.toolbar.style.bottom = 'auto';
    },

    onMouseUp: () => {
        if (!window.ganttZoom.dragState) return;

        document.removeEventListener('mousemove', window.ganttZoom.onMouseMove);
        document.removeEventListener('mouseup', window.ganttZoom.onMouseUp);

        window.ganttZoom.dragState = null;
    },

    dispose: function() {
        document.removeEventListener('mousemove', this.onMouseMove);
        document.removeEventListener('mouseup', this.onMouseUp);
        this.dragState = null;
    }
};
