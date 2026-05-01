document.addEventListener("DOMContentLoaded", async function () {
    await window.userReady;

    const role = getUserRole();
    if (role !== RoleIds.Teacher) {
        showToast("Bạn không có quyền truy cập trang này.", "error");
        window.location.href = "/Course/CourseList";
        return;
    }

    loadSubjects();
    initForm();
});

async function loadSubjects() {
    try {
        const subjects = await apiClient.get('/api/course/subjects');
        const select = document.getElementById("subjectSelect");

        select.innerHTML = '';
        const defaultOpt = document.createElement("option");
        defaultOpt.value = "";
        defaultOpt.textContent = "-- Chọn Môn Học --";
        select.appendChild(defaultOpt);

        subjects.forEach(s => {
            const opt = document.createElement("option");
            opt.value = s.subjectId;
            opt.textContent = `${s.code} - ${s.name}`;
            select.appendChild(opt);
        });
    } catch (error) {
        console.error(error);
        showToast(error.message || "Đã có lỗi xảy ra khi tải danh sách môn học.", "error");
    }
}

function initForm() {
    const form = document.getElementById("createClassForm");
    if (!form) return;

    form.addEventListener("submit", async function (e) {
        e.preventDefault();

        const btn = document.getElementById("submitBtn");
        btn.disabled = true;
        btn.textContent = "Đang xử lý...";

        const payload = {
            className: document.getElementById("classNameInput").value.trim(),
            semester: document.getElementById("semesterInput").value.trim(),
            subjectId: parseInt(document.getElementById("subjectSelect").value)
        };

        try {
            const data = await apiClient.post('/api/course', payload);
            showSuccessModal(data.invitationCode);
        } catch (error) {
            console.error(error);
            showToast(error.message || "Có lỗi xảy ra khi gọi API Tạo lớp.", "error");
        } finally {
            btn.disabled = false;
            btn.textContent = "Tạo Lớp Học";
        }
    });
}

function showSuccessModal(inviteCode) {
    document.getElementById("inviteCodeDisplay").textContent = inviteCode;

    const joinLink = `${window.location.origin}/Course/Join?code=${inviteCode}`;
    const linkInput = document.getElementById("inviteLinkInput");
    linkInput.value = joinLink;

    document.getElementById("copyLinkBtn").addEventListener("click", () => {
        navigator.clipboard.writeText(joinLink).then(() => {
            showToast("Đã chép link mời vào bộ nhớ tạm!", "info");
        });
    });

    const modal = new bootstrap.Modal(document.getElementById('successModal'));
    modal.show();
}