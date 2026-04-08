let allPendingStudents = [];
let classId = null;
let currentClassStatus = 1;

async function loadPendingStudents() {
    const token = getToken();
    if (!token) {
        showToast("Bạn chưa đăng nhập", "error");
        window.location.href = "/Auth/Login";
        return;
    }

    const role = getUserRole();
    if (role === "Student") {
        showToast("Bạn không có quyền truy cập", "error");
        window.location.href = `/Course/ExamListInCourse/${classId}`;
        return;
    }

    try {
        const settingsRes = await fetch(`${API_BASE_URL}/api/Course/${classId}/settings`, {
            headers: { "Authorization": "Bearer " + token }
        });
        if (settingsRes.ok) {
            const settingsData = await settingsRes.json();
            currentClassStatus = settingsData.status ?? 1;
        }

        const response = await fetch(`${API_BASE_URL}/api/Course/${classId}/students/pending`, {
            headers: { "Authorization": "Bearer " + token }
        });

        if (response.status === 401) {
            showToast("Phiên đăng nhập hết hạn", "error");
            window.location.href = "/Auth/Login";
            return;
        }

        if (!response.ok) throw new Error("Không thể tải danh sách chờ phê duyệt");

        allPendingStudents = await response.json();
        renderStudents(allPendingStudents);
    } catch (error) {
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
                if (btnApprove) btnApprove.onclick = () => approveStudent(student.studentId);
                if (btnReject) btnReject.onclick = () => rejectStudent(student.studentId);
            }
            
            tbody.appendChild(tr);
        }
    });
}

async function approveStudent(studentId) {
    if (!confirm("Cho phép học sinh này tham gia lớp?")) return;
    const token = getToken();

    try {
        const res = await fetch(`${API_BASE_URL}/api/Course/${classId}/students/${studentId}/approve`, {
            method: 'POST',
            headers: { "Authorization": "Bearer " + token }
        });

        if (res.ok) {
            showToast("Đã phê duyệt thành công", "success");
            loadPendingStudents();
        } else {
            showToast("Lỗi khi phê duyệt", "error");
        }
    } catch (e) {
        showToast("Đã xảy ra lỗi hệ thống", "error");
    }
}

async function rejectStudent(studentId) {
    if (!confirm("Bạn chắc chắn muốn từ chối học sinh này?")) return;
    const token = getToken();

    try {
        const res = await fetch(`${API_BASE_URL}/api/Course/${classId}/students/${studentId}/reject`, {
            method: 'DELETE',
            headers: { "Authorization": "Bearer " + token }
        });

        if (res.ok) {
            showToast("Đã từ chối thành công", "success");
            loadPendingStudents();
        } else {
            showToast("Lỗi khi từ chối", "error");
        }
    } catch (e) {
        showToast("Đã xảy ra lỗi hệ thống", "error");
    }
}

document.addEventListener("DOMContentLoaded", function() {
    const dataEl = document.getElementById("courseData");
    classId = dataEl ? dataEl.dataset.classId : null;

    // Bypass Bootstrap container for full-width layout
    const wrapper = document.querySelector('.course-wrapper');
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
