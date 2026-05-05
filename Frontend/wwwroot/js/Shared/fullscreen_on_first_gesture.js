// Fullscreen helper cho màn làm bài thi.
// Browser bắt buộc fullscreen request phải gắn với user gesture — không thể auto fire
// ngay khi load trang. Helper này thử request NGAY (sẽ rejected ở hầu hết browser nếu
// chưa có gesture) + arm fallback ở pointerdown/keydown đầu tiên (gesture đầu tiên
// cũng kích fullscreen luôn, học sinh không cần thao tác phụ). Học sinh chủ động Esc
// → KHÔNG tự bật lại để tránh annoying loop.
//
// Usage: window.enterFullscreenForExam() — gọi sau auth-guard pass.

(function () {
    'use strict';

    function requestFs() {
        if (document.fullscreenElement) return;
        const el = document.documentElement;
        const req = el.requestFullscreen
                 || el.webkitRequestFullscreen
                 || el.msRequestFullscreen;
        if (!req) return;
        try {
            const p = req.call(el);
            if (p && typeof p.catch === 'function') p.catch(() => {});
        } catch (_) { /* user denied / unsupported — bỏ qua */ }
    }

    window.enterFullscreenForExam = function () {
        // Thử ngay — nếu browser cho phép (vd activation từ click prev page chưa expire).
        requestFs();

        // Fallback: gesture đầu tiên (chỉ chạy 1 lần). once:true → tự gỡ listener.
        const handler = () => {
            requestFs();
            document.removeEventListener('pointerdown', handler);
            document.removeEventListener('keydown', handler);
        };
        document.addEventListener('pointerdown', handler, { once: true });
        document.addEventListener('keydown', handler, { once: true });
    };
})();
