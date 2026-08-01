/**
 * Claims UX stabilization utilities — client-side only.
 * Always defines window.ClaimsUx (no-op fallback if jQuery is unavailable).
 */
(function (window) {
    'use strict';

    function noop() { }

    function get$() {
        return window.jQuery;
    }

    function hasBootstrapModal() {
        var $ = get$();
        return $ && $.fn && typeof $.fn.modal === 'function';
    }

    var stub = {
        dirty: false,
        suppressDirtyCheck: false,
        processingCount: 0,
        _handlersBound: false,
        _pendingProcessingMessage: null,
        options: {},
        init: noop,
        initOnce: noop,
        markDirty: noop,
        clearDirty: noop,
        isDirty: function () { return false; },
        confirmLeave: function (onLeave) { if (typeof onLeave === 'function') onLeave(); },
        showConfirm: function (cfg) {
            if (cfg && typeof cfg.onConfirm === 'function' && window.confirm((cfg.title || 'Confirm') + '\n\n' + (cfg.message || '').replace(/<[^>]+>/g, ''))) {
                cfg.onConfirm();
            }
        },
        disableButton: noop,
        enableButton: noop,
        showProcessing: noop,
        hideProcessing: noop,
        armProcessingForPostback: noop,
        statusBadgeClass: function () { return 'claims-badge claims-badge-default'; },
        parseExpenseCounts: function () { return { total: 0, approved: 0, pending: 0 }; }
    };

    window.ClaimsUx = stub;

    if (!get$()) {
        return;
    }

    var ClaimsUx = {
        dirty: false,
        suppressDirtyCheck: false,
        processingCount: 0,
        _handlersBound: false,
        _pendingProcessingMessage: null,
        options: {},

        initOnce: function (options) {
            var self = this;

            function runInit() {
                var $ = get$();
                if (!$) return;

                options = options || {};
                self.options = $.extend({
                    dirtySelector: '.claims-ux-track-dirty',
                    postbackButtonSelector: '.claims-ux-postback-btn',
                    modalSelector: '.modal'
                }, options);

                if (!self._handlersBound) {
                    self._bindDirtyTracking();
                    self._bindBeforeUnload();
                    self._bindModalGuards();
                    self._bindPostbackButtons();
                    self._bindProcessingPanel();
                    self._handlersBound = true;
                }
            }

            if (hasBootstrapModal()) {
                runInit();
            } else {
                window.addEventListener('load', runInit, { once: true });
            }
        },

        init: function (options) {
            this.initOnce(options);
        },

        markDirty: function () {
            if (!this.suppressDirtyCheck) this.dirty = true;
        },

        clearDirty: function () {
            this.dirty = false;
        },

        isDirty: function () {
            return !!this.dirty;
        },

        confirmLeave: function (onLeave) {
            var self = this;
            if (!this.dirty) {
                if (typeof onLeave === 'function') onLeave();
                return;
            }
            this.showConfirm({
                title: 'Unsaved Changes',
                message: 'You have unsaved review changes.<br><br>Leaving now will discard them.',
                confirmText: 'Leave',
                confirmClass: 'btn-danger',
                cancelText: 'Stay',
                onConfirm: function () {
                    self.clearDirty();
                    if (typeof onLeave === 'function') onLeave();
                }
            });
        },

        showConfirm: function (cfg) {
            cfg = cfg || {};

            if (!hasBootstrapModal()) {
                stub.showConfirm(cfg);
                return;
            }

            var $ = get$();
            var $modal = $('#claimsUxConfirmModal');
            if (!$modal.length) {
                $('body').append(
                    '<div class="modal fade" id="claimsUxConfirmModal" tabindex="-1" role="dialog" data-backdrop="static">' +
                    '<div class="modal-dialog modal-dialog-centered"><div class="modal-content">' +
                    '<div class="modal-header"><h5 class="modal-title" id="claimsUxConfirmTitle"></h5>' +
                    '<button type="button" class="close" data-dismiss="modal"><span>&times;</span></button></div>' +
                    '<div class="modal-body" id="claimsUxConfirmBody"></div>' +
                    '<div class="modal-footer">' +
                    '<button type="button" class="btn btn-secondary" id="claimsUxConfirmCancel" data-dismiss="modal">Cancel</button>' +
                    '<button type="button" class="btn btn-primary" id="claimsUxConfirmOk">Confirm</button>' +
                    '</div></div></div></div>'
                );
                $modal = $('#claimsUxConfirmModal');
            }

            $('#claimsUxConfirmTitle').text(cfg.title || 'Confirm');
            $('#claimsUxConfirmBody').html(cfg.message || 'Are you sure?');
            $('#claimsUxConfirmCancel').text(cfg.cancelText || 'Cancel');
            $('#claimsUxConfirmOk')
                .text(cfg.confirmText || 'Confirm')
                .removeClass('btn-primary btn-danger btn-warning btn-success')
                .addClass(cfg.confirmClass || 'btn-primary');

            $modal.off('click.claimsUxConfirm', '#claimsUxConfirmOk').on('click.claimsUxConfirm', '#claimsUxConfirmOk', function () {
                $modal.modal('hide');
                if (typeof cfg.onConfirm === 'function') cfg.onConfirm();
            });

            $modal.modal('show');
        },

        disableButton: function (btn, loadingText) {
            var $ = get$();
            var $btn = $(btn);
            if (!$btn.length) return;
            if (!$btn.data('ux-original-html')) {
                $btn.data('ux-original-html', $btn.is('input') ? $btn.val() : $btn.html());
            }
            var spinner = '<i class="fa fa-spinner fa-spin mr-1"></i>';
            if ($btn.is('input')) {
                $btn.val(loadingText || 'Processing...');
            } else {
                $btn.html(spinner + (loadingText || 'Processing...'));
            }
            $btn.prop('disabled', true).addClass('disabled');
        },

        enableButton: function (btn) {
            var $ = get$();
            var $btn = $(btn);
            if (!$btn.length) return;
            var original = $btn.data('ux-original-html');
            if (original !== undefined) {
                if ($btn.is('input')) $btn.val(original);
                else $btn.html(original);
            }
            $btn.prop('disabled', false).removeClass('disabled').removeData('ux-locked').removeData('ux-original-html');
        },

        showProcessing: function (message) {
            var $ = get$();
            this.processingCount++;
            var $overlay = $('#claimsUxProcessingOverlay');
            if (!$overlay.length) {
                $('body').append(
                    '<div id="claimsUxProcessingOverlay" class="claims-ux-processing-overlay">' +
                    '<div class="claims-ux-processing-card">' +
                    '<i class="fa fa-spinner fa-spin fa-2x mb-3 text-primary"></i>' +
                    '<h5 class="mb-2">Processing...</h5>' +
                    '<p class="text-muted mb-0 claims-ux-processing-msg">Please wait...</p>' +
                    '</div></div>'
                );
                $overlay = $('#claimsUxProcessingOverlay');
            }
            $overlay.find('.claims-ux-processing-msg').text(message || 'Please wait...');
            $overlay.show();
        },

        hideProcessing: function () {
            var $ = get$();
            this.processingCount = 0;
            $('#claimsUxProcessingOverlay').hide();
        },

        armProcessingForPostback: function (message) {
            this._pendingProcessingMessage = message || 'Please wait...';
        },

        statusBadgeClass: function (status) {
            var s = (status || '').toString().trim().toLowerCase();
            if (s === 'pending' || s === 'submitted' || s === 'draft') return 'claims-badge claims-badge-pending';
            if (s === 'returned') return 'claims-badge claims-badge-returned';
            if (s === 'approved') return 'claims-badge claims-badge-approved';
            if (s === 'completed') return 'claims-badge claims-badge-completed';
            if (s === 'readyforpayment' || s === 'ready for payment') return 'claims-badge claims-badge-ready';
            if (s === 'rejected') return 'claims-badge claims-badge-rejected';
            return 'claims-badge claims-badge-default';
        },

        parseExpenseCounts: function (json) {
            var total = 0, approved = 0, pending = 0;
            try {
                var list = typeof json === 'string' ? JSON.parse(json) : json;
                if (!Array.isArray(list)) return { total: 0, approved: 0, pending: 0 };
                list.forEach(function (e) {
                    total++;
                    var st = ((e.ClaimStatus || e.claimStatus || '') + '').trim().toLowerCase();
                    if (st === 'approved') approved++;
                    else pending++;
                });
            } catch (ex) { /* ignore */ }
            return { total: total, approved: approved, pending: pending };
        },

        _resetPostbackButtons: function () {
            var self = this;
            var $ = get$();
            $(this.options.postbackButtonSelector).each(function () {
                self.enableButton(this);
            });
        },

        _bindDirtyTracking: function () {
            var self = this;
            var $ = get$();
            $(document).off('input.claimsUxDirty change.claimsUxDirty', this.options.dirtySelector);
            $(document).on('input.claimsUxDirty change.claimsUxDirty', this.options.dirtySelector, function () {
                self.markDirty();
            });
        },

        _bindBeforeUnload: function () {
            var self = this;
            var $ = get$();
            $(window).off('beforeunload.claimsUx').on('beforeunload.claimsUx', function (e) {
                if (!self.dirty) return undefined;
                var msg = 'You have unsaved review changes.';
                e.returnValue = msg;
                return msg;
            });
        },

        _bindModalGuards: function () {
            if (!hasBootstrapModal()) return;

            var self = this;
            var $ = get$();
            $(this.options.modalSelector).off('hide.bs.modal.claimsUx').on('hide.bs.modal.claimsUx', function (e) {
                if (self.suppressDirtyCheck || !self.dirty) return;
                e.preventDefault();
                e.stopImmediatePropagation();
                self.confirmLeave(function () {
                    self.suppressDirtyCheck = true;
                    $(e.target).modal('hide');
                    setTimeout(function () { self.suppressDirtyCheck = false; }, 300);
                });
            });
        },

        _bindPostbackButtons: function () {
            var self = this;
            var $ = get$();
            $(document).off('click.claimsUxPostback', this.options.postbackButtonSelector);
            $(document).on('click.claimsUxPostback', this.options.postbackButtonSelector, function (e) {
                var $btn = $(this);

                if ($btn.data('ux-skip-confirm')) {
                    $btn.removeData('ux-skip-confirm');
                } else if ($btn.data('ux-confirm')) {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                    var confirmCfg = $btn.data('ux-confirm');
                    if (typeof confirmCfg === 'string') {
                        try { confirmCfg = JSON.parse(confirmCfg); } catch (ex) { confirmCfg = { message: confirmCfg }; }
                    }
                    self.showConfirm(get$().extend({
                        onConfirm: function () {
                            $btn.data('ux-skip-confirm', true);
                            $btn[0].click();
                        }
                    }, confirmCfg));
                    return false;
                }

                if ($btn.data('ux-locked')) {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                    return false;
                }

                $btn.data('ux-locked', true);
                self.clearDirty();
                self.armProcessingForPostback($btn.data('ux-processing') || 'Please wait...');
                // Do NOT disable the button here — disabling cancels ASP.NET postback.
            });
        },

        _bindProcessingPanel: function () {
            var self = this;
            if (typeof Sys === 'undefined' || !Sys.WebForms || !Sys.WebForms.PageRequestManager) {
                return;
            }

            var prm = Sys.WebForms.PageRequestManager.getInstance();
            if (prm._claimsUxHooksBound) {
                return;
            }
            prm._claimsUxHooksBound = true;

            prm.add_beginRequest(function () {
                if (self._pendingProcessingMessage) {
                    self.showProcessing(self._pendingProcessingMessage);
                }
                var $ = get$();
                $(self.options.postbackButtonSelector).each(function () {
                    if ($(this).data('ux-locked')) {
                        self.disableButton(this, $(this).data('ux-loading'));
                    }
                });
            });

            prm.add_endRequest(function () {
                self.hideProcessing();
                self._pendingProcessingMessage = null;
                self._resetPostbackButtons();
            });
        }
    };

    window.ClaimsUx = ClaimsUx;
})(window);
