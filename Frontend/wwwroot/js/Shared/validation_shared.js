/**
 * validation_shared.js
 * Các tiện ích dùng chung cho việc hiển thị lỗi và đánh dấu trường không hợp lệ.
 */
window.ValidationShared = (() => {
    'use strict';

    /**
     * Đánh dấu hoặc bỏ đánh dấu một trường input là không hợp lệ.
     * @param {HTMLElement|null} field - Phần tử input cần đánh dấu.
     * @param {boolean} isInvalid - true nếu trường không hợp lệ.
     */
    const markFieldInvalid = (field, isInvalid) => {
        if (field) field.classList.toggle('is-invalid', Boolean(isInvalid));
    };

    /**
     * Xóa trạng thái lỗi cho danh sách các trường.
     * @param {Array<HTMLElement|null>} fields - Mảng các phần tử input.
     */
    const resetFields = (fields) => {
        (fields || []).forEach(f => markFieldInvalid(f, false));
    };

    /**
     * Hiển thị danh sách thông báo lỗi trong một container bằng template.
     * Container cần chứa:
     *   - <template id="{listTemplateId}"> với <ul data-error-list>
     *   - <template id="{itemTemplateId}"> với <li data-error-message>
     * @param {HTMLElement} container - Phần tử chứa thông báo lỗi.
     * @param {string[]} messages - Mảng các chuỗi thông báo lỗi.
     * @param {{ listTemplateId: string, itemTemplateId: string }} opts
     */
    const showErrorList = (container, messages, opts = {}) => {
        if (!container) return;
        container.innerHTML = '';

        const { listTemplateId = 'errorListTemplate', itemTemplateId = 'errorItemTemplate' } = opts;
        const listTpl = document.getElementById(listTemplateId);
        const itemTpl = document.getElementById(itemTemplateId);

        if (!listTpl || !itemTpl) {
            // Fallback nếu không có template: dùng textContent thuần
            container.textContent = messages.join('\n');
            container.classList.remove('d-none');
            return;
        }

        const list = listTpl.content.cloneNode(true).firstElementChild;
        const listBody = list.hasAttribute('data-error-list') ? list : list.querySelector('[data-error-list]');

        messages.forEach(m => {
            const item = itemTpl.content.cloneNode(true).firstElementChild;
            const msgNode = item.hasAttribute('data-error-message') ? item : item.querySelector('[data-error-message]');
            if (msgNode) msgNode.textContent = m;
            if (listBody) listBody.appendChild(item);
        });

        container.appendChild(list);
        container.classList.remove('d-none');
    };

    /**
     * Ẩn container thông báo lỗi.
     * @param {...HTMLElement} containers
     */
    const hideErrors = (...containers) => {
        containers.forEach(c => {
            if (c) {
                c.innerHTML = '';
                c.classList.add('d-none');
            }
        });
    };

    return { markFieldInvalid, resetFields, showErrorList, hideErrors };
})();
