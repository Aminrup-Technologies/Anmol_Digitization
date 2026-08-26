// ========================================================================
// When: 2026-08-26
// What: Lightweight request-scoped diagnostic tracker for the CreateTicket
//       submission pipeline. Records stage-by-stage timing to identify
//       exactly where a submission request spends time or fails.
// Why:  Production CreateTicket submissions show an indefinite "Creating..."
//       state with no ticket created, no error, and no redirect. We need
//       to establish the exact failure/delay point before applying fixes.
// ========================================================================

using System;
using System.Diagnostics;
using System.Text;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Request-scoped diagnostic tracker for the CreateTicket submission
    /// pipeline. Captures stage-by-stage timing, success/failure, and
    /// exception details for server-side logging.
    ///
    /// Usage:
    ///   var diag = new CreateTicketDiagnostics();
    ///   diag.BeginStage("Request received");
    ///   diag.EndStage();
    ///   // ... or on failure:
    ///   diag.FailStage(ex);
    ///   Trace.TraceInformation(diag.GetSummary());
    ///
    /// Thread safety: Not thread-safe. Intended for single-request use only.
    /// </summary>
    public class CreateTicketDiagnostics
    {
        private readonly Stopwatch _totalStopwatch = new Stopwatch();
        private readonly StringBuilder _timeline = new StringBuilder();
        private readonly string _correlationId;
        private Stopwatch _stageStopwatch;
        private string _currentStage;
        private string _failedStage;
        private Exception _exception;
        private bool _completed;

        public CreateTicketDiagnostics()
        {
            _correlationId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            _totalStopwatch.Start();
        }

        /// <summary>
        /// Correlation ID for this request (8-char hex, e.g. "CT-7F31A2").
        /// Include in user-facing error messages for support correlation.
        /// </summary>
        public string CorrelationId
        {
            get { return "CT-" + _correlationId; }
        }

        /// <summary>
        /// Total elapsed milliseconds since the diagnostics object was created.
        /// </summary>
        public long TotalElapsedMs
        {
            get { return _totalStopwatch.ElapsedMilliseconds; }
        }

        /// <summary>
        /// Begin timing a named stage. If a previous stage is still in
        /// progress, it is implicitly ended first.
        /// </summary>
        public void BeginStage(string stageName)
        {
            // End previous stage if still running.
            if (_stageStopwatch != null && _stageStopwatch.IsRunning)
            {
                EndStageInternal();
            }

            _currentStage = stageName;
            _stageStopwatch = new Stopwatch();
            _stageStopwatch.Start();
        }

        /// <summary>
        /// End the current stage and record it as successful.
        /// </summary>
        public void EndStage()
        {
            if (_stageStopwatch != null && _stageStopwatch.IsRunning)
            {
                EndStageInternal();
            }
        }

        /// <summary>
        /// End the current stage and record it as failed with an exception.
        /// </summary>
        public void FailStage(Exception ex)
        {
            _failedStage = _currentStage;
            _exception = ex;

            if (_stageStopwatch != null && _stageStopwatch.IsRunning)
            {
                _stageStopwatch.Stop();
                AppendEntry(_currentStage, _stageStopwatch.ElapsedMilliseconds, false, null);
                _stageStopwatch = null;
            }
        }

        /// <summary>
        /// Stop the total timer and finalize diagnostics.
        /// </summary>
        public void Complete()
        {
            if (_stageStopwatch != null && _stageStopwatch.IsRunning)
            {
                EndStageInternal();
            }

            _completed = true;
            _totalStopwatch.Stop();
        }

        /// <summary>
        /// Get the classification for the total elapsed time.
        /// </summary>
        public string GetDurationClassification()
        {
            long ms = _totalStopwatch.ElapsedMilliseconds;
            if (ms < 1000) return "normal";
            if (ms < 3000) return "slow";
            if (ms < 10000) return "very slow";
            return "critical";
        }

        /// <summary>
        /// Build a complete diagnostic summary for server-side logging.
        /// Contains NO sensitive user data, SQL, or credentials.
        /// </summary>
        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== CreateTicket Diagnostics ===");
            sb.AppendLine("Correlation: " + CorrelationId);
            sb.AppendLine("Total: " + _totalStopwatch.ElapsedMilliseconds + " ms (" + GetDurationClassification() + ")");
            sb.AppendLine("Timeline:");
            sb.Append(_timeline);

            if (_exception != null)
            {
                sb.AppendLine("FAILED at: " + (_failedStage ?? "unknown"));
                sb.AppendLine("Exception: " + _exception.GetType().Name + ": " + _exception.Message);
                if (_exception.InnerException != null)
                {
                    sb.AppendLine("Inner: " + _exception.InnerException.GetType().Name
                        + ": " + _exception.InnerException.Message);
                }
            }
            else if (_completed)
            {
                sb.AppendLine("Status: COMPLETED");
            }
            else
            {
                sb.AppendLine("Status: INCOMPLETE (request may have been interrupted)");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get a safe, user-facing error message that includes the correlation
        /// ID for support purposes. Contains no exception details.
        /// </summary>
        public string GetUserFacingMessage()
        {
            if (_failedStage != null)
            {
                return "We could not create the ticket. Please try again. Reference: " + CorrelationId;
            }

            return "An unexpected error occurred while creating the ticket. Please try again. Reference: " + CorrelationId;
        }

        private void EndStageInternal()
        {
            _stageStopwatch.Stop();
            AppendEntry(_currentStage, _stageStopwatch.ElapsedMilliseconds, true, null);
            _stageStopwatch = null;
        }

        private void AppendEntry(string stage, long elapsedMs, bool success, string detail)
        {
            string classification;
            if (elapsedMs < 1000) classification = "";
            else if (elapsedMs < 3000) classification = " [slow]";
            else if (elapsedMs < 10000) classification = " [VERY SLOW]";
            else classification = " [CRITICAL]";

            string status = success ? "ok" : "FAIL";
            _timeline.AppendLine("  " + stage.PadRight(35) + elapsedMs.ToString().PadLeft(8) + " ms  " + status + classification);
        }
    }
}
