// Delegated password show/hide toggle. Buttons need class="password-toggle" data-target="<inputId>".
$(function () {
    $(document).on('click', '.password-toggle', function () {
        const $toggle = $(this);
        const $input = $('#' + $toggle.attr('data-target'));
        if (!$input.length) return;
        const $icon = $toggle.find('i');
        const willShow = $input.attr('type') === 'password';
        $input.attr('type', willShow ? 'text' : 'password');
        $icon.toggleClass('fa-eye fa-eye-slash');
        $toggle.attr('aria-label', willShow ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
        $toggle.attr('aria-pressed', willShow ? 'true' : 'false');
    });
});
