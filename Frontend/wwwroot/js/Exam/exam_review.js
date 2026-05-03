/**
 * exam_review.js
 * Redesigned logic for Exam Review with Sidebar navigation.
 */

let API_BASE = "";
let CLASS_ID = null;
let currentReviewData = null;
let selectedAlternativeId = null;
let swapContext = {
    paperId: null,
    oldQuestionId: null
};

async function initReviewPage(config) {
    if (!config || !config.examId) {
        showToast("Thiếu cấu hình ExamID", "error");
        return;
    }

    API_BASE = config.apiBase || "/api/assign-exam";
    CLASS_ID = config.classId || null;

    try {
        await loadReviewData(config.examId);
    } catch (error) {
        console.error("Failed to initialize review page:", error);
        showToast("Không thể tải thông tin đề thi.", "error");
    }
}

async function loadReviewData(examId) {
    currentReviewData = await apiClient.get(`${API_BASE}/review/${examId}`);
    if (currentReviewData.classId && !CLASS_ID) {
        CLASS_ID = currentReviewData.classId;
    }
    renderInfoView();
    renderPaperSidebar();
    renderOverview();
    setupSidebarNavigation();
}

function setupSidebarNavigation() {
    // Only target top-level static nav items that have a data-target
    document.querySelectorAll(".sidebar-nav[data-target]").forEach(btn => {
        btn.onclick = () => {
            const targetId = btn.dataset.target;
            switchView(targetId, btn);
        };
    });
}

function switchView(viewId, navBtn) {
    // Update active nav state
    document.querySelectorAll(".sidebar-nav").forEach(b => b.classList.remove("active"));
    if (navBtn) navBtn.classList.add("active");

    // Show target section
    document.querySelectorAll(".section-view").forEach(v => v.classList.remove("active"));
    const target = document.getElementById(viewId);
    if (target) {
        target.classList.add("active");
    } else {
        console.warn(`View ID ${viewId} not found.`);
    }
}

