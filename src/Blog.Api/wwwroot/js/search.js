(function () {
    'use strict';

    var input = document.getElementById('site-search');
    var list = document.getElementById('search-suggestions');
    var clearBtn = document.querySelector('.search-clear');
    var toggleBtn = document.querySelector('.search-toggle');

    if (!input || !list) return;

    var debounceTimer = null;
    var currentFetch = null;

    // -----------------------------------------------------------------------
    // Small-screen expand / collapse
    // -----------------------------------------------------------------------
    function expandSearch() {
        if (window.innerWidth >= 992) return;
        document.querySelector('header').classList.add('search-expanded');
        if (toggleBtn) toggleBtn.setAttribute('aria-expanded', 'true');
    }

    function collapseSearch() {
        if (window.innerWidth >= 992) return;
        document.querySelector('header').classList.remove('search-expanded');
        if (toggleBtn) toggleBtn.setAttribute('aria-expanded', 'false');
        closeSuggestions();
    }

    if (toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            var expanded = toggleBtn.getAttribute('aria-expanded') === 'true';
            if (expanded) {
                collapseSearch();
            } else {
                expandSearch();
                input.focus();
            }
        });
    }

    // -----------------------------------------------------------------------
    // / keyboard shortcut
    // -----------------------------------------------------------------------
    document.addEventListener('keydown', function (e) {
        if (e.key !== '/') return;
        var active = document.activeElement;
        var isTextField = active && (
            active.tagName === 'INPUT' ||
            active.tagName === 'TEXTAREA' ||
            active.isContentEditable
        );
        if (isTextField) return;
        e.preventDefault();
        expandSearch();
        input.focus();
        input.select();
    });

    // -----------------------------------------------------------------------
    // Clear button
    // -----------------------------------------------------------------------
    function updateClearButton() {
        if (clearBtn) clearBtn.hidden = input.value.length === 0;
    }

    if (clearBtn) {
        clearBtn.addEventListener('click', function () {
            input.value = '';
            input.focus();
            closeSuggestions();
            updateClearButton();
        });
    }

    // -----------------------------------------------------------------------
    // Autocomplete suggestions
    // -----------------------------------------------------------------------
    input.addEventListener('input', function () {
        updateClearButton();
        closeSuggestions();
        clearTimeout(debounceTimer);
        if (input.value.trim().length < 2) return;
        debounceTimer = setTimeout(fetchSuggestions, 200);
    });

    function fetchSuggestions() {
        if (currentFetch) currentFetch.abort();
        var controller = new AbortController();
        currentFetch = controller;
        var q = encodeURIComponent(input.value.trim());
        fetch('/api/public/articles/suggestions?q=' + q, { signal: controller.signal })
            .then(function (res) {
                if (!res.ok) return;
                return res.json();
            })
            .then(function (items) {
                if (items) renderSuggestions(items);
            })
            .catch(function (err) {
                if (err.name !== 'AbortError') console.warn('Suggestions fetch failed', err);
            });
    }

    function renderSuggestions(items) {
        list.innerHTML = '';
        if (!items.length) { closeSuggestions(); return; }
        items.forEach(function (item, i) {
            var li = document.createElement('li');
            li.setAttribute('role', 'option');
            li.id = 'suggestion-' + i;
            li.innerHTML = item.titleHighlighted; // safe: server HTML-encodes + <mark> only
            li.addEventListener('click', function () { navigate(item.slug); });
            li.addEventListener('keydown', function (e) { handleOptionKey(e, i, items); });
            li.tabIndex = -1;
            list.appendChild(li);
        });
        list.hidden = false;
        input.setAttribute('aria-expanded', 'true');
    }

    function closeSuggestions() {
        list.hidden = true;
        list.innerHTML = '';
        input.setAttribute('aria-expanded', 'false');
        input.setAttribute('aria-activedescendant', '');
    }

    function setActivedescendant(id) {
        input.setAttribute('aria-activedescendant', id);
    }

    // -----------------------------------------------------------------------
    // Keyboard navigation in the suggestions listbox
    // -----------------------------------------------------------------------
    input.addEventListener('keydown', function (e) {
        if (e.key === 'ArrowDown' && !list.hidden) {
            e.preventDefault();
            var first = list.querySelector('[role=option]');
            if (first) { first.focus(); setActivedescendant(first.id); }
        }
        if (e.key === 'Escape') { collapseSearch(); input.blur(); }
    });

    function handleOptionKey(e, i, items) {
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            var next = list.children[i + 1];
            if (next) { next.focus(); setActivedescendant(next.id); }
        }
        if (e.key === 'ArrowUp') {
            e.preventDefault();
            if (i === 0) { input.focus(); setActivedescendant(''); }
            else { var prev = list.children[i - 1]; prev.focus(); setActivedescendant(prev.id); }
        }
        if (e.key === 'Enter') { e.preventDefault(); navigate(items[i].slug); }
        if (e.key === 'Escape') { closeSuggestions(); input.focus(); }
        if (e.key === 'Tab') { closeSuggestions(); }
    }

    function navigate(slug) {
        window.location.href = '/articles/' + slug;
    }

    // -----------------------------------------------------------------------
    // Dismiss on outside click
    // -----------------------------------------------------------------------
    document.addEventListener('click', function (e) {
        if (!e.target.closest('.search-wrapper')) closeSuggestions();
    });
}());
