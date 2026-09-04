/*
 * Master-detail grid behaviour.
 *
 * This file is responsible for UI only: adding a row, removing a row, the product quick-search and
 * the running totals shown in the summary panel and the action bar. It enforces no business rule.
 * Every rule that matters -- quantity greater than zero, at least one active line, detail
 * ownership, date limits -- is enforced again on the server, which is the only place a rule can
 * actually be relied upon.
 */
(function () {
    'use strict';

    var MAX_SEARCH_RESULTS = 8;

    var grid = document.getElementById('detail-grid');
    var template = document.getElementById('detail-row-template');
    var addButton = document.getElementById('add-detail-row');
    var form = document.getElementById('transaction-form');

    if (!grid || !template || !addButton || !form) {
        return;
    }

    var body = grid.querySelector('tbody');
    var transactionDateInput = document.querySelector('input[name="TransactionDate"]');

    var searchInput = document.getElementById('product-search');
    var searchResults = document.getElementById('product-search-results');

    /* ---------------------------------------------------------------------------------------
       Product catalogue.

       Read from the row template's <select> rather than serialised separately into the page, so
       there is exactly one copy of the product list in the markup and the two can never drift.
       --------------------------------------------------------------------------------------- */
    var products = Array.prototype
        .slice.call(template.content.querySelectorAll('select option'))
        .filter(function (option) { return option.value !== ''; })
        .map(function (option) {
            return { id: option.value, text: option.textContent.trim() };
        });

    function newRowKey() {
        if (window.crypto && typeof window.crypto.randomUUID === 'function') {
            return window.crypto.randomUUID().replace(/-/g, '');
        }

        return 'r' + Date.now().toString(16) + Math.floor(Math.random() * 1e6).toString(16);
    }

    function today() {
        var now = new Date();
        var month = String(now.getMonth() + 1).padStart(2, '0');
        var day = String(now.getDate()).padStart(2, '0');

        return now.getFullYear() + '-' + month + '-' + day;
    }

    function defaultDetailDate() {
        return (transactionDateInput && transactionDateInput.value) || today();
    }

    function addRow(productId) {
        var key = newRowKey();
        var markup = template.innerHTML.split('__KEY__').join(key);
        var host = document.createElement('tbody');

        host.innerHTML = markup.trim();

        var row = host.querySelector('tr');
        var dateInput = row.querySelector('input[type="date"]');
        var productSelect = row.querySelector('select');

        if (dateInput && !dateInput.value) {
            dateInput.value = defaultDetailDate();
        }

        if (productId && productSelect) {
            productSelect.value = productId;
        }

        body.appendChild(row);
        recalculateTotals();

        // Land the cursor where the user's next keystroke belongs.
        var focusTarget = productId ? row.querySelector('.detail-quantity') : productSelect;

        if (focusTarget) {
            focusTarget.focus();
            if (focusTarget.select) {
                focusTarget.select();
            }
        }

        return row;
    }

    function removeRow(button) {
        var row = button.closest('tr.detail-row');

        if (!row) {
            return;
        }

        // Rows are keyed, not numbered, so removing one never disturbs the others.
        row.parentNode.removeChild(row);
        recalculateTotals();
    }

    function parseNumber(value) {
        var parsed = parseFloat(value);

        return isNaN(parsed) ? 0 : parsed;
    }

    function setText(selector, text) {
        document.querySelectorAll(selector).forEach(function (element) {
            element.textContent = text;
        });
    }

    function recalculateTotals() {
        var rows = body.querySelectorAll('tr.detail-row');
        var totalQuantity = 0;
        var totalAmount = 0;
        var activeLines = 0;

        rows.forEach(function (row) {
            var active = row.querySelector('.detail-active');

            if (!active || !active.checked) {
                return;
            }

            activeLines++;
            totalQuantity += parseNumber(row.querySelector('.detail-quantity').value);
            totalAmount += parseNumber(row.querySelector('.detail-amount').value);
        });

        setText('[data-total-lines]', String(rows.length));
        setText('[data-active-lines]', String(activeLines));
        setText('[data-total-quantity]', totalQuantity.toFixed(3));
        setText('[data-total-amount]', totalAmount.toFixed(2));
    }

    /* -- Product quick-search ---------------------------------------------------------------- */
    function hideSearchResults() {
        if (searchResults) {
            searchResults.hidden = true;
            searchResults.replaceChildren();
        }
    }

    function renderSearchResults(matches) {
        if (!searchResults) {
            return;
        }

        searchResults.replaceChildren();

        if (matches.length === 0) {
            hideSearchResults();
            return;
        }

        matches.forEach(function (product) {
            var item = document.createElement('li');
            var button = document.createElement('button');

            button.type = 'button';
            button.className = 'invoice-search__option';
            button.dataset.productId = product.id;
            button.textContent = product.text;

            item.appendChild(button);
            searchResults.appendChild(item);
        });

        searchResults.hidden = false;
    }

    function findMatches(term) {
        var needle = term.trim().toLowerCase();

        if (needle.length === 0) {
            return [];
        }

        return products
            .filter(function (product) { return product.text.toLowerCase().indexOf(needle) !== -1; })
            .slice(0, MAX_SEARCH_RESULTS);
    }

    function selectProduct(productId) {
        addRow(productId);

        if (searchInput) {
            searchInput.value = '';
        }

        hideSearchResults();
    }

    if (searchInput && searchResults) {
        searchInput.addEventListener('input', function () {
            renderSearchResults(findMatches(searchInput.value));
        });

        searchInput.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                hideSearchResults();
                return;
            }

            if (event.key !== 'Enter') {
                return;
            }

            // Enter inside the search box adds the first match; it must never submit the form.
            event.preventDefault();

            var matches = findMatches(searchInput.value);

            if (matches.length > 0) {
                selectProduct(matches[0].id);
            }
        });

        searchResults.addEventListener('click', function (event) {
            var option = event.target.closest('.invoice-search__option');

            if (option) {
                selectProduct(option.dataset.productId);
            }
        });

        document.addEventListener('click', function (event) {
            if (!event.target.closest('.invoice-search')) {
                hideSearchResults();
            }
        });
    }

    /* -- Wiring ------------------------------------------------------------------------------ */
    addButton.addEventListener('click', function () { addRow(null); });

    body.addEventListener('click', function (event) {
        var removeButton = event.target.closest('.remove-detail-row');

        if (removeButton) {
            removeRow(removeButton);
        }
    });

    body.addEventListener('input', function (event) {
        if (event.target.classList.contains('detail-quantity')
            || event.target.classList.contains('detail-amount')) {
            recalculateTotals();
        }
    });

    body.addEventListener('change', function (event) {
        if (event.target.classList.contains('detail-active')) {
            recalculateTotals();
        }
    });

    form.addEventListener('submit', function (event) {
        var hasActiveRow = Array.prototype.some.call(
            body.querySelectorAll('.detail-active'),
            function (checkbox) { return checkbox.checked; });

        // A courtesy check only; the server refuses the same thing independently.
        if (!hasActiveRow) {
            event.preventDefault();
            window.alert('Please add at least one active detail line before saving.');
        }
    });

    recalculateTotals();
})();
