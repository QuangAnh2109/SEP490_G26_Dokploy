const API_BASE_URL = window.API_BASE_URL;

// Mirror Backend/Common/Roles.cs: BE issue JWT role claim dạng numeric ("1"=Teacher, "2"=Student, "3"=Admin),
// và Authorize attribute dùng RoleIds.Teacher/Student/Admin. FE compare trực tiếp với các const này.
const RoleIds = Object.freeze({
    Teacher: '1',
    Student: '2',
    Admin: '3'
});

// Bootstrap qua /me. Nếu 401 + có refresh cookie → tự refresh + retry /me, tránh logout oan khi reload sau access expiry.
// Nếu /me trả 403 + code AUTH_PASSWORD_CHANGE_REQUIRED → redirect trang đổi password lần đầu.
const PASSWORD_CHANGE_PATH = '/Auth/ChangePasswordFirstLogin';
window.currentUser = null;
window.userReady = (function () {
    const meUrl = API_BASE_URL.replace(/\/+$/, '') + '/api/auth/me';
    const refreshUrl = API_BASE_URL.replace(/\/+$/, '') + '/api/auth/refresh-token';

    function fetchMe() {
        return new Promise(function (resolve, reject) {
            $.ajax({
                url: meUrl, type: 'GET',
                xhrFields: { withCredentials: true },
                success: function (resp) { resolve(resp || null); },
                error: function (xhr) { reject({ status: xhr.status || 0, code: xhr.responseJSON?.code }); }
            });
        });
    }

    function tryRefresh() {
        return new Promise(function (resolve) {
            $.ajax({
                url: refreshUrl, type: 'POST',
                xhrFields: { withCredentials: true },
                success: function () { resolve(true); },
                error: function () { resolve(false); }
            });
        });
    }

    function redirectToPasswordChange() {
        window.currentUser = null;
        if (window.location.pathname !== PASSWORD_CHANGE_PATH) {
            window.location.href = PASSWORD_CHANGE_PATH;
        }
    }

    return (async function () {
        try {
            window.currentUser = await fetchMe();
            return window.currentUser;
        } catch (err) {
            if (err && err.status === 403 && err.code === 'AUTH_PASSWORD_CHANGE_REQUIRED') {
                redirectToPasswordChange();
                return null;
            }
            if (err && err.status === 401 && await tryRefresh()) {
                try {
                    window.currentUser = await fetchMe();
                    return window.currentUser;
                } catch (err2) {
                    if (err2 && err2.status === 403 && err2.code === 'AUTH_PASSWORD_CHANGE_REQUIRED') {
                        redirectToPasswordChange();
                        return null;
                    }
                }
            }
            window.currentUser = null;
            return null;
        }
    })();
})();

function getUserIdFromToken() {
    return window.currentUser ? window.currentUser.userId : null;
}

function getUserRole() {
    return window.currentUser ? window.currentUser.role : null;
}

function getUserEmail() {
    return window.currentUser ? window.currentUser.email : null;
}

function getAuthProvider() {
    return window.currentUser ? window.currentUser.authProvider : null;
}

function isGoogleUser() {
    return getAuthProvider() === 'google';
}

function isAuthenticated() {
    return window.currentUser !== null;
}

function logout() {
    apiClient.post('/api/auth/logout', {})
        .catch(function () { /* ignore — vẫn redirect */ })
        .finally(function () {
            window.currentUser = null;
            window.location.href = '/Auth/Login';
        });
}

// Dedup parallel refresh: nhiều API call song song hết hạn cùng lúc → chỉ gọi /refresh-token 1 lần.
let _refreshPromise = null;
function refreshAccessToken() {
    if (_refreshPromise) return _refreshPromise;

    const url = API_BASE_URL.replace(/\/+$/, '') + '/api/auth/refresh-token';

    _refreshPromise = new Promise(function (resolve, reject) {
        $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json',
            xhrFields: { withCredentials: true },
            success: function () {
                resolve(true);
            },
            error: function (xhr) {
                reject(new Error('Refresh failed: ' + xhr.status));
            }
        });
    }).finally(function () {
        _refreshPromise = null;
    });

    return _refreshPromise;
}

