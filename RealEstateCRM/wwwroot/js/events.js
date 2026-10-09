// CSP-friendly event delegation.
// Markup declares behaviour as data-onclick="editLead(5)", data-onsubmit="saveTask(event, 3)", etc.
// Instead of executing that text (which would need 'unsafe-inline' / 'unsafe-eval'), it is parsed as
// functionName(arg, ...) where each arg is a number, quoted string, true/false/null or the event itself,
// and the named global function is called.
(function () {
    'use strict';

    const CALL = /^([A-Za-z_$][\w$]*)\((.*)\)$/;
    const BUILTINS = { stopPropagation: function (ev) { ev.stopPropagation(); } };

    function parseArg(token, ev) {
        const t = token.trim();
        if (t === 'event') return ev;
        if (t === 'null') return null;
        if (t === 'true') return true;
        if (t === 'false') return false;
        if (/^-?\d+(\.\d+)?$/.test(t)) return Number(t);
        const quoted = /^'([^']*)'$|^"([^"]*)"$/.exec(t);
        if (quoted) return quoted[1] !== undefined ? quoted[1] : quoted[2];
        throw new Error('Unsupported handler argument: ' + t);
    }

    function dispatch(ev, attr) {
        if (!(ev.target instanceof Element)) return;
        const el = ev.target.closest('[' + attr + ']');
        if (!el) return;

        const match = CALL.exec(el.getAttribute(attr).trim());
        if (!match) return;

        if (BUILTINS[match[1]]) {
            BUILTINS[match[1]](ev);
            return;
        }

        const fn = window[match[1]];
        if (typeof fn !== 'function') {
            console.error('Unknown handler: ' + match[1]);
            return;
        }

        const rawArgs = match[2].trim();
        const args = rawArgs === '' ? [] : rawArgs.split(',').map(function (a) { return parseArg(a, ev); });
        const result = fn.apply(el, args);
        if (ev.type === 'submit' && result === false) ev.preventDefault();
    }

    ['click', 'input', 'change', 'submit'].forEach(function (type) {
        document.addEventListener(type, function (ev) { dispatch(ev, 'data-on' + type); });
    });
})();
