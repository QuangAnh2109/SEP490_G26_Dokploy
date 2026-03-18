// ================= GLOBAL =================
let originalProfile = {};

// ================= INIT =================
$(function () {

    // mở modal
    $('#profileModal').on('shown.bs.modal', function () {

        console.log("Profile modal opened");

        // ẩn đổi mật khẩu nếu login Google
        if (isGoogleUser()) {
            $("#tabPasswordBtn").hide();
            $("#passwordTab").hide();
        } else {
            $("#tabPasswordBtn").show();
        }

        loadProfile();
    });

    // reset khi đóng modal
    $('#profileModal').on('hidden.bs.modal', function () {
        $("#changePasswordForm")[0].reset();
        $("#changePasswordBtn").prop("disabled", true).text("Cập nhật mật khẩu");
        clearFieldError();
    });

});

// ================= TAB SWITCH =================
$(document).on("click", "#tabProfileBtn", function () {
    $("#profileTab").show();
    $("#passwordTab").hide();

    $(this).addClass("active");
    $("#tabPasswordBtn").removeClass("active");
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

        if (data.roleId === 2) {
            $("#studentIdGroup").removeAttr("hidden");
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

    const payload = {
        fullName: $("#fullName").val(),
        studentId: $("#studentId").val(),
        phoneNumber: $("#phoneNumber").val()
    };

    try {

        await apiClient.put("/api/profile", payload);

        showToast("Cập nhật thành công", "success");

        originalProfile = payload;
        $("#saveProfileBtn").prop("disabled", true);

    } catch (err) {
        showToast(err.message || "Cập nhật thất bại", "error");
    }
});

// ================= VALIDATE PASSWORD =================
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

// ================= PASSWORD INPUT =================
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

    const oldPassword = $("#currentPassword").val();
    const newPassword = $("#newPassword").val();
    const confirmPassword = $("#confirmPassword").val();

    const error = validateChangePassword(oldPassword, newPassword, confirmPassword);

    if (error) {

        clearFieldError();

        if (!oldPassword) showFieldError("#currentPassword");
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

    apiClient.post("/api/auth/change-password", {
        oldPassword,
        newPassword
    })
        .then(() => {

            showToast("Đổi mật khẩu thành công", "success");

            $("#changePasswordForm")[0].reset();

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