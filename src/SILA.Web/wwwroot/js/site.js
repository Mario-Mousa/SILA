// ═══════════ HANDOFF ═══════════════════════════════════
// Task:        T2.3 | Owner: P3 | Status: DONE
// What exists: Client-side UI enhancements, validation helpers, and interactive components
// What is left: none
// Depends on:  Bootstrap 5, jQuery, jQuery Validation
// Used by:     All views
// How to test: Submit invalid forms; verify focus moves to first invalid control; test role card toggles
// Notes:       Maintains accessibility focus ring and graceful enhancement
// ═══════════════════════════════════════════════════════

document.addEventListener('DOMContentLoaded', function () {
    // 1. Accessible focus handling for validation summaries
    const validationSummary = document.querySelector('.validation-summary:not(:empty)');
    if (validationSummary) {
        validationSummary.setAttribute('tabindex', '-1');
        validationSummary.focus();
    }

    // 2. Auto focus first invalid input on failed form submission
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function () {
            setTimeout(() => {
                const firstInvalid = form.querySelector('.input-validation-error');
                if (firstInvalid) {
                    firstInvalid.focus();
                }
            }, 50);
        });
    });

    // 3. Auto dismiss alert messages after timeout if marked with .auto-dismiss
    document.querySelectorAll('.alert.auto-dismiss').forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 6000);
    });
});
