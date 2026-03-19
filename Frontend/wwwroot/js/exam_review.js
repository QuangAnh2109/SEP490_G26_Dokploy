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
    setText("info-status-badge", "Chờ duyệt");

    // Info Cards
    setText("card-subject-code", d.subjectCode);
    setText("card-exam-title", d.title);
    setText("card-total-questions", d.totalQuestions + " câu");
    setText("card-duration", d.duration + " phút");
    setText("card-open-at", formatDateTime(d.openAt));
    setText("card-close-at", formatDateTime(d.closeAt));
    setText("card-teacher-name", d.teacherName);
    setText("card-updated-at", formatDateTime(d.updatedAtUtc));
    setText("card-description", d.description || "Không có ghi chú.");

    // Handle Status-based UI
    const isPending = d.status === 0;
    const approveBtn = document.getElementById("btnApprove");
    if (approveBtn) {
        approveBtn.style.display = isPending ? "block" : "none";
    }

    const badgeEl = document.getElementById("info-status-badge");
    if (badgeEl) {
        if (d.status === 1) {
            badgeEl.textContent = "Công khai";
            badgeEl.className = "meta-badge badge-public";
        } else if (d.status === 0) {
            badgeEl.textContent = "Chờ duyệt";
            badgeEl.className = "meta-badge badge-pending";
        } else {
            badgeEl.textContent = "Khác";
            badgeEl.className = "meta-badge badge-unknown";
        }
    }

    // Matrix
    const matrixBody = document.getElementById("matrix-body");
    if (!matrixBody) return;
    matrixBody.innerHTML = "";
    
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
    container.innerHTML = "";

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
    container.innerHTML = "";

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
        tagsContainer.innerHTML = '';
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
        renderQuestionItemContent(item.querySelector('.render-content-target'), q.contentLatex, q.questionType);
        renderAnswersAndExplanation(`answers-overview-${idx}`, `exp-overview-${idx}`, q);
    });
}

function renderPaperQuestions(paper) {
    setText("paper-detail-title", `Mã đề: ${paper.code}`);

    const container = document.getElementById("paperQuestionsList");
    if (!container) return;
    container.innerHTML = "";

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
        const isJson = content.trim().startsWith('{') && content.trim().endsWith('}');
        container.innerHTML = "";
        if (isJson || type === 'FillInBlank') {
            const data = JSON.parse(content);
            if (window.QuestionEditorUtils) {
                const tpl = getTemplateContent('questionContentJsonTemplate');
                container.appendChild(tpl);
                window.QuestionEditorUtils.renderLatexInElement(container.querySelector('.stem-container'), data.stem || '');
                window.QuestionEditorUtils.renderLatexInElement(container.querySelector('.frame-container'), data.frame || '');
            } else {
                const fbTpl = getTemplateContent('questionContentFallbackJsonTemplate');
                fbTpl.querySelector('.stem-content').innerHTML = data.stem || '';
                fbTpl.querySelector('.frame-content').innerHTML = data.frame || '';
                container.appendChild(fbTpl);
            }
        } else {
            const tpl = getTemplateContent('questionContentStandardTemplate');
            container.appendChild(tpl);
            if (window.QuestionEditorUtils) {
                window.QuestionEditorUtils.renderLatexInElement(tpl, content);
            } else {
                tpl.innerHTML = content;
            }
        }
    } catch (e) {
        const errTpl = getTemplateContent('questionContentErrorTemplate');
        errTpl.textContent = `Lỗi hiển thị nội dung: ${e.message}`;
        container.innerHTML = "";
        container.appendChild(errTpl);
    }
}

function renderAnswersAndExplanation(answersContainerId, explanationContainerId, q) {
    const ansContainer = document.getElementById(answersContainerId);
    const expContainer = document.getElementById(explanationContainerId);
    if (!ansContainer || !expContainer) return;

    ansContainer.innerHTML = '';
    if (q.answers && q.answers.length > 0) {
        const isMultipleChoice = q.questionType === 'MultipleChoice' || q.questionType === 'MultipleResponse';
        const labels = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'];
        
        const ansLabel = document.createElement("strong");
        ansLabel.className = "d-block mb-2 text-dark";
        ansLabel.textContent = "Đáp án:";
        ansContainer.appendChild(ansLabel);

        q.answers.forEach((ans, idx) => {
            try {
                const label = isMultipleChoice ? (labels[idx] || '?') : `Ô trống ${idx+1}`;
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
                    renderTarget.innerHTML = safeContent;
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
            const data = JSON.parse(q.contentLatex);
            explanationText = data.explanation || "";
        }
    } catch(e) {}

    const expContent = expContainer.querySelector('.explanation-content');
    if (explanationText.trim()) {
        if (window.QuestionEditorUtils) {
            window.QuestionEditorUtils.renderLatexInElement(expContent, explanationText);
        } else {
            expContent.innerHTML = explanationText;
        }
    } else {
        expContainer.style.display = 'none';
    }
}

// Modal and Swapping Logic
async function openSwapModal(paperId, questionId) {
    swapContext = { paperId, questionId };
    selectedAlternativeId = null;
    const btn = document.getElementById("btnConfirmSwap");
    if (btn) btn.disabled = true;

    const modalEl = document.getElementById('alternativeQuestionsModal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);
    modal.show();

    const list = document.getElementById("alternativesList");
    if (!list) return;
    list.innerHTML = "";
    list.appendChild(getTemplateContent('alternativesLoadingTemplate'));

    try {
        const alternatives = await apiClient.get(`${API_BASE}/papers/${paperId}/questions/${questionId}/alternatives`);
        renderAlternatives(alternatives);
    } catch (e) {
        const errTpl = getTemplateContent('alternativesErrorTemplate');
        errTpl.textContent = `Lỗi: ${e.message || 'Lỗi khi tải dữ liệu.'}`;
        list.innerHTML = "";
        list.appendChild(errTpl);
    }
}

function renderAlternatives(list) {
    const container = document.getElementById("alternativesList");
    if (!container) return;
    container.innerHTML = "";

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
    try {
        await apiClient.post(`${API_BASE}/swap-question`, {
            paperId: swapContext.paperId,
            oldQuestionId: swapContext.oldQuestionId,
            newQuestionId: selectedAlternativeId
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

async function approveExam() {
    if (!confirm("Sau khi phê duyệt, đề thi sẽ được công khai cho học sinh. Bạn đã kiểm tra kỹ mã đề?")) return;
    const btn = document.getElementById("btnApprove");
    btn.disabled = true;
    try {
        await apiClient.post(`${API_BASE}/approve/${currentReviewData.examId}`);
        showToast("Đề thi đã được phê duyệt!", "success");
        setTimeout(() => { window.location.href = `/Course/ExamListInCourse/${CLASS_ID || ''}`; }, 1000);
    } catch (e) {
        showToast("Phê duyệt thất bại.", "error");
    } finally {
        btn.disabled = false;
    }
}

// Helpers
function setText(id, val) { const el = document.getElementById(id); if (el) el.textContent = val || "---"; }

function formatDateTime(iso) {
    if (!iso) return "---";
    const d = new Date(iso);
    const pad = n => String(n).padStart(2, '0');
    return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
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
