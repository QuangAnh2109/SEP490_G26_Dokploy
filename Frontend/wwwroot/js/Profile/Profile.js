// ================= GLOBAL =================
let originalProfile = {};

// ================= INIT =================
$(function () {

    $('#profileModal').on('shown.bs.modal', async function () {

        console.log("Profile modal opened");

        // reset tab về mặc định
        resetTabs();

        // disable button password
        $("#changePasswordBtn").prop("disabled", true);

        await window.userReady;

        // ẩn tab password nếu login Google
        if (isGoogleUser()) {
            $("#tabPasswordBtn").hide();
            $("#passwordTab").hide();
        } else {
            $("#tabPasswordBtn").show();
        }

        loadProfile();
    });

    $('#profileModal').on('hidden.bs.modal', function () {

        $("#changePasswordForm")[0].reset();
        $("#changePasswordBtn").prop("disabled", true).text("Cập nhật mật khẩu");

        clearFieldError();
    });

});

// ================= TAB =================
function resetTabs() {
    $("#profileTab").show();
    $("#passwordTab").hide();

    $("#tabProfileBtn").addClass("active");
    $("#tabPasswordBtn").removeClass("active");
}

$(document).on("click", "#tabProfileBtn", function () {
    resetTabs();
});

$(document).on("click", "#tabPasswordBtn", function () {
    $("#profileTab").hide();
    $("#passwordTab").show();

    $(this).addClass("active");
    $("#tabProfileBtn").removeClass("active");
});

// ================= LOAD PROFILE =================
async function loadProfile() {
    try {

        const data = await apiClient.get("/api/profile");

        $("#fullName").val(data.fullName || "");
        $("#email").val(data.email || "");
        $("#phoneNumber").val(data.phoneNumber || "");
        $("#studentId").val(data.studentId || "");

        if (String(data.roleId) === RoleIds.Student) {
            $("#studentIdGroup").removeAttr("hidden");
        } else {
            $("#studentIdGroup").attr("hidden", true);
        }

        originalProfile = {
            fullName: data.fullName || "",
            phoneNumber: data.phoneNumber || "",
            studentId: data.studentId || ""
        };

        $("#saveProfileBtn").prop("disabled", true);

    } catch (err) {
        console.error(err);
        showToast("Không tải được thông tin", "error");
    }
}

// ================= ENABLE SAVE =================
$(document).on("input", "#profileForm input", function () {

    const changed =
        $("#fullName").val() !== originalProfile.fullName ||
        $("#phoneNumber").val() !== originalProfile.phoneNumber ||
        $("#studentId").val() !== originalProfile.studentId;

    $("#saveProfileBtn").prop("disabled", !changed);
});

