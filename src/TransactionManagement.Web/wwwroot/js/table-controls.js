/*
 * List controls.
 *
 * Gives the length and search inputs the responsiveness people expect from a client-side table,
 * while the query itself stays on the server: changing either one simply re-submits the GET form,
 * so filtering, ordering and paging keep happening in SQL over the full result set rather than
 * over whatever subset the browser happens to be holding.
 */
(function () {
    'use strict';

    var SEARCH_DEBOUNCE_MS = 450;

    var form = document.getElementById('table-controls');

    if (!form) {
        return;
    }

    function resetToFirstPage() {
        // Any change to length or search invalidates the current page number.
        var pageNumber = form.querySelector('input[name="PageNumber"]');

        if (!pageNumber) {
            pageNumber = document.createElement('input');
            pageNumber.type = 'hidden';
            pageNumber.name = 'PageNumber';
            form.appendChild(pageNumber);
        }

        pageNumber.value = '1';
    }

    function submit() {
        resetToFirstPage();
        form.submit();
    }

    form.querySelectorAll('[data-auto-submit]').forEach(function (control) {
        control.addEventListener('change', submit);
    });

    form.querySelectorAll('[data-auto-submit-delayed]').forEach(function (control) {
        var timer = null;
        var initialValue = control.value;

        control.addEventListener('input', function () {
            window.clearTimeout(timer);

            timer = window.setTimeout(function () {
                if (control.value !== initialValue) {
                    submit();
                }
            }, SEARCH_DEBOUNCE_MS);
        });

        // A submit triggered by typing reloads the page; put the caret back where it was so the
        // user can keep typing without reaching for the mouse.
        if (initialValue.length > 0 && control.type !== 'hidden') {
            control.focus();
            control.setSelectionRange(initialValue.length, initialValue.length);
        }
    });
})();
