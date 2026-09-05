/*
 * Master-detail grid behaviour.
 *
 * UI only: adding a line, removing a line, the server-backed product search, Select2 on the partner
 * picker and the running totals. It enforces no business rule. Every rule that matters -- quantity
 * greater than zero, at least one active line, detail ownership, date limits -- is enforced again on
 * the server, the only place a rule can actually be relied upon.
 *
 * No catalogue is embedded in the page. Products and partners are queried from /Lookups as the user
 * types, a bounded page at a time, so page weight does not grow with the size of the catalogue.
 */
(function () {
    'use strict';

    var SEARCH_DEBOUNCE_MS = 250;

    var grid = document.getElementById('detail-grid');
    var template = document.getElementById('detail-row-template');
    var form = document.getElementById('transaction-form');

    if (!grid || !template || !form) {
        return;
    }

    var body = grid.querySelector('tbody');
    var emptyRow = document.getElementById('detail-grid-empty');
    var transactionDateInput = document.querySelector('input[name="TransactionDate"]');

    var searchInput = document.getElementById('product-search');
    var searchResults = document.getElementById('product-search-results');
    var searchUrl = searchInput ? searchInput.dataset.productSearchUrl : null;

    /* -- Remote pickers ------------------------------------------------------------------------
       One helper for the partner select and for every row's product select. Both query the same
       lookup endpoint, so neither ever holds more than the option currently chosen plus whatever
       the last search returned. */
    function initRemoteSelect(select) {
        if (!select || !window.jQuery || !window.jQuery.fn || !window.jQuery.fn.select2) {
            return;
        }

        if (select.dataset.select2Ready === 'true') {
            return;
        }

        window.jQuery(select).select2({
            width: '100%',
            placeholder: select.dataset.select2Placeholder || 'Search...',
            allowClear: false,
            minimumInputLength: 0,
            ajax: {
                url: select.dataset.select2Url,
                dataType: 'json',
                delay: SEARCH_DEBOUNCE_MS,
                data: function (params) {
                    return { term: params.term || '' };
                },
                processResults: function (data) {
                    return { results: data.results };
                },
                cache: true
            }
        });

        select.dataset.select2Ready = 'true';
    }

    function initAllRemoteSelects() {
        document.querySelectorAll('select[data-select2-url]').forEach(initRemoteSelect);
    }

    /* -- Rows -------------------------------------------------------------------------------- */
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

    function updateEmptyState() {
        if (emptyRow) {
            emptyRow.hidden = body.querySelectorAll('tr.detail-row').length > 0;
        }
    }

    function addRow(product) {
        var key = newRowKey();
        var host = document.createElement('tbody');

        host.innerHTML = template.innerHTML.split('__KEY__').join(key).trim();

        var row = host.querySelector('tr');
        var dateInput = row.querySelector('input[type="date"]');

        if (dateInput && !dateInput.value) {
            dateInput.value = defaultDetailDate();
        }

        var productSelect = row.querySelector('select[data-select2-url]');

        if (productSelect && product) {
            // The option has to exist before Select2 initialises, or it renders as unselected.
            // textContent, not innerHTML: the name is server data and is never parsed as markup.
            var option = document.createElement('option');
            option.value = product.id;
            option.textContent = product.text;
            option.selected = true;
            productSelect.appendChild(option);
        }

        body.appendChild(row);

        // State first, so a later failure can never leave the grid claiming to be empty.
        updateEmptyState();
        recalculateTotals();

        // Select2 measures zero width unless the row is already in the document.
        initRemoteSelect(productSelect);

        var quantity = row.querySelector('.detail-quantity');

        if (quantity) {
            quantity.focus();
            quantity.select();
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

        updateEmptyState();
        recalculateTotals();
    }

    /* -- Totals ------------------------------------------------------------------------------ */
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

    /* -- Product search ---------------------------------------------------------------------- */
    var searchTimer = null;
    var searchSequence = 0;
    var activeIndex = -1;

    function searchOptions() {
        return Array.prototype.slice.call(
            searchResults.querySelectorAll('.invoice-search__option'));
    }

    /* Moves the highlight to `index`, clamped to the list, and keeps it in view. Passing -1
       clears the highlight. The highlight is what Enter acts on, so it is the single source of
       truth shared by the arrow keys and the mouse. */
    function setActiveOption(index) {
        var options = searchOptions();

        options.forEach(function (option) {
            option.classList.remove('invoice-search__option--active');
            option.removeAttribute('aria-selected');
        });

        if (options.length === 0 || index < 0) {
            activeIndex = -1;
            searchInput.removeAttribute('aria-activedescendant');
            return;
        }

        activeIndex = Math.min(Math.max(index, 0), options.length - 1);

        var active = options[activeIndex];
        active.classList.add('invoice-search__option--active');
        active.setAttribute('aria-selected', 'true');

        if (!active.id) {
            active.id = 'product-search-option-' + activeIndex;
        }

        searchInput.setAttribute('aria-activedescendant', active.id);
        active.scrollIntoView({ block: 'nearest' });
    }

    /* Wraps at both ends: from the last item Down returns to the first, and from the first Up
       jumps to the last, which is what people expect of a short suggestion list. */
    function moveActiveOption(delta) {
        var options = searchOptions();

        if (options.length === 0) {
            return;
        }

        var next = activeIndex < 0
            ? (delta > 0 ? 0 : options.length - 1)
            : (activeIndex + delta + options.length) % options.length;

        setActiveOption(next);
    }

    function hideSearchResults() {
        if (searchResults) {
            searchResults.hidden = true;
            searchResults.replaceChildren();
        }

        setActiveOptionCleared();
    }

    function setActiveOptionCleared() {
        activeIndex = -1;

        if (searchInput) {
            searchInput.removeAttribute('aria-activedescendant');
            searchInput.setAttribute('aria-expanded', 'false');
        }
    }

    function renderMessage(text) {
        searchResults.replaceChildren();

        var item = document.createElement('li');
        item.className = 'invoice-search__message';
        item.textContent = text;

        searchResults.appendChild(item);
        searchResults.hidden = false;

        setActiveOptionCleared();
        searchInput.setAttribute('aria-expanded', 'true');
    }

    function renderResults(results, heading) {
        searchResults.replaceChildren();

        if (results.length === 0) {
            renderMessage('No matching product.');
            return;
        }

        if (heading) {
            var caption = document.createElement('li');
            caption.className = 'invoice-search__heading';
            caption.textContent = heading;
            searchResults.appendChild(caption);
        }

        results.forEach(function (product, index) {
            var item = document.createElement('li');
            var button = document.createElement('button');

            button.type = 'button';
            button.className = 'invoice-search__option';
            button.id = 'product-search-option-' + index;
            button.setAttribute('role', 'option');
            button.dataset.productId = product.id;
            button.textContent = product.text;

            item.appendChild(button);
            searchResults.appendChild(item);
        });

        searchResults.hidden = false;
        searchInput.setAttribute('aria-expanded', 'true');

        // Pre-highlight the first suggestion so Enter is useful without pressing Down first.
        setActiveOption(0);
    }

    function runSearch(term) {
        if (!searchUrl) {
            return;
        }

        // Responses can arrive out of order; only the newest request is allowed to render.
        var sequence = ++searchSequence;

        fetch(searchUrl + '?term=' + encodeURIComponent(term), {
            headers: { 'Accept': 'application/json' }
        })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('Lookup failed with status ' + response.status);
                }

                return response.json();
            })
            .then(function (data) {
                if (sequence !== searchSequence) {
                    return;
                }

                renderResults(data.results || [], term.length === 0 ? 'Suggested products' : null);
            })
            .catch(function () {
                if (sequence === searchSequence) {
                    renderMessage('Product lookup is unavailable right now.');
                }
            });
    }

    function scheduleSearch(term) {
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(function () { runSearch(term); }, SEARCH_DEBOUNCE_MS);
    }

    function selectProduct(button) {
        addRow({ id: button.dataset.productId, text: button.textContent });

        searchInput.value = '';
        hideSearchResults();
    }

    if (searchInput && searchResults && searchUrl) {
        // Focusing the empty box offers a starting point rather than an empty dropdown.
        searchInput.addEventListener('focus', function () {
            if (searchInput.value.trim().length === 0) {
                runSearch('');
            }
        });

        searchInput.addEventListener('input', function () {
            scheduleSearch(searchInput.value.trim());
        });

        searchInput.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                hideSearchResults();
                return;
            }

            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                // Stop the caret jumping to the start or end of the input.
                event.preventDefault();

                if (searchResults.hidden) {
                    runSearch(searchInput.value.trim());
                    return;
                }

                moveActiveOption(event.key === 'ArrowDown' ? 1 : -1);
                return;
            }

            if (event.key === 'Tab') {
                hideSearchResults();
                return;
            }

            if (event.key !== 'Enter') {
                return;
            }

            // Enter picks the highlighted result; it must never submit the form.
            event.preventDefault();

            var options = searchOptions();

            if (activeIndex >= 0 && options[activeIndex]) {
                selectProduct(options[activeIndex]);
            }
        });

        searchResults.addEventListener('click', function (event) {
            var option = event.target.closest('.invoice-search__option');

            if (option) {
                selectProduct(option);
            }
        });

        // Hovering adopts the highlight, so the mouse and the arrow keys never disagree about
        // which suggestion Enter would take.
        searchResults.addEventListener('mousemove', function (event) {
            var option = event.target.closest('.invoice-search__option');

            if (option) {
                setActiveOption(searchOptions().indexOf(option));
            }
        });

        document.addEventListener('click', function (event) {
            if (!event.target.closest('.invoice-search')) {
                hideSearchResults();
            }
        });
    }

    /* -- Wiring ------------------------------------------------------------------------------ */
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

    initAllRemoteSelects();
    updateEmptyState();
    recalculateTotals();
})();
