using System;
using System.Data;
using System.Linq;

namespace AnmolDristi
{
    /// <summary>
    /// Resolves the current (latest) SalesApprovalRequestDetail row per ActivityId when
    /// resubmission inserts new detail versions for returned line items.
    /// </summary>
    public static class SalesApprovalDetailVersionHelper
    {
        /// <summary>
        /// WHERE fragment for alias d — keeps only the latest detail row per activity.
        /// Requires @Id (ApprovalRequestId).
        /// </summary>
        public const string LatestDetailWhereForD = @"
            d.ApprovalRequestDetailId IN (
                SELECT MAX(ApprovalRequestDetailId)
                FROM dbo.SalesApprovalRequestDetail
                WHERE ApprovalRequestId = @Id
                GROUP BY ActivityId
            )";

        /// <summary>
        /// INNER JOIN fragment for alias d onto the latest detail per activity.
        /// Requires @Id (ApprovalRequestId).
        /// </summary>
        public const string LatestDetailJoinForD = @"
            INNER JOIN (
                SELECT ActivityId, MAX(ApprovalRequestDetailId) AS LatestDetailId
                FROM dbo.SalesApprovalRequestDetail
                WHERE ApprovalRequestId = @Id
                GROUP BY ActivityId
            ) _latestDetail ON d.ApprovalRequestDetailId = _latestDetail.LatestDetailId";

        public static DataTable KeepLatestPerActivity(DataTable source)
        {
            if (source == null || source.Rows.Count == 0)
                return source ?? new DataTable();

            if (!source.Columns.Contains("ActivityId") || !source.Columns.Contains("ApprovalRequestDetailId"))
                return source;

            bool hasVersion = source.Columns.Contains("DetailVersion");

            var ordered = source.AsEnumerable()
                .GroupBy(r => Convert.ToInt32(r["ActivityId"]))
                .Select(g => g
                    .OrderByDescending(r => hasVersion && r["DetailVersion"] != DBNull.Value
                        ? Convert.ToInt32(r["DetailVersion"])
                        : 0)
                    .ThenByDescending(r => Convert.ToInt32(r["ApprovalRequestDetailId"]))
                    .First());

            if (source.Columns.Contains("ActivityDate"))
            {
                ordered = ordered.OrderBy(r =>
                    r["ActivityDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["ActivityDate"]));
            }

            DataTable result = source.Clone();
            foreach (var row in ordered)
                result.ImportRow(row);

            return result;
        }
    }
}
