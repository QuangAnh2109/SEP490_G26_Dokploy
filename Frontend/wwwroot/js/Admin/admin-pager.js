window.AdminPager = (function () {
    function render(container, options) {
        const el = typeof container === 'string' ? document.getElementById(container) : container;
        if (!el) return;

        const current = Math.max(1, parseInt(options.current) || 1);
        const totalItems = parseInt(options.totalItems) || 0;
        const pageSize = parseInt(options.pageSize) || 10;
        const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
        const onChange = typeof options.onChange === 'function' ? options.onChange : null;

        if (totalItems === 0) {
            el.innerHTML = '';
            return;
        }

        el.innerHTML = `
            <div class="pager">
                <div class="info">
                    Trang <strong>${current}</strong> / <strong>${totalPages}</strong>
                    (tổng ${totalItems} dòng)
                </div>
                <div class="ctrls">
                    <button type="button" class="btn-pager" data-page="${current - 1}" ${current <= 1 ? 'disabled' : ''}>&larr;</button>
                    <button type="button" class="active">${current}</button>
                    <button type="button" class="btn-pager" data-page="${current + 1}" ${current >= totalPages ? 'disabled' : ''}>&rarr;</button>
                </div>
            </div>
        `;

        if (onChange) {
            el.querySelectorAll('button[data-page]').forEach(btn => {
                btn.addEventListener('click', () => {
                    const target = parseInt(btn.dataset.page);
                    if (target >= 1 && target <= totalPages && target !== current) {
                        onChange(target);
                    }
                });
            });
        }
    }

    return { render };
})();