const apiClient = {
    request: function (method, endpoint, data = null, isRetry = false) {
        const self = this;
        return new Promise((resolve, reject) => {
            const ajaxOptions = {
                url: API_BASE_URL.replace(/\/+$/, '') + (endpoint.startsWith('/') ? endpoint : '/' + endpoint),
                type: method,
                contentType: "application/json",
                xhrFields: { withCredentials: true },
                success: function (response) {
                    resolve(response);
                },
                error: function (xhr, status, error) {
                    const isRefreshEndpoint = endpoint.indexOf('/api/auth/refresh-token') !== -1;
                    const isMeEndpoint = endpoint.indexOf('/api/auth/me') !== -1;
                    if (xhr.status === 403 && xhr.responseJSON?.code === 'AUTH_PASSWORD_CHANGE_REQUIRED') {
                        window.currentUser = null;
                        if (window.location.pathname !== PASSWORD_CHANGE_PATH) {
                            window.location.href = PASSWORD_CHANGE_PATH;
                        }
                        reject({ xhr: xhr, status: status, error: error, message: 'Yêu cầu đổi mật khẩu trước khi tiếp tục.' });
                        return;
                    }
                    if (xhr.status === 401 && !isRetry && !isRefreshEndpoint && !isMeEndpoint) {
                        refreshAccessToken()
                            .then(function () {
                                self.request(method, endpoint, data, true).then(resolve, reject);
                            })
                            .catch(function () {
                                window.currentUser = null;
                                if (window.location.pathname.indexOf('/Auth/') !== 0) {
                                    window.location.href = '/Auth/Login';
                                }
                                reject({
                                    xhr: xhr, status: status, error: error,
                                    message: 'Phiên đăng nhập đã hết hạn.'
                                });
                            });
                        return;
                    }
                    reject({
                        xhr: xhr,
                        status: status,
                        error: error,
                        message: xhr.responseJSON?.message || "Đã có lỗi xảy ra từ máy chủ."
                    });
                }
            };

            if (data && (method === 'POST' || method === 'PUT' || method === 'PATCH')) {
                ajaxOptions.data = JSON.stringify(data || {});
            }

            $.ajax(ajaxOptions);
        });
    },

    get: function (endpoint) { return this.request('GET', endpoint); },
    post: function (endpoint, data) { return this.request('POST', endpoint, data); },
    put: function (endpoint, data) { return this.request('PUT', endpoint, data); },
    delete: function (endpoint) { return this.request('DELETE', endpoint); },
    patch: function (endpoint, data) { return this.request('PATCH', endpoint, data); }
};

function showToast(message, type = 'success', duration = 3000) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const t = document.getElementById('toastTemplate');
    if (!t) return;

    const toastEl = t.content.cloneNode(true).firstElementChild;
    const bgClass = type === 'success' ? 'bg-success' : (type === 'error' ? 'bg-danger' : 'bg-info');
    toastEl.classList.add(bgClass);

    const body = toastEl.querySelector('[data-message]');
    if (body) body.textContent = message;

    container.appendChild(toastEl);

    const bsToast = new bootstrap.Toast(toastEl, { delay: duration });
    bsToast.show();

    toastEl.addEventListener('hidden.bs.toast', () => {
        toastEl.remove();
    });
}

/**
 * Set breadcrumb in header (Classroom-style: Khóa học > [Tên lớp] > Danh sách đề)
 * @param {Array<{text: string, url?: string|null}>} items - Each item: text, url (null/undefined = current, no link)
 */
function setBreadcrumb(items) {
    const container = document.getElementById('header-breadcrumb');
    if (!container || !Array.isArray(items) || items.length === 0) {
        if (container) container.innerHTML = '';
        return;
    }

    const parts = [];
    items.forEach((item, i) => {
        const text = item.text || '';
        const url = item.url;

        if (i > 0) {
            parts.push('<span class="breadcrumb-sep">›</span>');
        }

        if (url) {
            parts.push(`<a href="${encodeURI(url)}">${escapeHtml(text)}</a>`);
        } else {
            parts.push(`<span class="breadcrumb-current">${escapeHtml(text)}</span>`);
        }
    });

    container.innerHTML = parts.join('');
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showConfirm(message, title = 'Xác nhận', onConfirm) {
    const modalEl = document.getElementById('globalConfirmModal');
    if (!modalEl) return;

    const titleEl = document.getElementById('globalConfirmTitle');
    const msgEl = document.getElementById('globalConfirmMessage');
    const confirmBtn = document.getElementById('globalConfirmBtn');

    if (titleEl) titleEl.textContent = title;
    if (msgEl) msgEl.textContent = message;

    const modal = new bootstrap.Modal(modalEl);

    const newConfirmBtn = confirmBtn.cloneNode(true);
    confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);

    newConfirmBtn.addEventListener('click', () => {
        if (onConfirm) onConfirm();
        modal.hide();
    });

    modal.show();
}
