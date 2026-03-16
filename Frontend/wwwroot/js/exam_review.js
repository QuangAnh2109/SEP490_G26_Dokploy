/**
 * exam_review.js
 * Redesigned logic for Exam Review with Sidebar navigation.
 */

// Use global API_BASE_URL from site.js if available, else fallback
const API_BASE = (typeof API_BASE_URL !== 'undefined' ? API_BASE_URL : "https://localhost:7167") + "/api/assign-exam";
let currentReviewData = null;
let selectedAlternativeId = null;
let swapContext = {
    paperId: null,
    oldQuestionId: null
};

async function initReviewPage(examId) {
    try {
        await loadReviewData(examId);
    } catch (error) {
        console.error("Failed to initialize review page:", error);
        showToast("Không thể tải thông tin đề thi.", "error");
    }
}

async function loadReviewData(examId) {
    const response = await apiGet(`${API_BASE}/review/${examId}`);
    if (response.ok) {
        currentReviewData = await response.json();
        renderInfoView();
        renderPaperSidebar();
        renderOverview(); // Background render for All Questions view
        setupSidebarNavigation(); // Call after rendering to ensure items exist
    } else {
        throw new Error("API call failed");
    }
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

            const tr = document.createElement("tr");
            tr.innerHTML = `
                <th class="fw-semibold">${row.chapterName}</th>
                <td>${row.recognize}</td>
                <td>${row.understand}</td>
                <td>${row.apply}</td>
                <td>${row.advancedApply}</td>
                <td class="fw-bold">${row.total}</td>
            `;
            matrixBody.appendChild(tr);
        });

        // Totals row
        const footer = document.createElement("tr");
        footer.className = "table-light fw-bold";
        footer.innerHTML = `
            <th>Tổng cộng</th>
            <td>${totals.nb}</td>
            <td>${totals.th}</td>
            <td>${totals.vd}</td>
            <td>${totals.vdc}</td>
            <td class="text-primary">${totals.total}</td>
        `;
        matrixBody.appendChild(footer);
    } else {
        matrixBody.innerHTML = `<tr><td colspan="6" class="text-center py-3 text-muted">Đề thi không sử dụng ma trận blueprint.</td></tr>`;
    }
}

