/**
 * assign_exam_val.js
 * Quy tắc validate cho màn hình Giao đề (Assign Exam).
 * Phụ thuộc: validation_shared.js phải được nạp trước.
 */
window.AssignExamValidator = (() => {
    'use strict';

    const { markFieldInvalid } = window.ValidationShared;

    /**
     * Validate toàn bộ form Giao đề.
     * @param {object} data - Dữ liệu từ form.
     * @param {string}  data.title
     * @param {number}  data.duration
     * @param {number}  data.maxAttempts
     * @param {number}  data.paperCount
     * @param {Date|null} data.openAtDate
     * @param {Date|null} data.closeAtDate
     * @param {boolean} data.isPublic
     * @param {string}  data.publicSubjectValue
     * @param {number|null} data.resolvedClassId
     * @param {string}  data.generationMode  'blueprint' | 'manual'
     * @param {number|null} data.selectedBlueprintId
     * @param {number}  data.manualQuestionCount
     * @param {object}  data.fields - Các phần tử DOM để đánh dấu is-invalid.
     * @returns {string[]} Mảng thông báo lỗi (rỗng nếu hợp lệ).
     */
    const validate = (data) => {
        const errors = [];
        const f = data.fields || {};

        // Reset toàn bộ trạng thái lỗi trước
        Object.values(f).forEach(el => markFieldInvalid(el, false));

        if (!data.title) {
            errors.push('Vui lòng nhập tiêu đề đề thi.');
            markFieldInvalid(f.title, true);
        }

        if (!(data.duration > 0)) {
            errors.push('Thời lượng phải lớn hơn 0.');
            markFieldInvalid(f.duration, true);
        }

        if (!(data.maxAttempts > 0)) {
            errors.push('Số lần làm phải lớn hơn 0.');
            markFieldInvalid(f.maxAttempts, true);
        }

        const paperCountVal = data.paperCount;
        const paperCount = Number(paperCountVal);
        if (paperCountVal === '' || paperCountVal == null || paperCountVal === undefined) {
            errors.push('Vui lòng nhập số mã đề.');
            markFieldInvalid(f.paperCount, true);
        } else if (!Number.isInteger(paperCount) || paperCount < 1 || paperCount > 50) {
            errors.push('Số mã đề phải là số nguyên từ 1 đến 50.');
            markFieldInvalid(f.paperCount, true);
        }

        if (data.openAtDate && data.closeAtDate && data.openAtDate >= data.closeAtDate) {
            errors.push('Thời điểm mở phải < thời điểm đóng.');
            markFieldInvalid(f.openAt, true);
            markFieldInvalid(f.closeAt, true);
        }

        if (data.isPublic && !data.publicSubjectValue) {
            errors.push('Vui lòng chọn môn học.');
            markFieldInvalid(f.publicSubject, true);
        }

        if (!data.isPublic && !data.resolvedClassId) {
            errors.push('Vui lòng chọn lớp học.');
        }

        if (data.generationMode === 'blueprint' && !data.selectedBlueprintId) {
            errors.push('Vui lòng chọn ma trận đề.');
        }

        if (data.generationMode === 'manual' && !(data.manualQuestionCount > 0)) {
            errors.push('Vui lòng chọn ít nhất 1 câu hỏi.');
        }

        return errors;
    };

    return { validate };
})();
