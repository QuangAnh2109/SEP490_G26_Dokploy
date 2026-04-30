const API_BASE_URL = window.API_BASE_URL;

try {
    if (typeof window.$ !== 'undefined' && window.$.ajaxSetup) {
        window.$.ajaxSetup({
            beforeSend: function (xhr) {
                const token = localStorage.getItem('jwtToken');
                if (token) {
                    xhr.setRequestHeader('Authorization', 'Bearer ' + token);
                }
            }
        });
    }
} catch (_) { }

// Mirror Backend/Common/Roles.cs: BE issue JWT role claim dạng numeric ("1" = Teacher, "2" = Student),
// và Authorize attribute dùng RoleIds.Teacher/Student. FE compare trực tiếp với 2 const này.
const RoleIds = Object.freeze({
    Teacher: '1',
    Student: '2'
});

function setToken(token, refreshToken) {
    localStorage.setItem('jwtToken', token);
    if (refreshToken) {
        localStorage.setItem('refreshToken', refreshToken);
    }
}

function getToken() {
    return localStorage.getItem('jwtToken');
}

function getRefreshToken() {
    return localStorage.getItem('refreshToken');
}

function removeToken() {
    localStorage.removeItem('jwtToken');
    localStorage.removeItem('refreshToken');
}

function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        return JSON.parse(jsonPayload);
    } catch (e) {
        return null;
    }
}

function getUserIdFromToken() {
    const token = getToken();
    if (!token) return null;

    const decoded = parseJwt(token);
    if (!decoded) return null;

    const userId = decoded['sub']
        || decoded['nameid']
        || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
        || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier'];
    if (!userId) return null;

    return parseInt(userId, 10) || null;
}

function getUserRole() {
    const token = getToken();
    if (!token) return null;

    const decoded = parseJwt(token);
    if (!decoded) return null;
    return decoded['role'] || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || null;
}

function getUserEmail() {
    const token = getToken();
    if (!token) return null;

    const decoded = parseJwt(token);
    if (!decoded) return null;

    return decoded['email']
        || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];
}

// Xác định tài khoản đăng nhập bằng Google hay mật khẩu thường từ JWT
function getAuthProvider() {
    const token = getToken();
    if (!token) return null;

    const decoded = parseJwt(token);
    if (!decoded) return null;

    return decoded['auth_provider'] || null;
}

function isGoogleUser() {
    const provider = getAuthProvider();
    return provider === 'google';
}

function isAuthenticated() {
    return getToken() !== null;
}

// Logout: gọi BE để bump SecurityStamp (vô hiệu hóa mọi refresh token đã issue),
// rồi clear local state. Best-effort — nếu BE call fail, vẫn clear + redirect.
function logout() {
    apiClient.post('/api/auth/logout', {})
        .catch(function () { /* ignore — vẫn clear + redirect */ })
        .finally(function () {
            removeToken();
            window.location.href = '/Auth/Login';
        });
}

// Dedup parallel refresh: nhiều API call song song hết hạn cùng lúc → chỉ gọi /refresh-token 1 lần.
let _refreshPromise = null;
function refreshAccessToken() {
    if (_refreshPromise) return _refreshPromise;

    const refreshToken = getRefreshToken();
    if (!refreshToken) return Promise.reject(new Error('No refresh token'));

    const oldAccessToken = getToken() || '';
    const url = API_BASE_URL.replace(/\/+$/, '') + '/api/auth/refresh-token';

    _refreshPromise = new Promise(function (resolve, reject) {
        $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ AccessToken: oldAccessToken, RefreshToken: refreshToken }),
            success: function (response) {
                // BE TokenModel: { accessToken, refreshToken }
                if (response && response.accessToken) {
                    setToken(response.accessToken, response.refreshToken);
                    resolve(response.accessToken);
                } else {
                    reject(new Error('Invalid refresh response'));
                }
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
                success: function (response) {
                    resolve(response);
                },
                error: function (xhr, status, error) {
                    // 401 → thử refresh access token và retry 1 lần.
                    // Skip refresh cho chính endpoint /refresh-token (tránh loop) và khi đã retry rồi.
                    const isRefreshEndpoint = endpoint.indexOf('/api/auth/refresh-token') !== -1;
                    if (xhr.status === 401 && !isRetry && !isRefreshEndpoint && getRefreshToken()) {
                        refreshAccessToken()
                            .then(function () {
                                self.request(method, endpoint, data, true).then(resolve, reject);
                            })
                            .catch(function () {
                                // Refresh fail → access + refresh đều invalid → force logout.
                                removeToken();
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
