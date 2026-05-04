let allPendingStudents = [];
let classId = null;
let currentClassStatus = 1;

async function loadPendingStudents() {
    const role = getUserRole();
    if (role === RoleIds.Student) {
        showToast("Bạn không có quyền truy cập", "error");
        window.location.href = `/Class/ExamListInClass/${classId}`;
        return;
    }

    if (role === RoleIds.Teacher) {
        const practiceMenu = document.getElementById("practiceMenuItem");
        if (practiceMenu) practiceMenu.style.display = 'none';
        const historyMenu = document.getElementById("practiceHistoryMenuItem");
        if (historyMenu) historyMenu.style.display = 'none';
        const classAnalyticsMenu = document.getElementById("classAnalyticsMenuItem");
        if (classAnalyticsMenu) classAnalyticsMenu.classList.remove("d-none");
    }
    if (role === RoleIds.Student) {
        const studentAnalyticsMenu = document.getElementById("studentAnalyticsMenuItem");
        if (studentAnalyticsMenu) studentAnalyticsMenu.classList.remove("d-none");
    }

    try {
        // Sử dụng Promise.all để gọi đồng thời 2 API cho tối ưu
        const [settingsData, pendingData] = await Promise.all([
            apiClient.get(`/api/class/${classId}/settings`).catch(() => ({ status: 1 })), // Fallback nếu lỗi cài đặt
            apiClient.get(`/api/class/${classId}/students/pending`)
        ]);

        currentClassStatus = settingsData.status ?? 1;
        allPendingStudents = pendingData;

        renderStudents(allPendingStudents);
    } catch (error) {
        if (error.xhr && error.xhr.status === 401) {
            showToast("Phiên đăng nhập hết hạn", "error");
            window.location.href = "/Auth/Login";
            return;
        }

        console.error(error);
        const tbody = document.getElementById("studentTableBody");
        tbody.innerHTML = "";
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 5;
        td.className = "text-center text-danger";
        td.textContent = "Có lỗi xảy ra khi tải danh sách.";
        tr.appendChild(td);
        tbody.appendChild(tr);
    }
}

function filterStudents() {
    const emailFilter = document.getElementById("searchEmail").value.toLowerCase();
    const idFilter = document.getElementById("searchStudentId").value.toLowerCase();

    const filtered = allPendingStudents.filter(s => {
        const mailMatch = !emailFilter || (s.email && s.email.toLowerCase().includes(emailFilter));
        const idMatch = !idFilter || (s.studentCode && s.studentCode.toLowerCase().includes(idFilter));
        return mailMatch && idMatch;
    });

    renderStudents(filtered);
}

function renderStudents(students) {
    const tbody = document.getElementById("studentTableBody");
    tbody.innerHTML = "";

    if (!students || students.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 5;
        td.className = "text-center";
        td.textContent = "Không có học sinh nào đang chờ duyệt.";
        tr.appendChild(td);
        tbody.appendChild(tr);
        return;
    }

    const templateStr = document.getElementById("pending-row-template");

    students.forEach((student, index) => {
        if (templateStr) {
            let tr = templateStr.content.cloneNode(true).querySelector("tr");
            tr.querySelector(".student-index").textContent = index + 1;
            tr.querySelector(".student-code").textContent = student.studentCode || '-';
            tr.querySelector(".student-name").textContent = student.fullName || '-';
            tr.querySelector(".student-email").textContent = student.email || '-';

            const actionsCol = tr.querySelector("td:last-child");
            if (currentClassStatus === 0 && actionsCol) {
                actionsCol.innerHTML = "<span class='text-muted small'>Lớp đã đóng</span>";
            } else {
                const btnApprove = tr.querySelector(".btn-approve");
                const btnReject = tr.querySelector(".btn-reject");
                // Thay đổi từ string "confirm" sang sử dụng showConfirm để đồng bộ UI
                if (btnApprove) btnApprove.onclick = () => showConfirm("Cho phép học sinh này tham gia lớp?", "Phê duyệt", () => approveStudent(student.studentId));
                if (btnReject) btnReject.onclick = () => showConfirm("Bạn chắc chắn muốn từ chối học sinh này?", "Từ chối", () => rejectStudent(student.studentId));
            }

            tbody.appendChild(tr);
        }
    });
}

async function approveStudent(studentId) {
    try {
        await apiClient.post(`/api/class/${classId}/students/${studentId}/approve`);
        showToast("Đã phê duyệt thành công", "success");
        loadPendingStudents();
    } catch (error) {
        showToast(error.message || "Lỗi khi phê duyệt", "error");
    }
}

async function rejectStudent(studentId) {
    try {
        await apiClient.delete(`/api/class/${classId}/students/${studentId}/reject`);
        showToast("Đã từ chối thành công", "success");
        loadPendingStudents();
    } catch (error) {
        showToast(error.message || "Lỗi khi từ chối", "error");
    }
}

document.addEventListener("DOMContentLoaded", async function () {
    await window.userReady;
    const dataEl = document.getElementById("classData");
    classId = dataEl ? dataEl.dataset.classId : null;

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

    loadPendingStudents();
});