function renderInfoView() {
    const d = currentReviewData;

    // Header
    setText("info-exam-title", d.title);
    setText("info-subject-code", d.subjectCode);

    // Info Cards
    setText("card-subject-code", d.subjectCode);
    setText("card-exam-title", d.title);
    setText("card-total-questions", d.totalQuestions + " câu");
    setText("card-duration", d.duration + " phút");
    setText("card-visible-from", formatDateTime(d.visibleFrom));
    setText("card-open-at", formatDateTime(d.openAt));
    setText("card-close-at", formatDateTime(d.closeAt));
    setText("card-teacher-name", d.teacherName);
    setText("card-updated-at", formatDateTime(d.updatedAtUtc));
    setText("card-description", d.description || "Không có ghi chú.");

    // Status badge
    const badgeEl = document.getElementById("info-status-badge");
    if (badgeEl) {
        const statusMap = {
            0: { text: "Chờ duyệt", cls: "meta-badge badge-pending" },
            1: { text: "Đã duyệt", cls: "meta-badge badge-public" },
            2: { text: "Đang thi", cls: "meta-badge badge-inprogress" },
            3: { text: "Đã hủy", cls: "meta-badge badge-cancelled" },
            4: { text: "Đã đóng", cls: "meta-badge badge-closed" },
        };
        const info = statusMap[d.status] || { text: "Khác", cls: "meta-badge badge-unknown" };
        badgeEl.textContent = info.text;
        badgeEl.className = info.cls;
    }

    // Show/hide action buttons based on status
    const btnApprove = document.getElementById("btnApprove");
    const btnCancel = document.getElementById("btnCancel");
    const btnRestore = document.getElementById("btnRestore");
    const btnDelete = document.getElementById("btnDelete");
    const btnEdit = document.getElementById("btnEditInfo");

    // Hide all first
    [btnApprove, btnCancel, btnRestore, btnDelete, btnEdit].forEach(b => {
        if (b) b.classList.add("d-none");
    });

    // Hide edit form, show read-only
    const editForm = document.getElementById("infoEditForm");
    const readOnly = document.getElementById("infoReadOnly");
    if (editForm) editForm.classList.add("d-none");
    if (readOnly) readOnly.classList.remove("d-none");

    if (d.status === 0) {
        // Ready: Approve + Delete + Edit
        if (btnApprove) btnApprove.classList.remove("d-none");
        if (btnDelete) btnDelete.classList.remove("d-none");
        if (btnEdit) {
            btnEdit.classList.remove("d-none");
            setButtonContent(btnEdit, "fas fa-pencil-alt me-1", "Chỉnh sửa");
        }
    } else if (d.status === 1) {
        // Published: Cancel
        if (btnCancel) btnCancel.classList.remove("d-none");
    } else if (d.status === 3) {
        // Cancelled: Restore + Delete + Edit (time only)
        if (btnRestore) btnRestore.classList.remove("d-none");
        if (btnDelete) btnDelete.classList.remove("d-none");
        if (btnEdit) {
            btnEdit.classList.remove("d-none");
            setButtonContent(btnEdit, "fas fa-clock me-1", "Điều chỉnh thời gian");
        }
    }
    // InProgress (2) and Closed (5): no actions

    // Matrix
    const matrixBody = document.getElementById("matrix-body");
    if (!matrixBody) return;
    clearElement(matrixBody);

    if (d.blueprintMatrix && d.blueprintMatrix.length > 0) {
        let totals = { nb: 0, th: 0, vd: 0, vdc: 0, total: 0 };

        d.blueprintMatrix.forEach(row => {
            totals.nb += row.recognize;
            totals.th += row.understand;
            totals.vd += row.apply;
            totals.vdc += row.advancedApply;
            totals.total += row.total;

            const tr = getTemplateContent('blueprintMatrixRowTemplate');
            tr.querySelector('[data-field-chapter]').textContent = row.chapterName;
            tr.querySelector('[data-field-recognize]').textContent = row.recognize;
            tr.querySelector('[data-field-understand]').textContent = row.understand;
            tr.querySelector('[data-field-apply]').textContent = row.apply;
            tr.querySelector('[data-field-advanced]').textContent = row.advancedApply;
            tr.querySelector('[data-field-total]').textContent = row.total;
            matrixBody.appendChild(tr);
        });

        // Totals row
        const footer = getTemplateContent('blueprintMatrixFooterTemplate');
        footer.querySelector('[data-field-recognize]').textContent = totals.nb;
        footer.querySelector('[data-field-understand]').textContent = totals.th;
        footer.querySelector('[data-field-apply]').textContent = totals.vd;
        footer.querySelector('[data-field-advanced]').textContent = totals.vdc;
        footer.querySelector('[data-field-total]').textContent = totals.total;
        matrixBody.appendChild(footer);
    } else {
        matrixBody.appendChild(getTemplateContent('matrixEmptyTemplate'));
    }
}

function renderPaperSidebar() {
    const container = document.getElementById("paper-nav-list");
    if (!container) return;
    clearElement(container);

    currentReviewData.papers.forEach(p => {
        const btn = document.createElement("button");
        btn.className = "nav-item-custom sidebar-nav paper-item";

        const icon = document.createElement("i");
        icon.className = "far fa-file-alt";
        btn.appendChild(icon);
        btn.appendChild(document.createTextNode(` Mã đề ${p.code}`));

        btn.onclick = () => {
            renderPaperQuestions(p);
            switchView("view-paper-detail", btn);
        };
        container.appendChild(btn);
    });
}

