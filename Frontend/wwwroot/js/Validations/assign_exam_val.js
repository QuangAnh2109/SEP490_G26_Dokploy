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

        // ── Validate thời gian ──

        // 1. Bắt buộc nhập cả 3 mốc thời gian
        if (!data.visibleFromDate) {
            errors.push('Vui lòng nhập thời điểm học sinh thấy đề.');
            markFieldInvalid(f.visibleFrom, true);
        }
        if (!data.openAtDate) {
            errors.push('Vui lòng nhập thời điểm mở.');
            markFieldInvalid(f.openAt, true);
        }
        if (!data.closeAtDate) {
            errors.push('Vui lòng nhập thời điểm đóng.');
            markFieldInvalid(f.closeAt, true);
        }

        // Chỉ check quan hệ khi đã có đủ giá trị
        if (data.visibleFromDate && data.openAtDate && data.closeAtDate) {

            // 2. openAt < closeAt (bắt buộc, không cho bằng)
            if (data.openAtDate >= data.closeAtDate) {
                errors.push('Thời điểm mở phải nhỏ hơn thời điểm đóng.');
                markFieldInvalid(f.openAt, true);
                markFieldInvalid(f.closeAt, true);
            }

            // 3. visibleFrom <= openAt (phải thấy đề trước hoặc cùng lúc mở)
            if (data.visibleFromDate > data.openAtDate) {
                errors.push('Thời điểm thấy đề phải trước hoặc bằng thời điểm mở.');
                markFieldInvalid(f.visibleFrom, true);
                markFieldInvalid(f.openAt, true);
            }

            // 4. visibleFrom < closeAt (phải thấy đề trước khi đóng)
            if (data.visibleFromDate >= data.closeAtDate) {
                errors.push('Thời điểm thấy đề phải trước thời điểm đóng.');
                markFieldInvalid(f.visibleFrom, true);
                markFieldInvalid(f.closeAt, true);
            }

            // 5. Khoảng mở-đóng phải đủ cho thời lượng làm bài
            if (data.openAtDate < data.closeAtDate && data.duration > 0) {
                const windowMinutes = (data.closeAtDate - data.openAtDate) / 60000;
                if (windowMinutes < data.duration) {
                    errors.push(`Khoảng cách mở-đóng (${Math.round(windowMinutes)} phút) phải >= thời lượng làm bài (${data.duration} phút).`);
                    markFieldInvalid(f.openAt, true);
                    markFieldInvalid(f.closeAt, true);
                }
            }
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
