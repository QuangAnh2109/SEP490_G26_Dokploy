/**
 * blueprint_val.js
 * Quy tắc validate cho màn hình Tạo / Chỉnh sửa Ma trận đề (Exam Blueprint).
 * Phụ thuộc: validation_shared.js phải được nạp trước.
 */
window.BlueprintValidator = (() => {
    'use strict';

    /**
     * Validate client-side cho payload tạo/cập nhật ma trận đề.
     * @param {object} payload
     * @param {string}         payload.Name
     * @param {number}         payload.SubjectId
     * @param {number}         payload.TargetTotalQuestions
     * @param {number}         payload.TargetStatus   0=nháp, 1=xuất bản
     * @param {object[]}       payload.Rows
     * @param {object}         [chapterAvailability]  map chapterId -> {difficulty -> count}
     * @returns {string[]} Mảng thông báo lỗi (rỗng nếu hợp lệ).
     */
    const validate = (payload, chapterAvailability = {}) => {
        const errors = [];

        if (!payload.Name || !payload.Name.trim()) {
            errors.push('Tên ma trận đề là bắt buộc.');
        }

        if (!payload.SubjectId || payload.SubjectId <= 0) {
            errors.push('Vui lòng chọn môn học.');
        }

        if (!Number.isInteger(payload.TargetTotalQuestions) || payload.TargetTotalQuestions < 0) {
            errors.push('Tổng số câu mục tiêu phải là số nguyên >= 0.');
        }

        // Kiểm tra trùng cặp (chương + mức độ)
        const duplicateSet = new Set();
        payload.Rows.forEach((row, idx) => {
            const key = `${row.ChapterId}-${row.Difficulty}`;
            if (duplicateSet.has(key)) {
                errors.push(`Dòng ${idx + 1}: trùng chương và mức độ.`);
            } else {
                duplicateSet.add(key);
            }
        });

        if (payload.Rows.length === 0) {
            errors.push('Vui lòng chọn ít nhất 1 chương trong môn học để tạo cấu trúc ma trận.');
        }

        // Quy tắc bổ sung khi xuất bản
        if (payload.TargetStatus === 1) {
            const rowSum = payload.Rows.reduce((sum, r) => sum + r.TotalQuestions, 0);

            if (payload.TargetTotalQuestions <= 0) {
                errors.push('Xuất bản yêu cầu tổng số câu mục tiêu > 0.');
            }

            if (payload.TargetTotalQuestions !== rowSum) {
                errors.push('Tổng số câu mục tiêu phải bằng tổng số câu từ các dòng ma trận.');
            }

            payload.Rows.forEach((row, idx) => {
                const available = chapterAvailability[row.ChapterId]?.[row.Difficulty] ?? 0;
                if (row.TotalQuestions > available) {
                    errors.push(`Dòng ${idx + 1}: vượt số câu hiện có trong ngân hàng (${available}).`);
                }
            });
        }

        return errors;
    };

    /**
     * Validate từng dòng ma trận khi thu thập dữ liệu từ DOM.
     * @param {{ ChapterId: number, Difficulty: number, TotalQuestions: number }}[] rows
     * @returns {string[]}
     */
    const validateRows = (rows) => {
        const errors = [];
        rows.forEach((row, idx) => {
            const rowNo = idx + 1;
            if (!Number.isInteger(row.ChapterId) || row.ChapterId <= 0) {
                errors.push(`Dòng ${rowNo}: vui lòng chọn chương.`);
            }
            if (!Number.isInteger(row.Difficulty) || row.Difficulty < 1 || row.Difficulty > 4) {
                errors.push(`Dòng ${rowNo}: mức độ không hợp lệ.`);
            }
            if (!Number.isInteger(row.TotalQuestions) || row.TotalQuestions <= 0) {
                errors.push(`Dòng ${rowNo}: số câu trong chương phải lớn hơn 0.`);
            }
        });
        return errors;
    };

    return { validate, validateRows };
})();