function renderOverview() {
    const container = document.getElementById("overviewQuestionsList");
    if (!container) return;
    clearElement(container);

    const questionsMap = new Map();
    currentReviewData.papers.forEach(paper => {
        paper.questions.forEach(q => {
            if (!questionsMap.has(q.questionId)) {
                questionsMap.set(q.questionId, { ...q, papers: [] });
            }
            questionsMap.get(q.questionId).papers.push(paper.code);
        });
    });

    const uniqueQuestions = Array.from(questionsMap.values());
    if (uniqueQuestions.length === 0) {
        container.appendChild(getTemplateContent('emptyOverviewQuestionsTemplate'));
        return;
    }

    uniqueQuestions.forEach((q, idx) => {
        const item = getTemplateContent('overviewQuestionTemplate');
        item.querySelector('[data-question-label]').textContent = `Câu hỏi ${idx + 1}`;

        const tagsContainer = item.querySelector('[data-paper-tags-container]');
        clearElement(tagsContainer);
        q.papers.forEach(p => {
            const tag = getTemplateContent('paperTagTemplate');
            tag.textContent = `Mã đề ${p}`;
            tagsContainer.appendChild(tag);
        });

        const badge = item.querySelector('[data-difficulty-badge]');
        badge.className = `difficulty-badge ${getDifficultyClass(q.difficulty)}`;
        badge.textContent = getDifficultyText(q.difficulty);

        item.querySelector('.render-content-target').id = `overview-q-${q.questionId}`;
        item.querySelector('[data-chapter-name]').textContent = `Chương: ${q.chapterName}`;

        const collapseBtn = item.querySelector('[data-collapse-button]');
        collapseBtn.setAttribute('data-bs-target', `#collapse-overview-${idx}`);
        item.querySelector('[data-collapse-target]').id = `collapse-overview-${idx}`;

        const ansContainer = item.querySelector('.answers-container');
        ansContainer.id = `answers-overview-${idx}`;
        const expContainer = item.querySelector('.explanation-container');
        expContainer.id = `exp-overview-${idx}`;

        container.appendChild(item);

        if (currentReviewData.status === 0) {
            const paperInstance = currentReviewData.papers.find(p => p.questions.some(pq => pq.questionId === q.questionId));
            const paperId = paperInstance ? paperInstance.paperId : null;
            const swapBtnContainer = item.querySelector('.swap-button-container');
            if (swapBtnContainer) {
                swapBtnContainer.classList.remove('d-none');
                item.querySelector('[data-btn-swap]').onclick = () => openSwapModal(paperId, q.questionId, true);
            }
        }

        renderQuestionItemContent(item.querySelector('.render-content-target'), q.contentLatex, q.questionType);
        renderAnswersAndExplanation(`answers-overview-${idx}`, `exp-overview-${idx}`, q);
    });
}

function renderPaperQuestions(paper) {
    setText("paper-detail-title", `Mã đề: ${paper.code}`);

    const container = document.getElementById("paperQuestionsList");
    if (!container) return;
    clearElement(container);

    paper.questions.forEach((q, idx) => {
        const item = getTemplateContent('paperQuestionTemplate');
        item.querySelector('[data-question-label]').textContent = `Câu ${idx + 1}`;

        const badge = item.querySelector('[data-difficulty-badge]');
        badge.className = `difficulty-badge ${getDifficultyClass(q.difficulty)} me-2`;
        badge.textContent = getDifficultyText(q.difficulty);

        item.querySelector('.render-content-target').id = `paper-q-${q.questionId}`;

        const collapseBtn = item.querySelector('[data-collapse-button]');
        collapseBtn.setAttribute('data-bs-target', `#collapse-paper-${q.questionId}`);
        item.querySelector('[data-collapse-target]').id = `collapse-paper-${q.questionId}`;

        const ansContainer = item.querySelector('.answers-container');
        ansContainer.id = `answers-paper-${q.questionId}`;
        const expContainer = item.querySelector('.explanation-container');
        expContainer.id = `exp-paper-${q.questionId}`;

        if (currentReviewData.status === 0) {
            const swapBtnContainer = item.querySelector('.swap-button-container');
            swapBtnContainer.classList.remove('d-none');
            item.querySelector('[data-btn-swap]').onclick = () => openSwapModal(paper.paperId, q.questionId);
        }

        container.appendChild(item);
        renderQuestionItemContent(item.querySelector('.render-content-target'), q.contentLatex, q.questionType);
        renderAnswersAndExplanation(`answers-paper-${q.questionId}`, `exp-paper-${q.questionId}`, q);
    });
}

