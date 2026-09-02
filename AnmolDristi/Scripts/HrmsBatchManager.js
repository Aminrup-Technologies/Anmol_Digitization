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

        /* Keep chevron / aria in sync; scroll tech block into view when expanded */
        $(document)
            .off('show.bs.collapse.hrmsTech hide.bs.collapse.hrmsTech shown.bs.collapse.hrmsTech')
            .on('show.bs.collapse.hrmsTech', '#hrmsTechDetailsCollapse', function () {
                $('#hrmsTechDetailsToggle').attr('aria-expanded', 'true');
            })
            .on('hide.bs.collapse.hrmsTech', '#hrmsTechDetailsCollapse', function () {
                $('#hrmsTechDetailsToggle').attr('aria-expanded', 'false');
            })
            .on('shown.bs.collapse.hrmsTech', '#hrmsTechDetailsCollapse', function () {
                var $body = $panel().find(SELECTORS.body);
                var $section = $('#hrms-help-section-technical');
                if (!$body.length || !$section.length) {
                    return;
                }
                var top = ($section.offset().top - $body.offset().top) + $body.scrollTop() - 8;
                if (top < 0) {
                    top = 0;
                }
                $body.stop(true).animate({ scrollTop: top }, 200);
            });
    }

    /* ---- Page helpers (ClientIDs from HrmsBatchManagerConfig) ---- */

    function selectedBatchId() {
        var c = cfg();
        var hf = document.getElementById(c.hfSelectedBatchId);
        return hf ? (hf.value || '') : '';
    }

    function disableButton(buttonId) {
        if (!buttonId) {
            return;
        }
        var btn = document.getElementById(buttonId);
        if (btn) {
            btn.value = 'Running...';
            btn.classList.add('disabled');
        }
    }

    window.confirmSyncMasters = function () {
        var sel = selectedBatchId();
        if (!sel) {
            alert('Please select a batch first.');
            return false;
        }
        if (!confirm('Synchronize organization masters for batch ' + sel + '?\nThis does not insert or update employees.')) {
            return false;
        }
        disableButton(cfg().btnSyncMastersId);
        return true;
    };

    window.confirmExecute = function (actionLabel) {
        var sel = selectedBatchId();
        if (!sel) {
            alert('Please select a batch first.');
            return false;
        }
        if (!confirm('Execute "' + actionLabel + '" for batch ' + sel + '?\nOnly this action will run.')) {
            return false;
        }

        var c = cfg();
        disableButton(c.btnExecuteInsertId);
        disableButton(c.btnExecuteUpdateId);
        disableButton(c.btnExecuteHierarchyId);
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
