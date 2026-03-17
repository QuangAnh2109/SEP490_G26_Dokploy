// ===== INIT GLOBAL =====
document.addEventListener("DOMContentLoaded", function () {
    initAuthUI();
});

// ===== AUTH =====
function initAuthUI() {
    const isAuth = isAuthenticated();
    const role = getUserRole();

    document.querySelectorAll(".auth-guest")
        .forEach(e => e.classList.toggle("d-none", isAuth));

    document.querySelectorAll(".auth-user")
        .forEach(e => e.classList.toggle("d-none", !isAuth));

    document.querySelectorAll(".role-dependent")
        .forEach(e => e.classList.add("d-none"));

    if (role === "Teacher") {
        document.querySelectorAll(".role-teacher")
            .forEach(e => e.classList.remove("d-none"));
    }

    if (isGoogleUser()) {
        const el = document.querySelector('a[href="/Auth/ChangePassword"]');
        if (el) el.style.display = "none";
    }
}

// ===== SERVICE =====
const CourseService = (function () {

    function handle401(err) {
        if (err?.xhr?.status === 401) {
            removeToken();
            window.location.href = "/Auth/Login";
            return true;
        }
        return false;
    }

    async function getDetail(id) {
        try {
            return await apiClient.get(`/api/Course/${id}`);
        } catch (e) {
            if (handle401(e)) return null;
            throw e;
        }
    }

    async function getExams(id) {
        try {
            return await apiClient.get(`/api/Course/${id}/exams`);
        } catch (e) {
            if (handle401(e)) return null;
            throw e;
        }
    }

    async function getStudents(id) {
        try {
            return await apiClient.get(`/api/Course/${id}/students`);
        } catch (e) {
            if (handle401(e)) return null;
            throw e;
        }
    }

    return { getDetail, getExams, getStudents };
})();

// ===== UI =====
const CourseUI = (function () {

    function setClassName(name) {
        const el = document.getElementById("className");
        if (el) el.textContent = name || "Không có tên";
    }

    function hideSettingsIfStudent() {
        if (getUserRole() === "Student") {
            const el = document.getElementById("settingsMenuItem");
            if (el) el.style.display = "none";
        }
    }

    function formatDate(d) {
        if (!d) return "-";
        return new Date(d).toLocaleString("vi-VN");
    }

    function getStatus(open, close) {
        const now = new Date();
        const o = new Date(open);
        const c = new Date(close);

        if (now > c) return "Đóng";
        if (now >= o) return "Mở";
        return "Sắp diễn ra";
    }

    function renderExams(exams) {
        const tbody = document.getElementById("examTableBody");
        if (!tbody) return;

        tbody.innerHTML = "";

        if (!exams || exams.length === 0) {
            tbody.innerHTML = `<tr><td colspan="4">Không có đề</td></tr>`;
            return;
        }

        exams.forEach(e => {
            const row = document.createElement("tr");

            row.innerHTML = `
                <td>${e.title}</td>
                <td>${formatDate(e.openAt)} - ${formatDate(e.closeAt)}</td>
                <td>${e.durationMinutes ?? "-"}</td>
                <td>${getStatus(e.openAt, e.closeAt)}</td>
            `;

            tbody.appendChild(row);
        });
    }

    function renderStudents(students) {
        const tbody = document.getElementById("studentTableBody");
        if (!tbody) return;

        const isStudent = getUserRole() === "Student";

        tbody.innerHTML = "";

        students.forEach((s, i) => {
            const row = document.createElement("tr");

            row.innerHTML = `
                <td>${i + 1}</td>
                <td>${s.studentCode}</td>
                <td>${s.fullName}</td>
                <td>${s.email}</td>
                ${!isStudent ? `<td>${formatDate(s.joinedAtUtc)}</td>` : ""}
            `;

            tbody.appendChild(row);
        });
    }

    return {
        setClassName,
        hideSettingsIfStudent,
        renderExams,
        renderStudents
    };
})();

// ===== PAGE =====
const CoursePage = (function () {

    async function initCommon(classId) {
        if (!isAuthenticated()) {
            window.location.href = "/Auth/Login";
            return;
        }

        CourseUI.hideSettingsIfStudent();

        const detail = await CourseService.getDetail(classId);
        if (detail) {
            CourseUI.setClassName(detail.className);
        }
    }

    async function initExamList(classId) {
        await initCommon(classId);

        const exams = await CourseService.getExams(classId);
        CourseUI.renderExams(exams);
    }

    async function initStudentList(classId) {
        await initCommon(classId);

        const students = await CourseService.getStudents(classId);
        CourseUI.renderStudents(students);
    }

    return {
        initExamList,
        initStudentList
    };
})();