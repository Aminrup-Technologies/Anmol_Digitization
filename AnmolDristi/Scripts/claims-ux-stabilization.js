/**
 * Claims UX stabilization utilities — client-side only.
 * No business logic, routing, or workflow state changes.
 */
(function (window, $) {
    'use strict';

    if (!$) return;

    var ClaimsUx = {
        dirty: false,
        suppressDirtyCheck: false,
        processingCount: 0,

        init: function (options) {
            options = options || {};
            this.options = $.extend({
                dirtySelector: '.claims-ux-track-dirty',
                postbackButtonSelector: '.claims-ux-postback-btn',
                modalSelector: '.modal',
                detailsModalSelector: '#detailsModal',
                unifiedModalSelector: '#unifiedDayModal'
            }, options);

            this._bindDirtyTracking();
            this._bindBeforeUnload();
            this._bindModalGuards();
            this._bindPostbackButtons();
            this._bindProcessingPanel();
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
            var $modal = $('#claimsUxConfirmModal');
            if (!$modal.length) {
                $('body').append(
                    '<div class="modal fade" id="claimsUxConfirmModal" tabindex="-1" role="dialog" data-backdrop="static">' +
                    '<div class="modal-dialog modal-dialog-centered"><div class="modal-content">' +
                    '<div class="modal-header"><h5 class="modal-title" id="claimsUxConfirmTitle"></h5>' +
                    '<button type="button" class="close" data-dismiss="modal"><span>&times;</span></button></div>' +
                    '<div class="modal-body" id="claimsUxConfirmBody"></div>' +
                    '<div class="modal-footer">' +
                    '<button type="button" class="btn btn-secondary" id="claimsUxConfirmCancel">Cancel</button>' +
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

            $modal.off('click', '#claimsUxConfirmOk').on('click', '#claimsUxConfirmOk', function () {
                $modal.modal('hide');
                if (typeof cfg.onConfirm === 'function') cfg.onConfirm();
            });

            $modal.modal('show');
        },

        disableButton: function (btn, loadingText) {
            var $btn = $(btn);
            if (!$btn.length || $btn.data('ux-locked')) return;
            $btn.data('ux-locked', true);
            $btn.data('ux-original-html', $btn.is('input') ? $btn.val() : $btn.html());
            var spinner = '<i class="fa fa-spinner fa-spin mr-1"></i>';
            if ($btn.is('input')) {
                $btn.val(loadingText || 'Processing...');
            } else {
                $btn.html(spinner + (loadingText || 'Processing...'));
            }
            $btn.prop('disabled', true).addClass('disabled');
        },

        enableButton: function (btn) {
            var $btn = $(btn);
            if (!$btn.length) return;
            var original = $btn.data('ux-original-html');
            if (original !== undefined) {
                if ($btn.is('input')) $btn.val(original);
                else $btn.html(original);
            }
            $btn.prop('disabled', false).removeClass('disabled').removeData('ux-locked');
        },

        showProcessing: function (message) {
            this.processingCount++;
            var $overlay = $('#claimsUxProcessingOverlay');
            if (!$overlay.length) {
                $('body').append(
                    '<div id="claimsUxProcessingOverlay" class="claims-ux-processing-overlay">' +
                    '<div class="claims-ux-processing-card">' +
                    '<i class="fa fa-spinner fa-spin fa-2x mb-3 text-primary"></i>' +
                    '<h5 class="mb-2">Processing...</h5>' +
                    '<p class="text-muted mb-0 claims-ux-processing-msg">Please wait while the request is being processed.</p>' +
                    '</div></div>'
                );
                $overlay = $('#claimsUxProcessingOverlay');
            }
            $overlay.find('.claims-ux-processing-msg').text(message || 'Please wait while the request is being processed.');
            $overlay.show();
        },

        hideProcessing: function () {
            this.processingCount = Math.max(0, this.processingCount - 1);
            if (this.processingCount === 0) {
                $('#claimsUxProcessingOverlay').hide();
            }
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

        _bindDirtyTracking: function () {
            var self = this;
            $(document).on('input change', this.options.dirtySelector, function () {
                self.markDirty();
            });
        },

        _bindBeforeUnload: function () {
            var self = this;
            $(window).on('beforeunload.claimsUx', function (e) {
                if (!self.dirty) return undefined;
                var msg = 'You have unsaved review changes.';
                e.returnValue = msg;
                return msg;
            });
        },

        _bindModalGuards: function () {
            var self = this;
            $(this.options.modalSelector).on('hide.bs.modal.claimsUx', function (e) {
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
            $(document).on('click', this.options.postbackButtonSelector, function (e) {
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
                    self.showConfirm($.extend({
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

                self.clearDirty();
                if ($btn.data('ux-processing')) {
                    self.armProcessingForPostback($btn.data('ux-processing'));
                }
                self.disableButton($btn[0], $btn.data('ux-loading'));
            });
        },

        _bindProcessingPanel: function () {
            var self = this;
            if (typeof Sys !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
                var prm = Sys.WebForms.PageRequestManager.getInstance();
                prm.add_beginRequest(function () {
                    if (self._pendingProcessingMessage) {
                        self.showProcessing(self._pendingProcessingMessage);
                    }
                });
                prm.add_endRequest(function () {
                    self.hideProcessing();
                    self._pendingProcessingMessage = null;
                    $(self.options.postbackButtonSelector).each(function () {
                        ClaimsUx.enableButton(this);
                    });
                });
            }
        },

        armProcessingForPostback: function (message) {
            this._pendingProcessingMessage = message;
        }
    };

    window.ClaimsUx = ClaimsUx;
})(window, window.jQuery);
