// Global Configuration
const API_BASE_URL = "https://localhost:7167";

// Setup global Ajax defaults (chỉ chạy khi có jQuery; trang Assign Exam không load jQuery nên bỏ qua)
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
} catch (_) { /* bỏ qua nếu không có jQuery */ }

// Helper functions for auth
function setToken(token) {
    localStorage.setItem('jwtToken', token);
}

function getToken() {
    return localStorage.getItem('jwtToken');
}

function removeToken() {
    localStorage.removeItem('jwtToken');
}

// Helper to decode JWT payload safely
function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        return JSON.parse(jsonPayload);
    } catch (e) {
        return null; // Invalid token
    }
}

// Get the user's ID from the stored JWT token (for teacherId, etc.)
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

    const num = parseInt(userId, 10);
    return Number.isInteger(num) && num > 0 ? num : null;
}

// Get the user's role from the stored JWT token
function getUserRole() {
    const token = getToken();
    if (!token) return null;

    const decoded = parseJwt(token);
    // JWT sometimes maps Role to a complex schema URL, but standard .NET puts it in role or ClaimTypes.Role
    const roleClaim = decoded['role'] || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    return roleClaim;
}

function isAuthenticated() {
    return getToken() !== null;
}

// Handle global logout
function logout() {
    removeToken();
    window.location.href = '/Auth/Login';
}

// Global API Client for the team
const apiClient = {
    /**
     * Thực hiện một AJAX request chung
     * @param {string} method - 'GET', 'POST', 'PUT', 'DELETE'
     * @param {string} endpoint - Route API (ví dụ: '/api/auth/login')
     * @param {object} data - Dữ liệu body (cho POST/PUT)
     * @returns {Promise} Trả về Promise để dùng với .then() .catch() hoặc async/await
     */
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

    get: function (endpoint) {
        return this.request('GET', endpoint);
    },

    post: function (endpoint, data) {
        return this.request('POST', endpoint, data);
    },

    put: function (endpoint, data) {
        return this.request('PUT', endpoint, data);
    },

    delete: function (endpoint) {
        return this.request('DELETE', endpoint);
    }
};
