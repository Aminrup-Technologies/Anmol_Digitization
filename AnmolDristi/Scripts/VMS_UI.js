/* Shared VMS UI utilities. Pages supply DOM values; this file formats notices and statuses. */
(function (global) {
    "use strict";

    var DAY_NAMES = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
    var MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    function parseTimeSpan(value) {
        if (!value) return null;
        var parts = value.split(":");
        if (parts.length < 2) return null;
        var h = parseInt(parts[0], 10);
        var m = parseInt(parts[1], 10);
        if (isNaN(h) || isNaN(m)) return null;
        return (h * 60) + m;
    }

    function weekdayName(dateValue) {
        var d = new Date(dateValue + "T00:00:00");
        return DAY_NAMES[d.getDay()];
    }

    function formatClock(minutes) {
        if (minutes === null || minutes === undefined) return "";
        var h = Math.floor(minutes / 60);
        var m = minutes % 60;
        var suffix = h >= 12 ? "PM" : "AM";
        var hour12 = h % 12;
        if (hour12 === 0) hour12 = 12;
        return (hour12 < 10 ? "0" : "") + hour12 + ":" + (m < 10 ? "0" : "") + m + " " + suffix;
    }

    function formatNoticeDate(dateValue) {
        var d = new Date(dateValue + "T00:00:00");
        return weekdayName(dateValue) + " " + ("0" + d.getDate()).slice(-2) + " " + MONTHS[d.getMonth()];
    }

    function ymd(d) {
        return d.getFullYear() + "-" + ("0" + (d.getMonth() + 1)).slice(-2) + "-" + ("0" + d.getDate()).slice(-2);
    }

    function enumerateInclusiveDates(fromIso, toIso) {
        var dates = [];
        if (!fromIso) return dates;
        var cursor = new Date(fromIso + "T00:00:00");
        var end = new Date((toIso || fromIso) + "T00:00:00");
        if (end < cursor) end = cursor;
        while (cursor <= end) {
            dates.push(ymd(cursor));
            cursor.setDate(cursor.getDate() + 1);
        }
        return dates;
    }

    function parseTimingsJson(raw) {
        if (!raw) return {};
        try {
            return JSON.parse(raw) || {};
        } catch (e) {
            return {};
        }
    }

    function classifyPlantHourWindow(startMin, endMin, timing) {
        var result = {
            plantClosed: false,
            arrivalBeforeOpen: false,
            arrivalAfterClose: false,
            departureAfterClose: false,
            openMin: null,
            closeMin: null,
            hasViolation: false
        };
        if (!timing) return result;
        result.openMin = parseTimeSpan(timing.open);
        result.closeMin = parseTimeSpan(timing.close);
        if (!timing.open && !timing.close) {
            result.plantClosed = true;
            result.hasViolation = true;
            return result;
        }
        if (result.openMin !== null && startMin < result.openMin) result.arrivalBeforeOpen = true;
        if (result.closeMin !== null) {
            if (startMin >= result.closeMin) result.arrivalAfterClose = true;
            if (endMin > result.closeMin) result.departureAfterClose = true;
        }
        result.hasViolation = result.plantClosed || result.arrivalBeforeOpen || result.arrivalAfterClose || result.departureAfterClose;
        return result;
    }

    function describeCurrentTimeViolation(nowMin, timing, context) {
        if (!timing) return "";
        var leadIn = context === "approval"
            ? "Current approval is outside today's working hours."
            : "Current registration is outside today's working hours.";
        if (!timing.open && !timing.close)
            return leadIn + " The plant is closed today.";
        var openMin = parseTimeSpan(timing.open);
        var closeMin = parseTimeSpan(timing.close);
        if (openMin !== null && nowMin < openMin)
            return leadIn + " The plant opens at " + formatClock(openMin) + ".";
        if (closeMin !== null && nowMin >= closeMin)
            return leadIn + " The plant closed at " + formatClock(closeMin) + ".";
        return "";
    }

    function formatDayViolationBlock(dateValue, startMin, endMin, classification) {
        var block = formatNoticeDate(dateValue);
        if (classification.plantClosed) {
            block += "\n• The plant is closed this day.";
            return block;
        }
        if (classification.arrivalBeforeOpen)
            block += "\n• Arrival time (" + formatClock(startMin) + ") is before plant opening (" + formatClock(classification.openMin) + ").";
        if (classification.arrivalAfterClose)
            block += "\n• Arrival time (" + formatClock(startMin) + ") is after plant closing (" + formatClock(classification.closeMin) + ").";
        if (classification.departureAfterClose)
            block += "\n• Departure time (" + formatClock(endMin) + ") is after plant closing (" + formatClock(classification.closeMin) + ").";
        return block;
    }

    function collectDayBlocks(timings, timeFrom, timeTo, dateIsos) {
        var dayBlocks = [];
        if (timeFrom === null || timeTo === null) return dayBlocks;
        for (var i = 0; i < dateIsos.length; i++) {
            var classification = classifyPlantHourWindow(timeFrom, timeTo, timings[weekdayName(dateIsos[i])]);
            if (classification.hasViolation)
                dayBlocks.push(formatDayViolationBlock(dateIsos[i], timeFrom, timeTo, classification));
        }
        return dayBlocks;
    }

    function currentNow() {
        var now = new Date();
        return {
            dayName: DAY_NAMES[now.getDay()],
            minutes: now.getHours() * 60 + now.getMinutes()
        };
    }

    function buildHostHoursWarning(timings, timeFrom, timeTo, dateIsos) {
        var now = currentNow();
        var currentViolation = describeCurrentTimeViolation(now.minutes, timings[now.dayName], "approval");
        var dayBlocks = collectDayBlocks(timings, timeFrom, timeTo, dateIsos || []);
        if (!currentViolation && dayBlocks.length === 0)
            return "";
        var msg = "Warning:";
        if (currentViolation)
            msg += "\n" + currentViolation;
        if (dayBlocks.length)
            msg += (currentViolation ? "\n\n" : "\n") + dayBlocks.join("\n\n");
        msg += "\n\nApproving this visit will be recorded as an after-hours approval.";
        return msg;
    }

    function buildVisitorHoursNotice(options) {
        options = options || {};
        var timings = typeof options.timings === "object" ? options.timings : parseTimingsJson(options.timingsJson);
        var now = currentNow();
        var currentViolation = describeCurrentTimeViolation(now.minutes, timings[now.dayName], "registration");
        var fromDate = options.fromDate;
        var toDate = options.toDate || fromDate;
        var timeFrom = options.timeFromMinutes != null ? options.timeFromMinutes : parseTimeSpan(options.timeFrom);
        var timeTo = options.timeToMinutes != null ? options.timeToMinutes : parseTimeSpan(options.timeTo);
        var dates = enumerateInclusiveDates(fromDate, toDate);
        var dayBlocks = collectDayBlocks(timings, timeFrom, timeTo, dates);
        if (!currentViolation && dayBlocks.length === 0)
            return "";
        var msg = "Notice: Your visit falls outside the organization's normal working hours.\n\nYour host may still approve this request as an exception.";
        if (currentViolation)
            msg += "\n\n" + currentViolation;
        if (dayBlocks.length)
            msg += "\n\n" + dayBlocks.join("\n\n");
        return msg;
    }

    function setHoursWarning(text) {
        var panel = document.querySelector("[data-vms-hours-warning]");
        if (!panel) return;
        var body = panel.querySelector("[data-vms-hours-warning-body]");
        if (text) {
            if (body) body.textContent = text;
            panel.removeAttribute("hidden");
            panel.setAttribute("aria-hidden", "false");
        } else {
            if (body) body.textContent = "";
            panel.setAttribute("hidden", "hidden");
            panel.setAttribute("aria-hidden", "true");
        }
    }

    function statusClass(status) {
        var value = (status || "").toString().trim().toLowerCase();
        if (value === "approved" || value === "valid pass") return "badge-approved";
        if (value === "pending") return "badge-pending";
        if (value === "rejected") return "badge-rejected";
        if (value === "checked-in" || value === "checked in") return "badge-checked-in";
        if (value === "checked-out" || value === "checked out") return "badge-checked-out";
        if (value.indexOf("not approved") >= 0) return "badge-not-approved";
        return "badge-pending";
    }

    var api = {
        parseTimeSpan: parseTimeSpan,
        weekdayName: weekdayName,
        formatClock: formatClock,
        formatNoticeDate: formatNoticeDate,
        ymd: ymd,
        enumerateInclusiveDates: enumerateInclusiveDates,
        parseTimingsJson: parseTimingsJson,
        classifyPlantHourWindow: classifyPlantHourWindow,
        describeCurrentTimeViolation: describeCurrentTimeViolation,
        formatDayViolationBlock: formatDayViolationBlock,
        buildHostHoursWarning: buildHostHoursWarning,
        buildVisitorHoursNotice: buildVisitorHoursNotice,
        setHoursWarning: setHoursWarning,
        statusClass: statusClass
    };

    global.VmsUI = api;
})(window);
