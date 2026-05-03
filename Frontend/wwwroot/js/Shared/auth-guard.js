// Phân quyền route phía FE. Chạy IIFE sau khi site.js load → dùng lại window.userReady + getUserRole.
// Replace cho các inline guard cũ ở _Layout/_LayoutAdmin/Auth-login/Admin-login.
(function () {
    'use strict';

    const ROLE_HOME = {
        '1': '/Class/ClassList', // Teacher
        '2': '/Class/ClassList', // Student
        '3': '/Admin/Users'        // Admin
    };

    // Pages chỉ cho khách (đã login → bounce về role home).
    // "/" và "/home/*" là landing/giới thiệu — guest only.
    const ANON_ONLY = [
        '/home',
        '/auth/login',
        '/auth/forgotpassword',
        '/auth/resetpassword',
        '/admin/login'
    ];

    function isAnonOnly(path) {
        if (path === '/' || path === '') return true;
        return ANON_ONLY.some(p => path.startsWith(p));
    }

    // Pages giới hạn theo role. Path không match rule nào và không thuộc ANON_ONLY → không enforce ở FE (BE sẽ chặn).
    const ROUTE_POLICY = [
        { prefix: '/admin',         allow: ['3'] },
        { prefix: '/question',      allow: ['1'] },
        { prefix: '/examblueprint', allow: ['1'] },
        { prefix: '/analytics',     allow: ['1'] },
        { prefix: '/exam/',         allow: ['1'] },
        { prefix: '/practiceexam',  allow: ['2'] },
        { prefix: '/studentexam',   allow: ['2'] },
        { prefix: '/class',        allow: ['1', '2'] },
        { prefix: '/profile',       allow: ['1', '2'] }
    ];

    function decideRedirect(path, role, authed) {
        // (1) Đã login mà vào page khách → về role home
        if (authed && isAnonOnly(path)) {
            return ROLE_HOME[role] || '/Auth/Login';
        }
        // (2) Anon ở page khách → không redirect
        if (isAnonOnly(path)) return null;
        // (3) Page có rule role
        const rule = ROUTE_POLICY.find(r => path.startsWith(r.prefix));
        if (!rule) return null;
        if (!authed) return '/Auth/Login';
        if (!rule.allow.includes(role)) return ROLE_HOME[role] || '/Auth/Login';
        return null;
    }

    function reveal() {
        document.documentElement.classList.remove('auth-pending');
    }

    async function enforce() {
        if (!window.userReady) { reveal(); return; }
        await window.userReady;

        const path = window.location.pathname.toLowerCase();
        const role = (typeof window.getUserRole === 'function') ? window.getUserRole() : null;
        const authed = role !== null && role !== undefined;

        const redirectTo = decideRedirect(path, role, authed);
        if (redirectTo) {
            // Không reveal — page đang navigate đi.
            window.location.replace(redirectTo);
            return;
        }
        reveal();
    }

    // Failsafe: nếu /me hang quá lâu hoặc enforce throw, vẫn show page (tránh body stuck hidden).
    const failsafe = setTimeout(reveal, 3000);
    enforce()
        .catch(function (e) { console.error('auth-guard error', e); reveal(); })
        .finally(function () { clearTimeout(failsafe); });
})();
