// ========================================================================
// When: 2026-08-26
// What: Lightweight request-scoped diagnostic tracker for the CreateTicket
//       submission pipeline. Records BEGIN/END stage events to identify
//       exactly where a submission request spends time or fails.
// Why:  Production CreateTicket submissions show an indefinite "Creating..."
//       state with no ticket created, no error, and no redirect. We need
//       BEGIN events emitted BEFORE potentially blocking operations so
//       that if the operation hangs, the last diagnostic event identifies
//       the in-progress operation.
// ========================================================================

using System;
using System.Diagnostics;
using System.Text;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Request-scoped diagnostic tracker for the CreateTicket submission
    /// pipeline. Emits explicit BEGIN/END stage events for server-side logging.
    ///
    /// Key property: if a request hangs or is interrupted, the last BEGIN
    /// event without a matching END identifies the operation that blocked.
    ///
    /// Thread safety: Not thread-safe. Intended for single-request use only.
    /// </summary>
    public class CreateTicketDiagnostics
    {
        private readonly Stopwatch _totalStopwatch = new Stopwatch();
        private readonly StringBuilder _timeline = new StringBuilder();
        private readonly string _correlationId;
        private string _currentStage;
        private Stopwatch _stageStopwatch;
        private Exception _exception;
        private bool _completed;

        public CreateTicketDiagnostics()
        {
            _correlationId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            _totalStopwatch.Start();
        }

        /// <summary>
        /// Correlation ID for this request (e.g. "CT-7F31A2AB").
        /// Include in user-facing error messages for support correlation.
        /// </summary>
        public string CorrelationId
        {
            get { return "CT-" + _correlationId; }
        }

        /// <summary>
        /// The name of the stage currently in progress, or null if no stage
        /// is active. If the request hangs, this identifies the blocking
        /// operation.
        /// </summary>
        public string CurrentStage
        {
            get { return _currentStage; }
        }

        /// <summary>
        /// Elapsed milliseconds since the current stage was started.
        /// Returns 0 if no stage is active.
        /// </summary>
        public long CurrentStageElapsedMs
        {
            get
            {
                return _stageStopwatch != null && _stageStopwatch.IsRunning
                    ? _stageStopwatch.ElapsedMilliseconds
                    : 0;
            }
        }

        /// <summary>
        /// Total elapsed milliseconds since the diagnostics object was created.
        /// </summary>
        public long TotalElapsedMs
        {
            get { return _totalStopwatch.ElapsedMilliseconds; }
        }

        /// <summary>
        /// Emit a BEGIN event for a named stage. If a previous stage is
        /// still in progress, it is implicitly ended first.
        /// </summary>
        public void BeginStage(string stageName)
        {
            // End previous stage if still running (implicit end).
            if (_stageStopwatch != null && _stageStopwatch.IsRunning)
            {
                EndStageInternal();
            }

            _currentStage = stageName;
            _stageStopwatch = new Stopwatch();
            _stageStopwatch.Start();
            _timeline.AppendLine("[CT] BEGIN " + stageName);
        }

        /// <summary>
        /// Emit an END event for the current stage, recording it as
        /// successfully completed with elapsed time.
        /// </summary>
        public void EndStage()
        {
            if (_stageStopwatch != null && _stageStopwatch.IsRunning)
            {
                EndStageInternal();
            }
        }

        /// <summary>
        /// Emit a FAIL event for the current stage, recording the exception.
        /// </summary>
        public void FailStage(Exception ex)
        {
            _exception = ex;

            if (_stageStopwatch != null && _stageStopwatch.IsRunning)
            {
                _stageStopwatch.Stop();
                string classification = ClassifyDuration(_stageStopwatch.ElapsedMilliseconds);
                _timeline.AppendLine("[CT] FAIL " + _currentStage + "  "
                    + _stageStopwatch.ElapsedMilliseconds + " ms"
                    + classification
                    + "  " + ex.GetType().Name);
                _stageStopwatch = null;
            }
            else
            {
                _timeline.AppendLine("[CT] FAIL " + (_currentStage ?? "unknown")
                    + "  " + ex.GetType().Name);
            }
        }

        /// <summary>
        /// Mark diagnostics as completed. Call after the ticket is created
        /// but BEFORE Response.Redirect.
        /// </summary>
        public void Complete()
        {
            // End any remaining stage.
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
            return ClassifyDuration(_totalStopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Build a complete diagnostic summary for server-side logging.
        /// Contains NO sensitive user data, SQL, or credentials.
        ///
        /// If the request hangs, the last BEGIN without a matching END
        /// identifies the blocking operation.
        /// </summary>
        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[CT] Correlation: " + CorrelationId);
            sb.Append(_timeline);

            if (_exception != null)
            {
                sb.AppendLine("[CT] Exception: " + _exception.GetType().Name
                    + ": " + _exception.Message);
                if (_exception.InnerException != null)
                {
                    sb.AppendLine("[CT] Inner: " + _exception.InnerException.GetType().Name
                        + ": " + _exception.InnerException.Message);
                }
            }
            else if (_completed)
            {
                sb.AppendLine("[CT] Status: COMPLETED  Total: "
                    + _totalStopwatch.ElapsedMilliseconds + " ms ("
                    + GetDurationClassification() + ")");
            }
            else
            {
                // Request was interrupted/hung. Identify the last in-progress stage.
                sb.AppendLine("[CT] Status: INCOMPLETE (request interrupted)");
                if (_currentStage != null)
                {
                    sb.AppendLine("[CT] Last stage: " + _currentStage
                        + "  elapsed: " + CurrentStageElapsedMs + " ms");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get a safe, user-facing error message with correlation ID.
        /// Contains no exception details.
        /// </summary>
        public string GetUserFacingMessage()
        {
            return "An unexpected error occurred while creating the ticket. "
                + "Please try again. Reference: " + CorrelationId;
        }

        private void EndStageInternal()
        {
            _stageStopwatch.Stop();
            string classification = ClassifyDuration(_stageStopwatch.ElapsedMilliseconds);
            _timeline.AppendLine("[CT] END   " + _currentStage + "  "
                + _stageStopwatch.ElapsedMilliseconds + " ms"
                + classification);
            _stageStopwatch = null;
        }

        private static string ClassifyDuration(long ms)
        {
            if (ms < 1000) return "";
            if (ms < 3000) return "  [slow]";
            if (ms < 10000) return "  [VERY SLOW]";
            return "  [CRITICAL]";
        }
    }
}
