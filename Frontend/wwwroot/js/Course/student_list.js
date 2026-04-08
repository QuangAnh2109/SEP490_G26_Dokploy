let classId = null;
let classNameFromServer = null;

function initBreadcrumb(name) {
    setBreadcrumb([
        { text: "Khóa học", url: "/Course/CourseList" },
        { text: name, url: "/Course/ExamListInCourse/" + classId + (name ? "?className=" + encodeURIComponent(name) : "") },
        { text: "Danh sách học sinh", url: null }
    ]);
}

let currentClassStatus = 1;

async function ensureClassNameAndBreadcrumb() {
    const token = getToken();
    if (!token) return;
    try {
        const res = await fetch(`${API_BASE_URL}/api/Course/${classId}/settings`, { headers: { "Authorization": "Bearer " + token } });
        if (res.ok) {
            const data = await res.json();
            currentClassStatus = data.status ?? 1;
            if (!classNameFromServer) initBreadcrumb(data.className || "Lớp " + classId);
            else initBreadcrumb(classNameFromServer);
        } else {
            if (!classNameFromServer) initBreadcrumb("Lớp " + classId);
            else initBreadcrumb(classNameFromServer);
        }
    } catch {
        if (!classNameFromServer) initBreadcrumb("Lớp " + classId);
        else initBreadcrumb(classNameFromServer);
    }
}

async function loadStudents() {
    const token = getToken();

    if (!token) {
        showToast("Bạn chưa đăng nhập", "error");
        window.location.href = "/Auth/Login";
        return;
    }

    try {
        const role = getUserRole();
        if (role === "Student") {
            const settingsMenu = document.getElementById("settingsMenuItem");
            if (settingsMenu) settingsMenu.style.display = 'none';
            const pendingMenu = document.getElementById("pendingMenuItem");
            if (pendingMenu) pendingMenu.style.display = 'none';
        }
        if (role === "Teacher" && currentClassStatus !== 0) {
            document.querySelectorAll('.action-col').forEach(el => el.style.display = '');
        }

        const response = await fetch(`${API_BASE_URL}/api/Course/${classId}/students`, {
            headers: { "Authorization": "Bearer " + token }
        });

        if (response.status === 401) {
            showToast("Phiên đăng nhập hết hạn", "error");
            removeToken();
            window.location.href = "/Auth/Login";
            return;
        }

        if (!response.ok) {
            throw new Error("Không thể tải danh sách học sinh");
        }

        const students = await response.json();
        renderStudents(students);
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

function formatDateTime(dateString) {
    if (!dateString) return "-";
    return new Date(dateString).toLocaleString("vi-VN");
}

function renderStudents(students) {
    const tbody = document.getElementById("studentTableBody");
    tbody.innerHTML = "";

    if (!students || students.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 6;
        td.className = "text-center";
        td.textContent = "Chưa có học sinh nào tham gia lớp này.";
        tr.appendChild(td);
        tbody.appendChild(tr);
        return;
    }

    const templateStr = document.getElementById("student-row-template");
    const role = getUserRole();

    students.forEach((student, index) => {
        if (templateStr) {
            let tr = templateStr.content.cloneNode(true).querySelector("tr");
            tr.querySelector(".student-index").textContent = index + 1;
            tr.querySelector(".student-code").textContent = student.studentCode || '-';
            tr.querySelector(".student-name").textContent = student.fullName || '-';
            tr.querySelector(".student-email").textContent = student.email || '-';
            tr.querySelector(".student-date").textContent = formatDateTime(student.joinedAtUtc);

            const actionCol = tr.querySelector(".action-col");
            const removeBtn = tr.querySelector(".btn-remove-student");

            if (role === 'Teacher' && currentClassStatus !== 0) {
                if (actionCol) actionCol.style.display = '';
                if (removeBtn) {
                    removeBtn.addEventListener("click", () => removeStudent(student.studentId, student.fullName || student.email));
                }
            } else {
                if (actionCol) actionCol.style.display = 'none';
            }

            tbody.appendChild(tr);
        }
    });
}

async function removeStudent(studentId, studentName) {
    showConfirm(
        `Bạn có chắc muốn xóa học sinh "${studentName}" khỏi lớp không?`,
        "Xác nhận xóa học sinh",
        async () => {
            const token = getToken();
            if (!token) return;

            try {
                const res = await fetch(`${API_BASE_URL}/api/Course/${classId}/students/${studentId}/remove`, {
                    method: "DELETE",
                    headers: { "Authorization": "Bearer " + token }
                });

                if (res.ok) {
                    showToast("Đã xóa học sinh khỏi lớp.", "success");
                    loadStudents();
                } else {
                    const data = await res.json().catch(() => null);
                    showToast(data?.message || "Không thể xóa học sinh.", "error");
                }
            } catch (err) {
                console.error(err);
                showToast("Có lỗi xảy ra khi xóa học sinh.", "error");
            }
        }
    );
}

document.addEventListener("DOMContentLoaded", async function () {
    const dataEl = document.getElementById("courseData");
    classId = dataEl ? dataEl.dataset.classId : null;
    classNameFromServer = dataEl ? dataEl.dataset.className : null;

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

    // Set sidebar class name
    const sidebarName = document.getElementById('sidebarClassName');
    if (sidebarName && classNameFromServer) {
        sidebarName.textContent = classNameFromServer;
    }

    await ensureClassNameAndBreadcrumb();
    loadStudents();
});
