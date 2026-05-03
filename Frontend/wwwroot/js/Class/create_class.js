document.addEventListener("DOMContentLoaded", async function () {
    await window.userReady;

    const role = getUserRole();
    if (role !== RoleIds.Teacher) {
        showToast("Bạn không có quyền truy cập trang này.", "error");
        window.location.href = "/Class/ClassList";
        return;
    }

    loadSubjects();
    loadSemesters();
    initForm();
});

async function loadSemesters() {
    const select = document.getElementById("semesterSelect");
    const help = document.getElementById("semester-help");
    try {
        const items = await apiClient.get('/api/class/semesters');
        select.innerHTML = '';
        if (items.length === 0) {
            const opt = document.createElement("option");
            opt.value = "";
            opt.disabled = true;
            opt.selected = true;
            opt.textContent = "Chưa có kỳ học nào";
            select.appendChild(opt);
            help.textContent = "Liên hệ quản trị viên để tạo kỳ học.";
            return;
        }
        const placeholder = document.createElement("option");
        placeholder.value = "";
        placeholder.textContent = "-- Chọn kỳ học --";
        select.appendChild(placeholder);
        items
            .sort((a, b) => b.startDate.localeCompare(a.startDate))
            .forEach(s => {
                const opt = document.createElement("option");
                opt.value = s.semesterId;
                opt.textContent = `${s.code} — ${s.name} (${s.startDate} → ${s.endDate})${s.status === 0 ? ' [Đã đóng]' : ''}`;
                opt.disabled = (s.status === 0);
                select.appendChild(opt);
            });
    } catch (error) {
        console.error(error);
        help.textContent = "Không tải được danh sách kỳ học. Vui lòng tải lại trang.";
        select.innerHTML = '<option value="">-- Lỗi tải kỳ học --</option>';
    }
}

async function loadSubjects() {
    try {
        const subjects = await apiClient.get('/api/class/subjects');
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
            semesterId: parseInt(document.getElementById("semesterSelect").value),
            subjectId: parseInt(document.getElementById("subjectSelect").value)
        };

        try {
            const data = await apiClient.post('/api/class', payload);
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

    const joinLink = `${window.location.origin}/Class/Join?code=${inviteCode}`;
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