const API_BASE_URL = "https://localhost:7167";

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

function setToken(token) {
    localStorage.setItem('jwtToken', token);
    const decoded = parseJwt(token);
    if (decoded) {
        const roleClaim = decoded['role'] || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
        if (roleClaim) {
            localStorage.setItem('userRole', roleClaim);
        }
    }
}

function getToken() {
    return localStorage.getItem('jwtToken');
}

function removeToken() {
    localStorage.removeItem('jwtToken');
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

function logout() {
    removeToken();
    window.location.href = '/Auth/Login';
}

const apiClient = {
    request: function (method, endpoint, data = null) {
        return new Promise((resolve, reject) => {
            const ajaxOptions = {
                url: API_BASE_URL + endpoint,
                type: method,
                contentType: "application/json",
                success: function (response) {
                    resolve(response);
                },
                error: function (xhr, status, error) {
                    reject({
                        xhr: xhr,
                        status: status,
                        error: error,
                        message: xhr.responseJSON?.message || "Đã có lỗi xảy ra từ máy chủ."
                    });
                }
            };

            if (data && (method === 'POST' || method === 'PUT' || method === 'PATCH')) {
                ajaxOptions.data = JSON.stringify(data);
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
