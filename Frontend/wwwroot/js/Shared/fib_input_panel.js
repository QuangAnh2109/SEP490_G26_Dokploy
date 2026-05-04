// Singleton input panel cho FIB student-side.
// API qua window.FibInputPanel: open / close / isOpen / flushOpen.
// Phụ thuộc:
//   - DOM nodes #fib-input-panel, #fib-student-input, #fib-custom-keyboard,
//     #fib-active-blank-num, #fib-btn-confirm/cancel/clear (xem _QuestionArea.cshtml)
//   - window.katex (KaTeX core, load ở view qua CDN)
//   - window.inputTypeMathMapping (site.js)
//
// Usage:
//   FibInputPanel.open({
//     answer: { questionAnswerId, content, inputTypes },
//     currentValue: '\\sqrt{2}',
//     onConfirm: (latex) => { /* save + re-render frame */ },
//     onCancel:  () => { /* clear active highlight */ },
//   });
//   FibInputPanel.flushOpen();  // auto-confirm khi caller chuyển blank/câu/nộp bài

window.FibInputPanel = (() => {
    'use strict';

    const PLACEHOLDER_REGEX = /\\placeholder\[(\d+)\]/;
    const NUMPAD_TYPE_NAMES = new Set(['Số tự nhiên', 'Số nguyên']);

    let dom = null;
    let bound = false;
    let activeOnConfirm = null;
    let activeOnCancel = null;

    function ensureDom() {
        if (dom) return dom;
        const panel = document.getElementById('fib-input-panel');
        if (!panel) throw new Error('FibInputPanel: #fib-input-panel not found in DOM');
        dom = {
            panel,
            mathField: document.getElementById('fib-student-input'),
            keyboard:  document.getElementById('fib-custom-keyboard'),
            blankLabel: document.getElementById('fib-active-blank-num'),
            btnConfirm: document.getElementById('fib-btn-confirm'),
            btnCancel:  document.getElementById('fib-btn-cancel'),
            btnClear:   document.getElementById('fib-btn-clear'),
        };
        if (!bound) {
            dom.btnConfirm.addEventListener('click', onConfirmClick);
            dom.btnCancel.addEventListener('click', onCancelClick);
            dom.btnClear.addEventListener('click', onClearClick);
            bound = true;
        }
        return dom;
    }

    function extractBlankId(answerContent) {
        const m = (answerContent || '').match(PLACEHOLDER_REGEX);
        return m ? m[1] : '?';
    }

    function open({ answer, currentValue, onConfirm, onCancel }) {
        const d = ensureDom();
        activeOnConfirm = onConfirm || null;
        activeOnCancel  = onCancel  || null;

        d.blankLabel.textContent = extractBlankId(answer && answer.content);

        const value = currentValue || '';
        if (typeof d.mathField.setValue === 'function') {
            d.mathField.setValue(value, { silenceNotifications: true });
        } else {
            d.mathField.value = value;
        }
        setTimeout(() => d.mathField.focus(), 60);

        buildCustomKeyboard(d.keyboard, answer);
        d.panel.removeAttribute('hidden');
    }

    function close() {
        if (!dom) return;
        dom.panel.setAttribute('hidden', '');
        if (typeof dom.mathField.setValue === 'function') {
            dom.mathField.setValue('', { silenceNotifications: true });
        } else {
            dom.mathField.value = '';
        }
        dom.keyboard.innerHTML = '';
        activeOnConfirm = null;
        activeOnCancel = null;
    }

    function isOpen() {
        return !!(dom && !dom.panel.hasAttribute('hidden'));
    }

    // Auto-confirm khi caller chuyển blank/câu/nộp bài mà không bắt user bấm "Xác nhận".
    // Idempotent: no-op khi panel đóng.
    function flushOpen() {
        if (!isOpen()) return;
        const cb = activeOnConfirm;
        const latex = readMathFieldValue();
        close();
        if (cb) cb(latex);
    }

    function onConfirmClick() {
        const cb = activeOnConfirm;
        const latex = readMathFieldValue();
        close();
        if (cb) cb(latex);
    }

    function onCancelClick() {
        const cb = activeOnCancel;
        close();
        if (cb) cb();
    }

    function onClearClick() {
        if (!dom) return;
        if (typeof dom.mathField.setValue === 'function') {
            dom.mathField.setValue('', { silenceNotifications: true });
        } else {
            dom.mathField.value = '';
        }
        dom.mathField.focus();
    }

    function readMathFieldValue() {
        if (!dom) return '';
        if (typeof dom.mathField.getValue === 'function') {
            return (dom.mathField.getValue('latex') || '').trim();
        }
        return (dom.mathField.value || '').trim();
    }

    // ────────────────── Keyboard ──────────────────
    function buildCustomKeyboard(kb, answer) {
        kb.innerHTML = '';
        const types = (answer && answer.inputTypes) || [];

        const typeLabel = document.createElement('p');
        typeLabel.className = 'fib-keyboard-type-label';
        typeLabel.innerHTML = '<strong>Loại dữ liệu:</strong> ' +
            (types.length ? types.map(t => t.name).join(', ') : '(không giới hạn)');
        kb.appendChild(typeLabel);

        const isNaturalOnly = types.length > 0 && types.every(t => NUMPAD_TYPE_NAMES.has(t.name));
        const allowNegative = types.some(t => t.name === 'Số nguyên');

        if (isNaturalOnly) {
            const hint = document.createElement('p');
            hint.className = 'text-muted small mb-2';
            hint.innerHTML = allowNegative
                ? '<i class="bi bi-info-circle me-1"></i>Chỉ nhập số nguyên (có thể âm). Vd: -5, 0, 12'
                : '<i class="bi bi-info-circle me-1"></i>Chỉ nhập số nguyên dương. Vd: 5, 12, 100';
            kb.appendChild(hint);

            const grid = document.createElement('div');
            grid.className = 'fib-numpad';
            '1234567890'.split('').forEach(d => grid.appendChild(makeKeyBtn(d, d, false)));
            kb.appendChild(grid);

            const extra = document.createElement('div');
            extra.className = 'fib-numpad-extra';
            if (allowNegative) extra.appendChild(makeKeyBtn('-', '-', false));
            extra.appendChild(makeKeyBtn('⌫', null, false, mfBackspace));
            kb.appendChild(extra);
            return;
        }

        types.forEach(it => {
            const symbols = window.inputTypeMathMapping ? window.inputTypeMathMapping[it.name] : null;
            if (!Array.isArray(symbols) || symbols.length === 0) {
                const note = document.createElement('div');
                note.className = 'small text-muted mb-2';
                const setBadge = it.groupType === 'Sets'
                    ? ' <span class="badge bg-light text-dark border">Tập hợp</span>' : '';
                note.innerHTML = `<i class="bi bi-tag-fill me-1"></i>${escapeHtml(it.name)}${setBadge}`;
                kb.appendChild(note);
                return;
            }

            const row = document.createElement('div');
            row.className = 'fib-keyboard-row';

            const label = document.createElement('span');
            label.className = 'fib-keyboard-group-label';
            label.textContent = it.name;
            row.appendChild(label);

            const btnRow = document.createElement('div');
            btnRow.style.cssText = 'display:flex;flex-wrap:wrap;';
            symbols.forEach(latexStr => btnRow.appendChild(makeKeyBtn(null, latexStr, true)));
            row.appendChild(btnRow);
            kb.appendChild(row);
        });

        const utilRow = document.createElement('div');
        utilRow.style.cssText = 'display:flex;gap:5px;margin-top:6px;';
        utilRow.appendChild(makeKeyBtn('⌫ Xóa ký tự', null, false, mfBackspace));
        kb.appendChild(utilRow);
    }

    function makeKeyBtn(textFallback, latexStr, renderAsKatex, customOnClick) {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-outline-secondary btn-sm fib-key-btn';
        if (renderAsKatex && latexStr && window.katex) {
            const preview = document.createElement('span');
            const display = latexStr.replace(/\\placeholder\[\d+\]\{\}/g, '\\square');
            try { window.katex.render(display, preview, { throwOnError: false, trust: true, strict: false }); }
            catch (_) { preview.textContent = latexStr; }
            btn.appendChild(preview);
        } else {
            btn.textContent = textFallback || latexStr || '';
        }
        btn.addEventListener('mousedown', e => e.preventDefault());
        btn.addEventListener('click', customOnClick || (() => insertSymbol(latexStr)));
        return btn;
    }

    function insertSymbol(latex) {
        if (!latex || !dom) return;
        dom.mathField.focus();
        if (typeof dom.mathField.insert === 'function') dom.mathField.insert(latex);
        else dom.mathField.value = (dom.mathField.value || '') + latex;
    }

    function mfBackspace() {
        if (!dom) return;
        dom.mathField.focus();
        if (typeof dom.mathField.executeCommand === 'function') {
            dom.mathField.executeCommand('deleteBackward');
        } else {
            dom.mathField.value = (dom.mathField.value || '').slice(0, -1);
        }
    }

    function escapeHtml(s) {
        return String(s).replace(/[&<>"']/g, c => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[c]));
    }

    return { open, close, isOpen, flushOpen };
})();
