let currentInviteLink = "";
let classId = null;
let classNameFromServer = null;
let currentClassStatus = 1;

function initBreadcrumb(name) {
    setBreadcrumb([
        { text: "Lớp học", url: "/Class/ClassList" },
        { text: name, url: "/Class/ExamListInClass/" + classId + (name ? "?className=" + encodeURIComponent(name) : "") },
        { text: "Cài đặt", url: null }
    ]);
}

async function loadSettings() {

    const role = getUserRole();
    if (role === RoleIds.Student) {
        showToast("Bạn không có quyền truy cập trang này.", "error");
        window.location.href = `/Class/ExamListInClass/${classId}`;
        return;
    }

    if (role === RoleIds.Teacher) {
        const practiceMenu = document.getElementById("practiceMenuItem");
        if (practiceMenu) practiceMenu.style.display = 'none';
        const historyMenu = document.getElementById("practiceHistoryMenuItem");
        if (historyMenu) historyMenu.style.display = 'none';
    }

    try {
        const data = await apiClient.get(`/api/class/${classId}/settings`);

        // Breadcrumb
        initBreadcrumb(classNameFromServer || data.className || "Lớp " + classId);

        // Binding dữ liệu
        currentClassStatus = data.status ?? 1;
        document.getElementById("classNameInput").value = data.className;
        document.getElementById("subjectInput").value = (data.subjectCode ? data.subjectCode + ' - ' : '') + data.subjectName;
        document.getElementById("semesterInput").value = data.semesterCode ?? "";
        document.getElementById("inviteCodeInput").value = data.invitationCode;

        // Generate link
        const baseUrl = window.location.origin;
        currentInviteLink = `${baseUrl}/Class/Join?code=${data.invitationCode}`;

        const statusSwitch = document.getElementById("invitationStatusSwitch");
        if (statusSwitch) {
            statusSwitch.checked = data.invitationCodeStatus !== 0;
            toggleInviteVisibility();
        }

        applyClassStatusUI();

    } catch (error) {
        // Cập nhật kiểm tra HTTP status code qua đối tượng XHR
        if (error.xhr && error.xhr.status === 401) {
            window.location.href = "/Auth/Login";
        } else {
            console.error(error);
            showToast(error.message || "Lỗi khi tải dữ liệu cài đặt lớp.", "error");
        }
    }
}

document.getElementById("settingsForm").addEventListener("submit", async function (e) {
    e.preventDefault();
    const saveBtn = document.getElementById("saveButton");

    const className = document.getElementById("classNameInput").value;
    const status = document.getElementById("invitationStatusSwitch").checked ? 1 : 0;

    saveBtn.disabled = true;
    saveBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i> Đang lưu...';

    try {
        await apiClient.put(`/api/class/${classId}/settings`, {
            className: className,
            invitationCodeStatus: status
        });

        showToast("Lưu cài đặt thành công!", "success");
    } catch (error) {
        showToast(error.message || "Lỗi khi lưu cài đặt", "error");
    } finally {
        saveBtn.disabled = false;
        saveBtn.innerHTML = '<i class="fas fa-save me-2"></i> Lưu thay đổi';
    }
});

function copyToClipboard(elementId) {
    const el = document.getElementById(elementId);
    el.select();
    el.setSelectionRange(0, 99999);
    document.execCommand("copy");

    showToast("Đã sao chép vào clipboard!", "info");
}

function toggleInviteVisibility() {
    const isChecked = document.getElementById("invitationStatusSwitch").checked;
    const contentArea = document.getElementById("inviteContentArea");
    if (contentArea) {
        contentArea.style.display = isChecked ? "block" : "none";
    }
}

function showInviteDetails() {
    const code = document.getElementById("inviteCodeInput").value;
    if (!code || !currentInviteLink) return;

    document.getElementById("modalCodeInput").value = code;
    document.getElementById("modalLinkInput").value = currentInviteLink;

    // Generate QR Code via API
    const qrImg = document.getElementById("qrCodeImage");
    const qrLoading = document.getElementById("qrLoading");

    qrImg.style.display = "none";
    qrLoading.style.display = "inline-block";

    qrImg.onload = function () {
        qrLoading.style.display = "none";
        qrImg.style.display = "block";
    };

    qrImg.src = `https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${encodeURIComponent(currentInviteLink)}`;

    // Initialize and show modal
    const modalElement = document.getElementById("inviteDetailsModal");
    if (window.bootstrap && window.bootstrap.Modal) {
        const modal = new bootstrap.Modal(modalElement);
        modal.show();
    } else {
        // Fallback config
        const myModal = new bootstrap.Modal(document.getElementById('inviteDetailsModal'));
        myModal.show();
    }
}