function renderQuestionItemContent(container, content, type) {
    try {
        if (!content) content = "";
        const isJson = content.trim().startsWith('{') && content.trim().endsWith('}');
        clearElement(container);
        if (isJson || type === 'FillInBlank') {
            const data = JSON.parse(content || "{}");
            if (window.QuestionEditorUtils) {
                const tpl = getTemplateContent('questionContentJsonTemplate');
                container.appendChild(tpl);
                window.QuestionEditorUtils.renderLatexInElement(container.querySelector('.stem-container'), data.stem || '');
                window.QuestionEditorUtils.renderLatexInElement(container.querySelector('.frame-container'), data.frame || '');
            } else {
                const fbTpl = getTemplateContent('questionContentFallbackJsonTemplate');
                fbTpl.querySelector('.stem-content').textContent = data.stem || '';
                fbTpl.querySelector('.frame-content').textContent = data.frame || '';
                container.appendChild(fbTpl);
            }
        } else {
            const tpl = getTemplateContent('questionContentStandardTemplate');
            container.appendChild(tpl);
            if (window.QuestionEditorUtils) {
                window.QuestionEditorUtils.renderLatexInElement(tpl, content);
            } else {
                tpl.textContent = content;
            }
        }
    } catch (e) {
        const errTpl = getTemplateContent('questionContentErrorTemplate');
        errTpl.textContent = `Lỗi hiển thị nội dung: ${e.message}`;
        clearElement(container);
        container.appendChild(errTpl);
    }
}

function renderAnswersAndExplanation(answersContainerId, explanationContainerId, q) {
    const ansContainer = document.getElementById(answersContainerId);
    const expContainer = document.getElementById(explanationContainerId);
    if (!ansContainer || !expContainer) return;

    clearElement(ansContainer);
    if (q.answers && q.answers.length > 0) {
        const isMultipleChoice = q.questionType === 'MultipleChoice' || q.questionType === 'MultipleResponse';
        const labels = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'];

        const ansLabel = document.createElement("strong");
        ansLabel.className = "d-block mb-2 text-dark";
        ansLabel.textContent = "Đáp án:";
        ansContainer.appendChild(ansLabel);

        q.answers.forEach((ans, idx) => {
            try {
                const label = isMultipleChoice ? (labels[idx] || '?') : `Ô trống ${idx + 1}`;
                const displayContent = ans.correctAnswer ? ans.correctAnswer : ans.content;
                const safeContent = (displayContent !== null && displayContent !== undefined) ? String(displayContent) : '';

                const row = getTemplateContent('answerRowTemplate');
                if (ans.isCorrect) {
                    row.className = `p-2 mb-2 border rounded bg-success bg-opacity-10 border-success`;
                    row.querySelector('[data-correct-icon]').classList.remove('d-none');
                } else {
                    row.className = `p-2 mb-2 border rounded bg-white`;
                }

                row.querySelector('[data-answer-label]').textContent = `${label}.`;

                ansContainer.appendChild(row);

                const renderTarget = row.querySelector('.render-target');
                if (window.QuestionEditorUtils) {
                    window.QuestionEditorUtils.renderLatexInElement(renderTarget, safeContent);
                } else {
                    renderTarget.textContent = safeContent;
                }
            } catch (e) {
                console.error("Lỗi khi kết xuất đáp án", e);
            }
        });
    } else {
        ansContainer.appendChild(getTemplateContent('emptyAnswersTemplate'));
    }

    let explanationText = "";
    try {
        const isJson = q.contentLatex && q.contentLatex.trim().startsWith('{') && q.contentLatex.trim().endsWith('}');
        if (isJson || q.questionType === 'FillInBlank') {
            const data = JSON.parse(q.contentLatex || "{}");
            explanationText = data.explanation || "";
        }
    } catch (e) { }

    const expContent = expContainer.querySelector('.explanation-content');
    if (explanationText.trim()) {
        if (window.QuestionEditorUtils) {
            window.QuestionEditorUtils.renderLatexInElement(expContent, explanationText);
        } else {
            expContent.textContent = explanationText;
        }
    } else {
        expContainer.style.display = 'none';
    }
}

