window.QuestionEditorUtils = (() => {
    'use strict';
    const toArray = (v) => Array.from(v || []);

    const getMathValue = (f) => {
        if (!f) return '';
        if (typeof f.getValue === 'function') {
            const l = f.getValue('latex');
            return typeof l === 'string' ? l : '';
        }
        if (typeof f.value === 'string') return f.value;
        return f.textContent || '';
    };

    const setMathValue = (f, v) => {
        if (!f) return;
        const target = (v || '');
        if (typeof f.setValue === 'function') {
            // Normalize for comparison
            const current = (f.getValue('latex') || '').replace(/\s+/g, '');
            const targetNorm = target.replace(/\s+/g, '');
            if (current === targetNorm) return;

            // Critical: Ensure no hidden text nodes exist that MathLive might pick up
            while (f.firstChild) f.removeChild(f.firstChild);
            f.setValue(target, { silenceNotifications: true });
            return;
        }
        if (typeof f.value === 'string') { if (f.value === target) return; f.value = target; return; }
        if (f.textContent === target) return;
        f.textContent = target;
    };

    const getNumberedPlaceholders = (latex) => {
        const s = typeof latex === 'string' ? latex : '';
        const matches = s.matchAll(/\\placeholder\s*\[(\d+)\]/g);
        const nums = new Set();
        for (const m of matches) { nums.add(parseInt(m[1], 10)); }
        return Array.from(nums).sort((a, b) => a - b);
    };

    const getFrameLatex = (item) => {
        const raw = item.querySelector('[data-frame-raw]');
        if (raw && !raw.classList.contains('d-none')) { return raw.value; }
        const editor = item.querySelector('[data-frame-editor]');
        return getMathValue(editor);
    };

    const setupPairToggle = (mathField, rawTextarea, onChange) => {
        if (!mathField || !rawTextarea) return { setRenderMode: () => { } };
        let mirroring = false;
        let currentRenderOn = !mathField.classList.contains('d-none');

        const setRenderMode = (renderOn) => {
            if (mirroring || renderOn === currentRenderOn) return;
            mirroring = true;
            currentRenderOn = renderOn;
            try {
                if (renderOn) {
                    setMathValue(mathField, rawTextarea.value);
                    mathField.classList.remove('d-none');
                    rawTextarea.classList.add('d-none');
                } else {
                    rawTextarea.value = getMathValue(mathField);
                    mathField.classList.add('d-none');
                    rawTextarea.classList.remove('d-none');
                    if (document.activeElement !== rawTextarea) rawTextarea.focus();
                }
                if (onChange) onChange();
            } finally {
                setTimeout(() => { mirroring = false; }, 100);
            }
        };

        mathField.addEventListener('input', () => {
            if (mirroring) return;
            mirroring = true;
            rawTextarea.value = getMathValue(mathField);
            if (onChange) onChange();
            setTimeout(() => { mirroring = false; }, 10);
        });

        rawTextarea.addEventListener('input', () => {
            if (mirroring) return;
            mirroring = true;
            if (onChange) onChange();
            setTimeout(() => { mirroring = false; }, 10);
        });

        return { setRenderMode };
    };

    const parseLatexSegments = (latex) => {
        if (!latex || !latex.trim()) return [];
        let s = latex.trim();
        const dlMatch = s.match(/^\\displaylines\s*\{([\s\S]*)\}$/);
        const inner = dlMatch ? dlMatch[1] : s;
        const segments = [];
        let current = "", envDepth = 0, braceDepth = 0;

        for (let i = 0; i < inner.length; i++) {
            const char = inner[i];
            if (char === '\\') {
                if (inner.startsWith("begin", i + 1)) { envDepth++; }
                else if (inner.startsWith("end", i + 1)) { envDepth = Math.max(0, envDepth - 1); }
            }
            if (char === '{' && (i === 0 || inner[i - 1] !== '\\')) { braceDepth++; }
            else if (char === '}' && (i === 0 || inner[i - 1] !== '\\')) { braceDepth = Math.max(0, braceDepth - 1); }

            if (char === '\\' && inner[i + 1] === '\\' && envDepth === 0 && braceDepth === 0) {
                const trimmed = current.trim();
                if (trimmed) segments.push(trimmed);
                current = ""; i++; continue;
            }
            current += char;
        }
        const last = current.trim();
        if (last) segments.push(last);
        return segments.map((content, i) => ({ index: i, content }));
    };

    return { toArray, getMathValue, setMathValue, getNumberedPlaceholders, getFrameLatex, setupPairToggle, parseLatexSegments };
})();