async function inviteStudentByEmail() {
    const emailInput = document.getElementById("inviteEmailInput");
    const btn = document.getElementById("btnInviteEmail");
    const email = emailInput.value.trim();

    if (!email) {
        showToast("Vui lòng nhập địa chỉ email.", "warning");
        return;
    }

    btn.disabled = true;
    const originalHtml = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> Đang gửi...';

    try {
        const response = await apiClient.post(`/api/class/${classId}/invite`, { email: email });

        // Nếu API trả về JSON chứa thuộc tính message, ta dùng trực tiếp
        showToast(response.message || "Đã gửi lời mời thành công!", "success");
        emailInput.value = "";
    } catch (error) {
        showToast(error.message || "Lỗi khi gửi lời mời", "error");
    } finally {
        btn.disabled = false;
        btn.innerHTML = originalHtml;
    }
}

function applyClassStatusUI() {
    const isClosed = currentClassStatus === 0;

    // Texts and Toggle Button
    const title = document.getElementById("classStatusTitle");
    const desc = document.getElementById("classStatusDesc");
    const container = document.getElementById("classStatusContainer");
    const btnToggle = document.getElementById("btnToggleClassStatus");

    if (isClosed) {
        title.textContent = "Đã đóng";
        title.classList.remove('text-success');
        title.classList.add('text-danger');
        desc.textContent = "Lớp học đã bị đóng. Chỉ có thể xem thông tin, không thể sửa đổi hay quản lý.";
        container.classList.add('bg-light');

        btnToggle.textContent = "Mở lại lớp học";
        btnToggle.classList.remove('btn-outline-danger');
        btnToggle.classList.add('btn-outline-success');
    } else {
        title.textContent = "Đang hoạt động";
        title.classList.remove('text-danger');
        title.classList.add('text-success');
        desc.textContent = "Học sinh có thể làm bài. Giáo viên có thể chỉnh sửa cài đặt và quản lý lớp.";
        container.classList.remove('bg-light');

        btnToggle.textContent = "Đóng lớp học";
        btnToggle.classList.remove('btn-outline-success');
        btnToggle.classList.add('btn-outline-danger');
    }

    // Disable inputs
    document.getElementById("classNameInput").readOnly = isClosed;
    document.getElementById("invitationStatusSwitch").disabled = isClosed;
    document.getElementById("inviteEmailInput").disabled = isClosed;
    document.getElementById("btnInviteEmail").disabled = isClosed;
    document.getElementById("saveButton").disabled = isClosed;
}

async function toggleClassStatus() {
    const isCurrentlyClosed = currentClassStatus === 0;
    const action = isCurrentlyClosed ? "mở lại" : "đóng";
    const endpoint = isCurrentlyClosed ? "reopen" : "close";

    showConfirm(`Bạn có chắc muốn ${action} lớp học này không?`, "Xác nhận", async () => {
        const btnToggle = document.getElementById("btnToggleClassStatus");
        const originalHtml = btnToggle.innerHTML;
        btnToggle.disabled = true;
        btnToggle.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';

        try {
            const data = await apiClient.post(`/api/class/${classId}/${endpoint}`);
            showToast(data.message || `Đã ${action} lớp học.`, "success");
            currentClassStatus = isCurrentlyClosed ? 1 : 0;
            applyClassStatusUI();
        } catch (error) {
            console.error(error);
            showToast(error.message || `Lỗi khi ${action} lớp học.`, "error");
        } finally {
            btnToggle.disabled = false;
            // Restore HTML if it hasn't been changed by UI function yet (applyClassStatusUI updates the text content, so it might overwrite the innerHTML if not careful, but the logic handles it by calling applyClassStatusUI before unlocking or setting text directly)
        }
    });
}

document.addEventListener("DOMContentLoaded", async function () {
    await window.userReady;
    const dataEl = document.getElementById("classData");
    classId = dataEl ? dataEl.dataset.classId : null;
    classNameFromServer = dataEl ? dataEl.dataset.className : null;

    // Bypass Bootstrap container for full-width layout
    const wrapper = document.querySelector('.class-wrapper');
    if (wrapper) {
        let parent = wrapper.parentElement;
        while (parent && parent.tagName !== 'BODY') {
            if (parent.classList.contains('container')) {
                parent.classList.remove('container');
                parent.classList.add('container-fluid');
                parent.style.padding = '0';
                parent.style.maxWidth = '100%';
            }
            if (parent.tagName === 'MAIN') {
                parent.style.padding = '0';
            }
            parent = parent.parentElement;
        }
    }

    // Set sidebar class name
    const sidebarName = document.getElementById('sidebarClassName');
    if (sidebarName && classNameFromServer) {
        sidebarName.textContent = classNameFromServer;
    }

    if (classNameFromServer) initBreadcrumb(classNameFromServer);
    loadSettings();
});