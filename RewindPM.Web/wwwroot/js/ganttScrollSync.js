// ガントチャートのスクロール同期とリサイズ機能
window.ganttScrollSync = {
    // 状態管理
    timelineScroll: null,
    wheelHandler: null,
    dotNetRef: null,
    resizeState: null,

    /**
     * ガントチャートのリサイズハンドル機能を初期化
     * @param {DotNetObjectReference} dotNetReference - .NETオブジェクトへの参照
     */
    initialize: function (dotNetReference) {
        this.dispose();
        this.dotNetRef = dotNetReference;
        this.initializeResizeHandles();
        this.initializeRowHighlight();
    },

    initializeResizeHandles: function () {
        const ganttBars = document.querySelectorAll('.gantt-bar');

        ganttBars.forEach((bar) => {
            const leftHandle = bar.querySelector('.gantt-resize-handle-left');
            const rightHandle = bar.querySelector('.gantt-resize-handle-right');

            // イベントリスナーの重複登録を防ぐため、data属性でチェック
            // Blazorの再レンダリング時は新しいDOM要素が作成されるため、data属性もリセットされる
            if (leftHandle && !leftHandle.dataset.listenerAttached) {
                leftHandle.addEventListener('mousedown', (e) => this.startResize(e, bar, 'left'));
                leftHandle.dataset.listenerAttached = 'true';
            }
            if (rightHandle && !rightHandle.dataset.listenerAttached) {
                rightHandle.addEventListener('mousedown', (e) => this.startResize(e, bar, 'right'));
                rightHandle.dataset.listenerAttached = 'true';
            }
        });
    },

    reinitializeResizeHandles: function () {
        // 再レンダリング後にリサイズハンドルと行ハイライトを再初期化
        // ブラウザのレンダリングサイクルが完了するまで待機
        requestAnimationFrame(() => {
            this.initializeResizeHandles();
            this.initializeRowHighlight();
        });
    },

    initializeRowHighlight: function () {
        const taskRows = document.querySelectorAll('.gantt-task-row');

        taskRows.forEach((row) => {
            const taskNameCell = row.querySelector('.gantt-task-name-cell');
            const timelineArea = row.querySelector('.gantt-timeline-area');

            if (taskNameCell && timelineArea) {
                // マウスオーバー時にタイムラインエリアをハイライト
                taskNameCell.addEventListener('mouseenter', () => {
                    timelineArea.classList.add('highlighted');
                });

                // マウスアウト時にハイライトを解除
                taskNameCell.addEventListener('mouseleave', () => {
                    timelineArea.classList.remove('highlighted');
                });
            }
        });
    },

    startResize: function (e, bar, side) {
        e.preventDefault();
        e.stopPropagation();

        const taskId = bar.getAttribute('data-task-id');
        const barType = bar.getAttribute('data-bar-type'); // 'scheduled' or 'actual'
        const grid = bar.parentElement;
        const gridRect = grid.getBoundingClientRect();

        // グリッドの列幅を計算
        const gridStyle = window.getComputedStyle(grid);
        const gridTemplateColumns = gridStyle.gridTemplateColumns;
        const columnWidths = gridTemplateColumns.split(' ').map(w => parseFloat(w));
        const columnWidth = columnWidths[0]; // すべての列が同じ幅と仮定

        // 現在のgrid-columnを取得
        const gridColumnStyle = window.getComputedStyle(bar).gridColumn;
        const [startCol, endCol] = gridColumnStyle.split(' / ').map(n => parseInt(n));

        this.resizeState = {
            taskId,
            barType,
            side,
            grid,
            bar,
            startCol,
            endCol,
            columnWidth,
            gridLeft: gridRect.left,
            startX: e.clientX
        };

        document.addEventListener('mousemove', this.onMouseMove);
        document.addEventListener('mouseup', this.onMouseUp);

        // カーソルを変更
        document.body.style.cursor = 'ew-resize';
        document.body.style.userSelect = 'none';
    },

    onMouseMove: (e) => {
        if (!window.ganttScrollSync.resizeState) return;

        const state = window.ganttScrollSync.resizeState;
        const deltaX = e.clientX - state.startX;
        const columnsDelta = Math.round(deltaX / state.columnWidth);

        let newStartCol = state.startCol;
        let newEndCol = state.endCol;

        if (state.side === 'left') {
            // 左ハンドル（開始日）をドラッグ
            newStartCol = state.startCol + columnsDelta;
            // 開始日は必ず終了日より前でなければならない（最小幅を1日とする）
            // また、1列目より前には移動できない
            newStartCol = Math.max(1, Math.min(newStartCol, state.endCol - 1));
        } else {
            // 右ハンドル（終了日）をドラッグ
            newEndCol = state.endCol + columnsDelta;
            // 終了日は必ず開始日より後でなければならない（最小幅を1日とする）
            newEndCol = Math.max(state.startCol + 1, newEndCol);
        }

        // grid-columnを更新
        state.bar.style.gridColumn = `${newStartCol} / ${newEndCol}`;
    },

    onMouseUp: async () => {
        if (!window.ganttScrollSync.resizeState) return;

        const state = window.ganttScrollSync.resizeState;

        // 新しいgrid-columnを取得
        const gridColumnStyle = window.getComputedStyle(state.bar).gridColumn;
        const [newStartCol, newEndCol] = gridColumnStyle.split(' / ').map(n => parseInt(n));

        // .NETに変更を通知
        if (window.ganttScrollSync.dotNetRef) {
            try {
                await window.ganttScrollSync.dotNetRef.invokeMethodAsync(
                    'OnBarResized',
                    state.taskId,
                    state.barType,
                    newStartCol - 1, // 0-indexedに変換
                    newEndCol - 2    // 0-indexedに変換（終了日は含む）
                );
            } catch (error) {
                console.error('Error calling OnBarResized:', error);
            }
        }

        // クリーンアップ
        document.removeEventListener('mousemove', window.ganttScrollSync.onMouseMove);
        document.removeEventListener('mouseup', window.ganttScrollSync.onMouseUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        window.ganttScrollSync.resizeState = null;
    },

    dispose: function () {
        // イベントリスナーを削除
        if (this.timelineScroll && this.wheelHandler) {
            this.timelineScroll.removeEventListener('wheel', this.wheelHandler);
        }

        // リサイズ状態をクリア
        if (this.resizeState) {
            document.removeEventListener('mousemove', this.onMouseMove);
            document.removeEventListener('mouseup', this.onMouseUp);
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
            this.resizeState = null;
        }

        this.timelineScroll = null;
        this.wheelHandler = null;
        this.dotNetRef = null;
    }
};