function renderPaperSidebar() {
    const container = document.getElementById("paper-nav-list");
    if (!container) return;
    container.innerHTML = "";

    currentReviewData.papers.forEach(p => {
        const btn = document.createElement("button");
        btn.className = "nav-item-custom sidebar-nav paper-item";
        btn.innerHTML = `<i class="far fa-file-alt"></i> Mã đề ${p.code}`;
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
        container.innerHTML = `<div class="text-center py-5 text-muted">Không có câu hỏi nào.</div>`;
        return;
    }

    uniqueQuestions.forEach((q, idx) => {
        const item = document.createElement("div");
        item.className = "question-item";
        item.innerHTML = `
            <div class="d-flex justify-content-between align-items-start mb-2">
                <span class="fw-bold text-primary">Câu hỏi ${idx + 1}</span>
                <div>
                    ${q.papers.map(p => `<span class="paper-tag">Mã đề ${p}</span>`).join('')}
                    <span class="difficulty-badge ${getDifficultyClass(q.difficulty)}">
                        ${getDifficultyText(q.difficulty)}
                    </span>
                </div>
            </div>
            <div class="render-content-target" id="overview-q-${q.questionId}"></div>
            <div class="text-muted small mt-2">Chương: ${q.chapterName}</div>
        `;
        container.appendChild(item);
        renderQuestionItemContent(item.querySelector('.render-content-target'), q.contentLatex, q.questionType);
    });
}

function renderPaperQuestions(paper) {
    setText("paper-detail-title", `Mã đề: ${paper.code}`);

    const container = document.getElementById("paperQuestionsList");
    if (!container) return;
    container.innerHTML = "";

    paper.questions.forEach((q, idx) => {
        const item = document.createElement("div");
        item.className = "question-item";
        item.innerHTML = `
            <div class="d-flex justify-content-between align-items-start mb-2">
                <span class="fw-bold">Câu ${idx + 1}</span>
                <div class="d-flex align-items-center">
                    <span class="difficulty-badge ${getDifficultyClass(q.difficulty)} me-2">
                        ${getDifficultyText(q.difficulty)}
                    </span>
                    ${currentReviewData.status === 0 ? `
                    <button class="btn btn-sm btn-outline-primary" onclick="openSwapModal(${paper.paperId}, ${q.questionId})">
                        <i class="fas fa-exchange-alt me-1"></i> Đổi câu hỏi
                    </button>` : ''}
                </div>
            </div>
            <div class="render-content-target" id="paper-q-${q.questionId}"></div>
        `;
        container.appendChild(item);
        renderQuestionItemContent(item.querySelector('.render-content-target'), q.contentLatex, q.questionType);
    });
}

function renderQuestionItemContent(container, content, type) {
    try {
        const isJson = content.trim().startsWith('{') && content.trim().endsWith('}');
        if (isJson || type === 'FillInBlank') {
            const data = JSON.parse(content);
            container.innerHTML = `
                <div class="mb-2"><math-span class="latex-content">${data.stem || ''}</math-span></div>
                <div><math-field read-only class="w-100">${data.frame || ''}</math-field></div>
            `;
        } else {
            container.innerHTML = `<math-span class="latex-content">${content}</math-span>`;
        }
    } catch (e) {
        container.innerHTML = `<div class="latex-content">${content}</div>`;
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
    list.innerHTML = `<div class="text-center py-4"><i class="fas fa-spinner fa-spin"></i> Đang tìm câu hỏi thay thế...</div>`;

    try {
        const response = await apiGet(`${API_BASE}/papers/${paperId}/questions/${questionId}/alternatives`);
        if (response.ok) {
            const alternatives = await response.json();
            renderAlternatives(alternatives);
        } else {
            list.innerHTML = `<div class="alert alert-danger">Lỗi khi tải dữ liệu.</div>`;
        }
    } catch (e) {
        list.innerHTML = `<div class="alert alert-danger">Lỗi: ${e.message}</div>`;
    }
}

function renderAlternatives(list) {
    const container = document.getElementById("alternativesList");
    if (!container) return;
    container.innerHTML = "";

    if (list.length === 0) {
        container.innerHTML = `<div class="text-center py-4 text-muted">Không tìm thấy câu hỏi phù hợp.</div>`;
        return;
    }

    list.forEach(q => {
        const div = document.createElement("div");
        div.className = "alternative-item";
        div.innerHTML = `
            <div class="d-flex justify-content-between small text-muted mb-1">
                <span>ID: ${q.questionId} | ${q.chapterName}</span>
                <span class="difficulty-badge ${getDifficultyClass(q.difficulty)}">${getDifficultyText(q.difficulty)}</span>
            </div>
            <div class="render-content-target-alt" style="font-size:0.95rem"></div>
        `;
        div.onclick = () => {
            container.querySelectorAll(".alternative-item").forEach(i => i.classList.remove("selected"));
            div.classList.add("selected");
            selectedAlternativeId = q.questionId;
            const btn = document.getElementById("btnConfirmSwap");
            if (btn) btn.disabled = false;
        };
        container.appendChild(div);
        renderQuestionItemContent(div.querySelector('.render-content-target-alt'), q.questionContent, q.questionType);
    });
}

async function confirmSwap() {
    if (!selectedAlternativeId) return;
    const btn = document.getElementById("btnConfirmSwap");
    btn.disabled = true;
    try {
        const response = await apiPost(`${API_BASE}/swap-question`, {
            paperId: swapContext.paperId,
            oldQuestionId: swapContext.oldQuestionId,
            newQuestionId: selectedAlternativeId
        });

        if (response.ok) {
            showToast("Đổi câu hỏi thành công!", "success");
            bootstrap.Modal.getInstance(document.getElementById('alternativeQuestionsModal')).hide();
            await loadReviewData(currentReviewData.examId);
            // Re-render current paper view
            const paper = currentReviewData.papers.find(p => p.paperId === swapContext.paperId);
            if (paper) renderPaperQuestions(paper);
        } else {
            showToast("Đổi câu hỏi thất bại.", "error");
        }
    } catch (e) {
        showToast("Lỗi kết nối.", "error");
    } finally {
        btn.disabled = false;
    }
}

async function approveExam() {
    if (!confirm("Sau khi phê duyệt, đề thi sẽ được công khai cho học sinh. Bạn đã kiểm tra kỹ mã đề?")) return;
    const btn = document.getElementById("btnApprove");
    btn.disabled = true;
    try {
        const response = await apiPost(`${API_BASE}/approve/${currentReviewData.examId}`);
        if (response.ok) {
            showToast("Đề thi đã được phê duyệt!", "success");
            setTimeout(() => { window.location.href = `/Course/ExamListInCourse/${CLASS_ID || ''}`; }, 1000);
        } else {
            showToast("Phê duyệt thất bại.", "error");
        }
    } catch (e) {
        showToast("Lỗi kết nối.", "error");
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

function getToken() {
    return localStorage.getItem('jwtToken') || sessionStorage.getItem('jwtToken') || '';
}

async function apiGet(url) {
    return await fetch(url, { headers: { "Authorization": `Bearer ${getToken()}` } });
}

async function apiPost(url, data) {
    return await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", "Authorization": `Bearer ${getToken()}` },
        body: JSON.stringify(data)
    });
}

// Re-use site.js toast if possible
function showToast(msg, type = "success") {
    if (window.showToast) {
        window.showToast(msg, type);
    } else if (window.toastr) {
        window.toastr[type](msg);
    } else {
        alert(msg);
    }
}
