// ガントチャートのスクロール同期
window.ganttScrollSync = {
    timelineScroll: null,
    wheelHandler: null,

    initialize: function (containerRef, timelineScrollRef, timelineRefs) {
        this.dispose();

        this.timelineScroll = timelineScrollRef;

        // マウスホイールでの横スクロール対応
        this.wheelHandler = (e) => {
            // Shiftキーを押しながらホイール、または横スクロール可能な場合
            if (e.shiftKey || Math.abs(e.deltaX) > 0) {
                e.preventDefault();

                // Shiftキー + ホイール: 縦スクロールを横スクロールに変換
                if (e.shiftKey && Math.abs(e.deltaY) > 0) {
                    this.timelineScroll.scrollLeft += e.deltaY;
                } else {
                    // 通常の横スクロール
                    this.timelineScroll.scrollLeft += e.deltaX;
                }
            }
        };

        // タイムラインスクロールにイベントリスナーを追加
        if (this.timelineScroll) {
            this.timelineScroll.addEventListener('wheel', this.wheelHandler, { passive: false });
        }
    },

    dispose: function () {
        // イベントリスナーを削除
        if (this.timelineScroll && this.wheelHandler) {
            this.timelineScroll.removeEventListener('wheel', this.wheelHandler);
        }

        this.timelineScroll = null;
        this.wheelHandler = null;
    }
};
