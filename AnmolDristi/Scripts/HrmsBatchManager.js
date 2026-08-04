/**
 * HrmsBatchManager page behaviour + Synchronization Information slide-out panel.
 * Depends on jQuery (provided by Dristi.Master). No other external libraries.
 */
(function (window, document, $) {
    'use strict';

    var ANIM_MS = 250;
    var SELECTORS = {
        backdrop: '#hrmsSyncInfoBackdrop',
        panel: '#hrmsSyncInfoPanel',
        trigger: '[data-hrms-help-trigger]',
        close: '[data-hrms-help-close]',
        body: '.hrms-sync-info-body',
        section: '.hrms-sync-info-section'
    };

    var state = {
        open: false,
        lastFocus: null,
        closeTimer: null
    };

    function cfg() {
        return window.HrmsBatchManagerConfig || {};
    }

    function $panel() {
        return $(SELECTORS.panel);
    }

    function $backdrop() {
        return $(SELECTORS.backdrop);
    }

    function isOpen() {
        return state.open;
    }

    function clearHighlight() {
        $panel().find(SELECTORS.section).removeClass('is-highlighted');
    }

    function scrollToSection(sectionId) {
        if (!sectionId) {
            return;
        }

        var $section = $('#' + sectionId);
        var $body = $panel().find(SELECTORS.body);
        if (!$section.length || !$body.length) {
            return;
        }

        clearHighlight();
        $section.addClass('is-highlighted');

        var top = ($section.offset().top - $body.offset().top) + $body.scrollTop() - 12;
        if (top < 0) {
            top = 0;
        }
        $body.stop(true).animate({ scrollTop: top }, 200);
    }

    function openPanel(sectionId) {
        if (state.closeTimer) {
            window.clearTimeout(state.closeTimer);
            state.closeTimer = null;
        }

        state.lastFocus = document.activeElement;
        state.open = true;

        $backdrop()
            .attr('aria-hidden', 'false')
            .addClass('is-visible');

        $panel()
            .attr('aria-hidden', 'false')
            .addClass('is-open');

        $('body').addClass('hrms-sync-info-open');

        window.setTimeout(function () {
            scrollToSection(sectionId);
            $panel().find(SELECTORS.close).first().focus();
        }, ANIM_MS);
    }

    function closePanel() {
        if (!state.open) {
            return;
        }

        state.open = false;

        $panel().removeClass('is-open');
        $backdrop().removeClass('is-visible');
        $('body').removeClass('hrms-sync-info-open');
        clearHighlight();

        state.closeTimer = window.setTimeout(function () {
            $panel().attr('aria-hidden', 'true');
            $backdrop().attr('aria-hidden', 'true');
            state.closeTimer = null;

            if (state.lastFocus && typeof state.lastFocus.focus === 'function') {
                try {
                    state.lastFocus.focus();
                } catch (e) {
                    /* ignore */
                }
            }
        }, ANIM_MS);
    }

    function bindHelpPanel() {
        $(document)
            .off('click.hrmsHelp', SELECTORS.trigger)
            .on('click.hrmsHelp', SELECTORS.trigger, function (e) {
                e.preventDefault();
                e.stopPropagation();
                openPanel($(this).attr('data-hrms-help-section'));
            });

        $(document)
            .off('click.hrmsHelpClose', SELECTORS.close)
            .on('click.hrmsHelpClose', SELECTORS.close, function (e) {
                e.preventDefault();
                closePanel();
            });

        $(document)
            .off('keydown.hrmsHelp')
            .on('keydown.hrmsHelp', function (e) {
                if (!isOpen()) {
                    return;
                }
                if (e.key === 'Escape' || e.keyCode === 27) {
                    e.preventDefault();
                    closePanel();
                }
            });

        /* Trap basic Tab focus within the open panel */
        $(document)
            .off('keydown.hrmsHelpTrap')
            .on('keydown.hrmsHelpTrap', function (e) {
                if (!isOpen() || (e.key !== 'Tab' && e.keyCode !== 9)) {
                    return;
                }

                var $focusable = $panel()
                    .find('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])')
                    .filter(':visible');
                if (!$focusable.length) {
                    return;
                }

                var first = $focusable[0];
                var last = $focusable[$focusable.length - 1];
                if (e.shiftKey && document.activeElement === first) {
                    e.preventDefault();
                    last.focus();
                } else if (!e.shiftKey && document.activeElement === last) {
                    e.preventDefault();
                    first.focus();
                }
            });
    }

    /* ---- Existing page helpers (ClientIDs from HrmsBatchManagerConfig) ---- */

    window.onRowSelect = function (btnElement, batchId) {
        var c = cfg();
        var hf = document.getElementById(c.hfSelectedBatchId);
        var lbl = document.getElementById(c.lblSelectedBatchId);
        if (hf) {
            hf.value = batchId;
        }
        if (lbl) {
            lbl.innerText = batchId;
        }

        var rows = document.querySelectorAll('#' + c.gvBatchesId + ' tr');
        for (var i = 0; i < rows.length; i++) {
            rows[i].classList.remove('selected-row');
        }

        var clickedRow = btnElement.closest ? btnElement.closest('tr') : null;
        if (!clickedRow && btnElement.parentNode) {
            var el = btnElement;
            while (el && el.tagName !== 'TR') {
                el = el.parentNode;
            }
            clickedRow = el;
        }
        if (clickedRow) {
            clickedRow.classList.add('selected-row');
        }
    };

    window.confirmRun = function () {
        var c = cfg();
        var hf = document.getElementById(c.hfSelectedBatchId);
        var sel = hf ? hf.value : '';
        if (!sel) {
            alert('Please select a batch to run from the grid.');
            return false;
        }
        if (!confirm('Execute processing for batch:\n' + sel + '?')) {
            return false;
        }

        var btn = document.getElementById(c.btnRunId);
        if (btn) {
            btn.value = 'Running...';
            btn.classList.add('disabled');
        }
        return true;
    };

    window.HrmsSyncInfoPanel = {
        open: openPanel,
        close: closePanel,
        isOpen: isOpen
    };

    function init() {
        bindHelpPanel();
    }

    $(init);

    /* Re-bind after ASP.NET UpdatePanel async postbacks */
    if (window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
            bindHelpPanel();
        });
    }
}(window, document, window.jQuery));
