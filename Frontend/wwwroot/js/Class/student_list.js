let classId = null;
let classNameFromServer = null;

function initBreadcrumb(name) {
    setBreadcrumb([
        { text: "Lớp học", url: "/Class/ClassList" },
        { text: name, url: "/Class/ExamListInClass/" + classId + (name ? "?className=" + encodeURIComponent(name) : "") },
        { text: "Danh sách học sinh", url: null }
    ]);
}

let currentClassStatus = 1;

async function ensureClassNameAndBreadcrumb() {
    try {
        const data = await apiClient.get(`/api/class/${classId}/settings`);
        currentClassStatus = data.status ?? 1;
        if (!classNameFromServer) initBreadcrumb(data.className || "Lớp " + classId);
        else initBreadcrumb(classNameFromServer);
    } catch {
        if (!classNameFromServer) initBreadcrumb("Lớp " + classId);
        else initBreadcrumb(classNameFromServer);
    }
}

async function loadStudents() {
    try {
        const role = getUserRole();
        if (role === RoleIds.Student) {
            const settingsMenu = document.getElementById("settingsMenuItem");
            if (settingsMenu) settingsMenu.style.display = 'none';
            const pendingMenu = document.getElementById("pendingMenuItem");
            if (pendingMenu) pendingMenu.style.display = 'none';
            const studentAnalyticsMenu = document.getElementById("studentAnalyticsMenuItem");
            if (studentAnalyticsMenu) studentAnalyticsMenu.classList.remove("d-none");
        }
        if (role === RoleIds.Teacher) {
            const practiceMenu = document.getElementById("practiceMenuItem");
            if (practiceMenu) practiceMenu.style.display = 'none';
            const historyMenu = document.getElementById("practiceHistoryMenuItem");
            if (historyMenu) historyMenu.style.display = 'none';
            const classAnalyticsMenu = document.getElementById("classAnalyticsMenuItem");
            if (classAnalyticsMenu) classAnalyticsMenu.classList.remove("d-none");
        }
        if (role === RoleIds.Teacher && currentClassStatus !== 0) {
            document.querySelectorAll('.action-col').forEach(el => el.style.display = '');
        }

        const students = await apiClient.get(`/api/class/${classId}/students`);
        renderStudents(students);
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

            if (role === RoleIds.Teacher && currentClassStatus !== 0) {
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
            try {
                await apiClient.delete(`/api/class/${classId}/students/${studentId}/remove`);
                showToast("Đã xóa học sinh khỏi lớp.", "success");
                loadStudents();
            } catch (error) {
                console.error(error);
                showToast(error.message || "Không thể xóa học sinh.", "error");
            }
        }
    );
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

    await ensureClassNameAndBreadcrumb();
    loadStudents();
});