window.QuestionEditorUtils = (() => {
    'use strict';
    const toArray = (v) => Array.from(v || []);

    const getMathValue = (f) => {
        if (!f) {
            return '';
        }
        if (typeof f.getValue === 'function') {
            const l = f.getValue('latex');
            return typeof l === 'string' ? l : '';
        }
        if (typeof f.value === 'string') {
            return f.value;
        }
        return f.textContent || '';
    };

    const setMathValue = (f, v) => {
        if (!f) {
            return;
        }
        const target = (v || '');
        if (typeof f.setValue === 'function') {
            // Normalize for comparison: remove all whitespace AND ensure we compare with what MathLive WOULD return
            const current = (f.getValue('latex') || '').replace(/\s+/g, '');
            const targetNorm = target.replace(/\s+/g, '');
            if (current === targetNorm) {
                return;
            }

            // Ensure no hidden text nodes exist that MathLive might pick up
            while (f.firstChild) {
                f.removeChild(f.firstChild);
            }
            f.setValue(target, {
                silenceNotifications: true
            });
            return;
        }
        if (typeof f.value === 'string') {
            if (f.value === target) {
                return;
            }
            f.value = target;
            return;
        }
        if (f.textContent === target) {
            return;
        }
        f.textContent = target;
    };

    const getNumberedPlaceholders = (latex) => {
        if (typeof latex !== 'string' || !latex.trim()) {
            return [];
        }
        // Match \placeholder[n] but ignore those without [n]
        // Using global flag with matchAll
        const regex = /\\placeholder\s*\[(\d+)\]/g;
        const nums = new Set();
        let match;
        while ((match = regex.exec(latex)) !== null) {
            nums.add(parseInt(match[1], 10));
        }
        return Array.from(nums).sort((a, b) => a - b);
    };

    const getFrameLatex = (item) => {
        if (item._frameEditor) {
            return item._frameEditor.getValue();
        }
        const raw = item.querySelector('[data-frame-raw]');
        if (raw && !raw.classList.contains('d-none')) {
            return raw.value;
        }
        const editor = item.querySelector('[data-frame-editor]');
        return getMathValue(editor);
    };





    const parseLatexSegments = (latex) => {
        if (!latex || !latex.trim()) {
            return [];
        }
        let s = latex.trim();
        const dlMatch = s.match(/^\\displaylines\s*\{([\s\S]*)\}\s*$/);
        const inner = dlMatch ? dlMatch[1].trim() : s;

        const segments = [];
        let current = "";
        let envDepth = 0;
        let braceDepth = 0;

        for (let i = 0; i < inner.length; i++) {
            const char = inner[i];
            if (char === '\\') {
                if (inner.startsWith("begin", i + 1)) {
                    envDepth++;
                } else if (inner.startsWith("end", i + 1)) {
                    envDepth = Math.max(0, envDepth - 1);
                }
            }
            if (char === '{' && (i === 0 || inner[i - 1] !== '\\')) {
                braceDepth++;
            } else if (char === '}' && (i === 0 || inner[i - 1] !== '\\')) {
                braceDepth = Math.max(0, braceDepth - 1);
            }

            if (char === '\\' && inner[i + 1] === '\\' && envDepth === 0 && braceDepth === 0) {
                const trimmed = current.trim();
                if (trimmed) {
                    segments.push(trimmed);
                }
                current = "";
                i++;
                continue;
            }
            current += char;
        }
        const last = current.trim();
        if (last) {
            segments.push(last);
        }
        return segments.map((content, i) => ({
            index: i,
            content
        }));
    };

    const renderLatexInElement = (previewBox, content, options = {}) => {
        // Delegate xuống FibKatexRenderer (Shared/fib_katex_renderer.js).
        // Behavior teacher giữ nguyên (mode='teacher' → blank = <input readonly>).
        window.FibKatexRenderer.render(previewBox, content, {
            mode: 'teacher',
            placeholder: options.placeholder
        });
    };

    const setupTabbedEditor = (container, { onChange, onInsertPlaceholder } = {}) => {
        if (!container) return null;

        const btnEdit = container.querySelector('[data-tab-edit]');
        const btnView = container.querySelector('[data-tab-view]');
        const codeArea = container.querySelector('[data-editor-code]');
        const previewBox = container.querySelector('[data-editor-preview]');
        const btnInsert = container.querySelector('[data-editor-insert]');

        const render = () => {
            renderLatexInElement(previewBox, codeArea.value);
        };


        btnEdit?.addEventListener('click', () => {
            btnEdit.classList.add('active');
            btnView?.classList.remove('active');
            codeArea.style.display = 'block';
            previewBox.style.display = 'none';
            codeArea.focus();
        });

        btnView?.addEventListener('click', () => {
            btnView.classList.add('active');
            btnEdit?.classList.remove('active');
            codeArea.style.display = 'none';
            previewBox.style.display = 'block';
            render();
        });

        codeArea?.addEventListener('input', () => {
            if (onChange) onChange();
        });

        btnInsert?.addEventListener('click', () => {
            if (onInsertPlaceholder) {
                onInsertPlaceholder();
            } else {
                // Default insert logic if not provided
                const start = codeArea.selectionStart;
                const end = codeArea.selectionEnd;
                const matches = codeArea.value.match(/\\placeholder\[(\d+)\]/g) || [];
                const nextId = matches.length + 1;
                const textToInsert = `\\placeholder[${nextId}]{}`;
                codeArea.value = codeArea.value.substring(0, start) + textToInsert + codeArea.value.substring(end);
                codeArea.selectionStart = codeArea.selectionEnd = start + textToInsert.length;
                codeArea.focus();
                if (onChange) onChange();
            }
        });

        return {
            getValue: () => codeArea.value,
            setValue: (val) => {
                codeArea.value = val || '';
                if (previewBox.style.display === 'block') {
                    render();
                }
            },
            refreshPreview: render
        };
    };

    return {
        toArray,
        getMathValue,
        setMathValue,
        getNumberedPlaceholders,
        getFrameLatex,
        parseLatexSegments,
        renderLatexInElement,
        setupTabbedEditor
    };
})();