// Modal and Swapping Logic
async function openSwapModal(paperId, questionId, isForceGlobal = false) {
    swapContext = { paperId, questionId };
    selectedAlternativeId = null;
    const btn = document.getElementById("btnConfirmSwap");
    if (btn) btn.disabled = true;

    const chkGlobal = document.getElementById("chkSwapGlobal");
    if (chkGlobal) {
        chkGlobal.checked = isForceGlobal;
        chkGlobal.disabled = isForceGlobal;
    }

    const modalEl = document.getElementById('alternativeQuestionsModal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);
    modal.show();

    const list = document.getElementById("alternativesList");
    if (!list) return;
    clearElement(list);
    list.appendChild(getTemplateContent('alternativesLoadingTemplate'));

    try {
        const alternatives = await apiClient.get(`${API_BASE}/papers/${paperId}/questions/${questionId}/alternatives`);
        renderAlternatives(alternatives);
    } catch (e) {
        const errTpl = getTemplateContent('alternativesErrorTemplate');
        errTpl.textContent = `Lỗi: ${e.message || 'Lỗi khi tải dữ liệu.'}`;
        clearElement(list);
        list.appendChild(errTpl);
    }
}

function renderAlternatives(list) {
    const container = document.getElementById("alternativesList");
    if (!container) return;
    clearElement(container);

    if (list.length === 0) {
        container.appendChild(getTemplateContent('alternativesEmptyTemplate'));
        return;
    }

    list.forEach(q => {
        const row = getTemplateContent('alternativeItemTemplate');
        row.querySelector('[data-item-info]').textContent = `ID: ${q.questionId} | ${q.chapterName}`;

        const badge = row.querySelector('[data-difficulty-badge]');
        badge.className = `difficulty-badge ${getDifficultyClass(q.difficulty)}`;
        badge.textContent = getDifficultyText(q.difficulty);

        row.onclick = () => {
            container.querySelectorAll(".alternative-item").forEach(i => i.classList.remove("selected"));
            row.classList.add("selected");
            selectedAlternativeId = q.questionId;
            const btn = document.getElementById("btnConfirmSwap");
            if (btn) btn.disabled = false;
        };
        container.appendChild(row);
        renderQuestionItemContent(row.querySelector('.render-content-target-alt'), q.questionContent, q.questionType);
    });
}

async function confirmSwap() {
    if (!selectedAlternativeId) return;
    const btn = document.getElementById("btnConfirmSwap");
    btn.disabled = true;

    const chkGlobal = document.getElementById("chkSwapGlobal");
    const isGlobal = chkGlobal ? chkGlobal.checked : false;

    try {
        await apiClient.post(`${API_BASE}/swap-question`, {
            paperId: swapContext.paperId,
            oldQuestionId: swapContext.questionId,
            newQuestionId: selectedAlternativeId,
            swapGlobal: isGlobal
        });
        showToast("Đổi câu hỏi thành công!", "success");
        bootstrap.Modal.getInstance(document.getElementById('alternativeQuestionsModal')).hide();
        await loadReviewData(currentReviewData.examId);
        const paper = currentReviewData.papers.find(p => p.paperId === swapContext.paperId);
        if (paper) renderPaperQuestions(paper);
    } catch (e) {
        showToast("Đổi câu hỏi thất bại.", "error");
    } finally {
        btn.disabled = false;
    }
}

function approveExam() {
    showConfirm("Sau khi phê duyệt, đề thi sẽ được công khai cho học sinh. Bạn đã kiểm tra kỹ mã đề?", "Phê duyệt đề thi", async () => {
        const btn = document.getElementById("btnApprove");
        btn.disabled = true;
        try {
            await apiClient.post(`${API_BASE}/approve/${currentReviewData.examId}`);
            showToast("Đề thi đã được phê duyệt!", "success");
            setTimeout(() => {
                if (CLASS_ID) window.location.href = `/Class/ExamListInClass/${CLASS_ID}`;
                else window.location.href = "/Exam";
            }, 1000);
        } catch (e) {
            showToast("Phê duyệt thất bại.", "error");
        } finally {
            btn.disabled = false;
        }
    });
}

function toLocalDateTimeString(dateStr) {
    if (!dateStr) return "";
    const d = new Date(dateStr);
    const offset = d.getTimezoneOffset();
    const local = new Date(d.getTime() - offset * 60 * 1000);
    return local.toISOString().slice(0, 16);
}

function toggleEditMode() {
    const readOnly = document.getElementById("infoReadOnly");
    const editForm = document.getElementById("infoEditForm");
    const isEditing = !editForm.classList.contains("d-none");

    if (isEditing) {
        editForm.classList.add("d-none");
        readOnly.classList.remove("d-none");
    } else {
        const d = currentReviewData;
        const titleGroup = document.getElementById("edit-title-group");

        // Cancelled (status===3): chỉ cho sửa thời gian, ẩn Title
        if (d.status === 3) {
            if (titleGroup) titleGroup.classList.add("d-none");
        } else {
            if (titleGroup) titleGroup.classList.remove("d-none");
        }

        document.getElementById("edit-title").value = d.title || "";
        document.getElementById("edit-visible-from").value = toLocalDateTimeString(d.visibleFrom);
        document.getElementById("edit-open-at").value = toLocalDateTimeString(d.openAt);
        document.getElementById("edit-close-at").value = toLocalDateTimeString(d.closeAt);

        readOnly.classList.add("d-none");
        editForm.classList.remove("d-none");
    }
}

async function saveExamInfo() {
    const btn = document.getElementById("btnSaveInfo");
    btn.disabled = true;

    const isCancelled = currentReviewData.status === 3;
    const title = document.getElementById("edit-title").value.trim();
    const visibleFrom = document.getElementById("edit-visible-from").value || null;
    const openAt = document.getElementById("edit-open-at").value || null;
    const closeAt = document.getElementById("edit-close-at").value || null;

    if (!isCancelled && !title) {
        showToast("Tên bài thi không được để trống.", "error");
        btn.disabled = false;
        return;
    }

    const payload = {
        visibleFrom: visibleFrom ? new Date(visibleFrom).toISOString() : null,
        openAt: openAt ? new Date(openAt).toISOString() : null,
        closeAt: closeAt ? new Date(closeAt).toISOString() : null,
    };

    // Cancelled: không gửi title (backend sẽ bỏ qua)
    if (!isCancelled) {
        payload.title = title;
    }

    try {
        await apiClient.put(`${API_BASE}/${currentReviewData.examId}/info`, payload);
        showToast("Cập nhật thông tin thành công!", "success");
        await loadReviewData(currentReviewData.examId);
    } catch (e) {
        const msg = e.data?.message || e.message || "Cập nhật thất bại.";
        showToast(msg, "error");
    } finally {
        btn.disabled = false;
    }
}

function cancelExam() {
    showConfirm("Bạn có chắc chắn muốn hủy đề thi này? Đề thi sẽ không hiển thị cho học sinh.", "Hủy đề thi", async () => {
        const btn = document.getElementById("btnCancel");
        btn.disabled = true;
        try {
            await apiClient.post(`${API_BASE}/cancel/${currentReviewData.examId}`);
            showToast("Đề thi đã được hủy!", "success");
            await loadReviewData(currentReviewData.examId);
        } catch (e) {
            const msg = e.data?.message || e.message || "Hủy đề thi thất bại.";
            showToast(msg, "error");
            // Reload to reflect any auto-transition (e.g. Published → InProgress)
            await loadReviewData(currentReviewData.examId);
        } finally {
            btn.disabled = false;
        }
    });
}

function restoreExam() {
    const d = currentReviewData;

    // Validate phía client trước khi gọi API
    const now = new Date();
    if (d.openAt && new Date(d.openAt) <= now) {
        showToast("Thời điểm mở đề đã qua. Vui lòng điều chỉnh thời gian trước khi khôi phục.", "error");
        return;
    }
    if (d.openAt && d.closeAt && d.duration > 0) {
        const windowMinutes = (new Date(d.closeAt) - new Date(d.openAt)) / 60000;
        if (windowMinutes < d.duration) {
            showToast(`Khoảng cách mở-đóng (${Math.round(windowMinutes)} phút) phải >= thời lượng làm bài (${d.duration} phút). Vui lòng điều chỉnh thời gian trước khi khôi phục.`, "error");
            return;
        }
    }

    showConfirm("Khôi phục đề thi này? Đề thi sẽ trở lại trạng thái Published.", "Khôi phục đề thi", async () => {
        const btn = document.getElementById("btnRestore");
        btn.disabled = true;
        try {
            await apiClient.post(`${API_BASE}/restore/${currentReviewData.examId}`);
            showToast("Đề thi đã được khôi phục!", "success");
            await loadReviewData(currentReviewData.examId);
        } catch (e) {
            const msg = e.data?.message || e.message || "Khôi phục thất bại.";
            showToast(msg, "error");
        } finally {
            btn.disabled = false;
        }
    });
}

function deleteExam() {
    showConfirm("⚠️ XÓA VĨNH VIỄN đề thi này?\nHành động này CỰC KỲ NGUY HIỂM và không thể hoàn tác. Tất cả mã đề và bản ghi liên quan sẽ bị xóa sạch!\n\nBạn đã chắc chắn chưa?", "Xác nhận xóa vĩnh viễn", async () => {
        const btn = document.getElementById("btnDelete");
        const originalIcon = "fas fa-trash me-1";
        const originalLabel = "XÓA ĐỀ THI";
        setButtonContent(btn, "fas fa-spinner fa-spin", "Đang xóa...");
        btn.disabled = true;
        try {
            await apiClient.delete(`${API_BASE}/${currentReviewData.examId}`);
            showToast("Đề thi đã được xóa!", "success");
            let redirectUrl = CLASS_ID ? `/Class/ExamListInClass/${CLASS_ID}` : "/Exam";
            setTimeout(() => {
                window.location.href = redirectUrl;
            }, 500);
        } catch (e) {
            const msg = e.data?.message || e.message || "Xóa đề thi thất bại.";
            showToast(msg, "error");
            btn.disabled = false;
            setButtonContent(btn, originalIcon, originalLabel);
        }
    });
}

function goBack() {
    if (CLASS_ID) {
        window.location.href = `/Class/ExamListInClass/${CLASS_ID}`;
    } else {
        history.back();
    }
}

// Helpers
function setText(id, val) { const el = document.getElementById(id); if (el) el.textContent = val || "---"; }

function clearElement(el) { while (el.firstChild) el.removeChild(el.firstChild); }

function setButtonContent(btn, iconClass, label) {
    clearElement(btn);
    if (iconClass) {
        const icon = document.createElement("i");
        icon.className = iconClass;
        btn.appendChild(icon);
    }
    btn.appendChild(document.createTextNode(" " + label));
}



function getDifficultyText(d) {
    if (d === 1) return "Nhận biết";
    if (d === 2) return "Thông hiểu";
    if (d === 3) return "Vận dụng";
    if (d === 4) return "Vận dụng cao";
    return "N/A";
}

function getDifficultyClass(d) {
    if (d === 1) return "badge-easy";
    if (d === 2) return "badge-medium";
    if (d === 3) return "badge-hard";
    if (d === 4) return "badge-advanced";
    return "bg-secondary";
}

function getTemplateContent(id) {
    const t = document.getElementById(id);
    if (!t) return document.createElement('div');
    return t.content.cloneNode(true).firstElementChild;
}
