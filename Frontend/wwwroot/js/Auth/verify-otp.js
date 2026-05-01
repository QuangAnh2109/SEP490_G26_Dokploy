let targetEmail = "";
let countdownTimer;
let timeLeft = 60;

$(document).ready(function () {
    targetEmail = localStorage.getItem('pendingRegistrationEmail');

    if (!targetEmail) {
        // Must go to Register first
        window.location.href = '/Auth/Register';
        return;
    }

    $('#displayEmail').text(targetEmail);
    startTimer();
    setupOtpInputs();
});

function setupOtpInputs() {
    const inputs = document.querySelectorAll('.otp-input');

    inputs.forEach((input, index) => {
        // Focus first input on load
        if (index === 0) input.focus();

        input.addEventListener('input', function (e) {
            const val = this.value;
            // Only allow numbers
            if (val && !/^\d+$/.test(val)) {
                this.value = '';
                return;
            }

            // Auto focus next input
            if (val && index < inputs.length - 1) {
                inputs[index + 1].focus();
            }
        });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace' && !this.value && index > 0) {
                inputs[index - 1].focus();
            } else if (e.key === 'Enter') {
                verifyOtp();
            }
        });

        // Handle Paste (e.g user copies "123456" from email)
        input.addEventListener('paste', function (e) {
            e.preventDefault();
            const pastedData = e.clipboardData.getData('text').trim();
            if (/^\d{6}$/.test(pastedData)) {
                for (let i = 0; i < 6; i++) {
                    inputs[i].value = pastedData[i];
                }
                verifyOtp();
            }
        });
    });
}

function startTimer() {
    if (countdownTimer) clearInterval(countdownTimer);
    timeLeft = 60;
    $('#resendLink').addClass('disabled').html('Gửi lại mã (<span id="timer">' + timeLeft + '</span>s)');

    countdownTimer = setInterval(function () {
        timeLeft--;
        $('#timer').text(timeLeft);
        if (timeLeft <= 0) {
            clearInterval(countdownTimer);
            countdownTimer = null;
            $('#resendLink').removeClass('disabled').html('Gửi lại mã');
        }
    }, 1000);
}

function readOtpValue() {
    let otp = "";
    $('.otp-input').each(function () {
        otp += $(this).val();
    });
    return otp;
}

function verifyOtp() {
    const otpCode = readOtpValue();
    if (otpCode.length < 6) {
        showError("Vui lòng nhập đủ 6 chữ số.");
        return;
    }

    hideMessages();
    $('#loadingText').text('Đang xác thực mã OTP...');
    $('#loadingOverlay').css('display', 'flex');

    var requestData = {
        Email: targetEmail,
        OtpCode: otpCode
    };

    apiClient.post('/api/auth/verify-otp', requestData)
        .then(function (data) {
            localStorage.removeItem('pendingRegistrationEmail');
            localStorage.removeItem('pendingRegistrationRole');

            $('#loadingText').text('Đăng ký thành công! Đang chuyển hướng...');
            setTimeout(() => {
                window.location.href = '/';
            }, 1000);
        })
        .catch(function (err) {
            $('#loadingOverlay').hide();
            if (err.xhr && err.xhr.responseJSON && err.xhr.responseJSON.message) {
                showError(err.xhr.responseJSON.message);
            } else {
                showError("Mã OTP không đúng hoặc đã hết hạn.");
            }
        });
}

function resendOtp() {
    if ($('#resendLink').hasClass('disabled')) return;

    hideMessages();
    $('#loadingText').text('Đang gửi lại mã...');
    $('#loadingOverlay').css('display', 'flex');

    apiClient.post('/api/auth/resend-otp', { Email: targetEmail })
        .then(function () {
            $('#loadingOverlay').hide();
            showSuccess('Mã OTP mới đã được gửi đến email của bạn.');
            startTimer();
        })
        .catch(function (err) {
            $('#loadingOverlay').hide();
            showError(err.xhr?.responseJSON?.message || err.message || "Không thể gửi lại mã. Mã OTP có thể đã hết hạn.");
        });
}

function showError(message) {
    $('#formError').text(message).show();
}

function showSuccess(message) {
    $('#formSuccess').text(message).show();
}

function hideMessages() {
    $('#formError').hide();
    $('#formSuccess').hide();
}
