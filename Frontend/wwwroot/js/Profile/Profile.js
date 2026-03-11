document.addEventListener("DOMContentLoaded", function () {

    const path = window.location.pathname;

    if (path === "/Profile" || path === "/Profile/Index") {


        if (isGoogleUser()) {
            document.getElementById("tabPasswordBtn").style.display = "none";
            document.getElementById("passwordTab").style.display = "none";
        }

        loadProfile();
        initUpdateProfile();
        initChangePassword();
    }

});


let originalProfile = {};

async function loadProfile() {

    try {

        const data = await apiClient.get("/api/profile");

        $("#fullName").val(data.fullName);
        $("#email").val(data.email);
        $("#phoneNumber").val(data.phoneNumber);
        $("#studentId").val(data.studentId);

        // roleId = 2 thì hiện mã sinh viên
        if (data.roleId === 2) {
            $("#studentIdGroup").removeAttr("hidden");
        }

        originalProfile = {
            fullName: data.fullName || "",
            phoneNumber: data.phoneNumber || "",
            studentId: data.studentId || ""
        };

        $("#saveProfileBtn").prop("disabled", true);

    }
    catch (err) {
        showToast("Không tải được thông tin người dùng", "error");
    }

}


function initUpdateProfile() {

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

            // cập nhật lại dữ liệu gốc
            originalProfile = {
                fullName: payload.fullName || "",
                phoneNumber: payload.phoneNumber || "",
                studentId: payload.studentId || ""
            };

            $("#saveProfileBtn").prop("disabled", true);

        }
        catch (err) {

            showToast(err.message || "Cập nhật thất bại", "error");

        }

    });

}


function initChangePassword() {
    console.log("initChangePassword loaded");
    $("#currentPassword, #newPassword, #confirmPassword").on("input", function () {

        const current = $("#currentPassword").val().trim();
        const newPass = $("#newPassword").val().trim();
        const confirm = $("#confirmPassword").val().trim();

        const enable = current !== "" && newPass !== "" && confirm !== "";

        $("#changePasswordBtn").prop("disabled", !enable);

    });

    $("#changePasswordForm").on("submit", function (e) {

        e.preventDefault();

        const oldPassword = $("#currentPassword").val();
        const newPassword = $("#newPassword").val();
        const confirmPassword = $("#confirmPassword").val();

        if (!oldPassword || !newPassword || !confirmPassword) {
            showToast("Vui lòng điền đầy đủ thông tin", "error");
            return;
        }

        if (newPassword !== confirmPassword) {
            showToast("Mật khẩu xác nhận không khớp", "error");
            return;
        }

        const passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$/;

        if (!passwordPattern.test(newPassword)) {
            showToast("Mật khẩu phải từ 8-72 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt", "error");
            return;
        }

        const payload = {
            oldPassword: oldPassword,
            newPassword: newPassword
        };

        $("#changePasswordBtn").prop("disabled", true).text("Đang cập nhật...");

        apiClient.post("/api/auth/change-password", payload)
            .then(function () {

                showToast("Đổi mật khẩu thành công", "success");

                $("#changePasswordForm")[0].reset();
                $("#changePasswordBtn").prop("disabled", true).text("Cập nhật mật khẩu");

            })
            .catch(function (err) {

                showToast(err.responseJSON?.message || "Đổi mật khẩu thất bại", "error");

                $("#changePasswordBtn").prop("disabled", false).text("Cập nhật mật khẩu");

            });

    });

}
