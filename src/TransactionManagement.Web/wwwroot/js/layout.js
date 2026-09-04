/*
 * Application shell behaviour: sidebar visibility only.
 *
 * On wide screens the toggle collapses the sidebar and the content reclaims the space; on narrow
 * screens the sidebar slides over the content with a backdrop. Both states are expressed as
 * classes on <body> so all the actual styling stays in site.css.
 */
(function () {
    'use strict';

    var toggle = document.getElementById('sidebarToggle');
    var sidebar = document.getElementById('appSidebar');

    if (!toggle || !sidebar) {
        return;
    }

    var narrowScreen = window.matchMedia('(max-width: 991.98px)');
    var backdrop = null;

    function removeBackdrop() {
        if (backdrop) {
            backdrop.remove();
            backdrop = null;
        }
    }

    function closeOverlay() {
        document.body.classList.remove('sidebar-open');
        removeBackdrop();
    }

    function openOverlay() {
        document.body.classList.add('sidebar-open');

        backdrop = document.createElement('div');
        backdrop.className = 'sidebar-backdrop';
        backdrop.addEventListener('click', closeOverlay);
        document.body.appendChild(backdrop);
    }

    toggle.addEventListener('click', function () {
        if (narrowScreen.matches) {
            if (document.body.classList.contains('sidebar-open')) {
                closeOverlay();
            } else {
                openOverlay();
            }

            return;
        }

        document.body.classList.toggle('sidebar-collapsed');
    });

    // Leaving the narrow breakpoint must not strand the overlay state.
    narrowScreen.addEventListener('change', closeOverlay);

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeOverlay();
        }
    });
})();

/*
 * Document actions.
 *
 * Declared with data attributes rather than inline onclick/onsubmit handlers, so behaviour stays
 * out of the markup the same way styling does.
 */
(function () {
    'use strict';

    document.addEventListener('click', function (event) {
        if (event.target.closest('[data-print-trigger]')) {
            window.print();
        }
    });

    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (!window.confirm(form.dataset.confirm)) {
                event.preventDefault();
            }
        });
    });
})();