// ================= UPDATE PROFILE =================
$("#profileForm").on("submit", async function (e) {

    e.preventDefault();

    // Clear previous invalid marks
    $("#fullName, #phoneNumber, #studentId").removeClass("is-invalid");

    const fullName = ($("#fullName").val() || "").toString().trim();
    const phoneNumber = ($("#phoneNumber").val() || "").toString().trim();
    const studentId = ($("#studentId").val() || "").toString().trim();
    const isStudent = !$("#studentIdGroup").attr("hidden");

    // Validate FullName
    if (!fullName) {
        showToast("Họ và tên là bắt buộc.", "error");
        $("#fullName").addClass("is-invalid");
        return;
    }
    const fullNamePattern = /^[\p{L}\p{M}]+(?:\s+[\p{L}\p{M}]+)*$/u;
    if (!fullNamePattern.test(fullName)) {
        showToast("Họ và tên chỉ được chứa chữ cái và khoảng trắng.", "error");
        $("#fullName").addClass("is-invalid");
        return;
    }

    // Validate PhoneNumber (optional)
    if (phoneNumber) {
        const phonePattern = /^0\d{9}$/;
        if (!phonePattern.test(phoneNumber)) {
            showToast("Số điện thoại phải gồm 10 số và bắt đầu bằng 0.", "error");
            $("#phoneNumber").addClass("is-invalid");
            return;
        }
    }

    // Validate StudentId (student must have, format must match)
    if (isStudent) {
        if (!studentId) {
            showToast("Mã sinh viên là bắt buộc cho tài khoản học sinh.", "error");
            $("#studentId").addClass("is-invalid");
            return;
        }
        const studentIdPattern = /^[A-Za-z]{2}\d{6}$/;
        if (!studentIdPattern.test(studentId)) {
            showToast("Mã sinh viên phải gồm 8 ký tự: 2 chữ cái đầu và 6 chữ số sau (ví dụ: SE123456).", "error");
            $("#studentId").addClass("is-invalid");
            return;
        }
    } else if (studentId) {
        const studentIdPattern = /^[A-Za-z]{2}\d{6}$/;
        if (!studentIdPattern.test(studentId)) {
            showToast("Mã sinh viên phải gồm 8 ký tự: 2 chữ cái đầu và 6 chữ số sau (ví dụ: SE123456).", "error");
            $("#studentId").addClass("is-invalid");
            return;
        }
    }

    const payload = {
        fullName: fullName,
        phoneNumber: phoneNumber || null
    };

    // chỉ gửi studentId nếu có
    if (!$("#studentIdGroup").attr("hidden")) {
        payload.studentId = studentId;
    } else if (studentId) {
        payload.studentId = studentId;
    }

    try {

        await apiClient.put("/api/profile", payload);

        showToast("Cập nhật thành công", "success");

        originalProfile = { ...payload };
        $("#saveProfileBtn").prop("disabled", true);

    } catch (err) {
        showToast(err.message || "Cập nhật thất bại", "error");
    }
});

// ================= PASSWORD VALIDATION =================
function validateChangePassword(oldPassword, newPassword, confirmPassword) {

    if (!oldPassword || !newPassword || !confirmPassword) {
        return "Vui lòng nhập đầy đủ thông tin";
    }

    if (newPassword !== confirmPassword) {
        return "Mật khẩu xác nhận không khớp";
    }

    const pattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$/;

    if (!pattern.test(newPassword)) {
        return "Mật khẩu phải 8-72 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt";
    }

    return null;
}

// ================= FIELD ERROR =================
function showFieldError(selector) {
    $(selector).addClass("is-invalid");
}

function clearFieldError() {
    $("#currentPassword, #newPassword, #confirmPassword").removeClass("is-invalid");
}

// ================= ENABLE PASSWORD BUTTON =================
$(document).on("input", "#currentPassword, #newPassword, #confirmPassword", function () {

    const enable =
        $("#currentPassword").val() &&
        $("#newPassword").val() &&
        $("#confirmPassword").val();

    $("#changePasswordBtn").prop("disabled", !enable);
});

// ================= CHANGE PASSWORD =================
$("#changePasswordForm").on("submit", function (e) {

    e.preventDefault();

    clearFieldError();

    const currentPassword = $("#currentPassword").val();
    const newPassword = $("#newPassword").val();
    const confirmPassword = $("#confirmPassword").val();

    const error = validateChangePassword(currentPassword, newPassword, confirmPassword);

    if (error) {

        if (!currentPassword) showFieldError("#currentPassword");
        if (!newPassword) showFieldError("#newPassword");
        if (!confirmPassword) showFieldError("#confirmPassword");

        if (newPassword !== confirmPassword) {
            showFieldError("#confirmPassword");
        }

        showToast(error, "error");
        return;
    }

    $("#changePasswordBtn")
        .prop("disabled", true)
        .html('<span class="spinner-border spinner-border-sm"></span> Đang xử lý...');

    apiClient.put("/api/profile/change-password", {
        currentPassword,
        newPassword
    })
        .then(() => {

            showToast("Đổi mật khẩu thành công", "success");

            $("#changePasswordForm")[0].reset();

            // đóng modal cho UX tốt hơn
            $("#profileModal").modal("hide");

        })
        .catch(err => {

            showToast(err.responseJSON?.message || "Mật khẩu hiện tại không đúng", "error");
            showFieldError("#currentPassword");

        })
        .finally(() => {

            $("#changePasswordBtn")
                .prop("disabled", true)
                .text("Cập nhật mật khẩu");

        });
});