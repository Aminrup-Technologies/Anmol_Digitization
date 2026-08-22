// ========================================================================
// When: 2026-08-22
// What: ApprovalConfigurationService — manages request-type approval flags
//       and the global IT Head approver configuration.
// Why: Prompt 1.6 — IT Head is administratively configurable, not hard-coded.
//       Privileged changes now require the authenticated actor code and the
//       IT Head switch is performed in a transaction-safe sequence so the
//       filtered unique active-row index cannot be violated by SaveChanges
//       ordering.
// ========================================================================

using System;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    public class RequestTypeConfigDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool RequiresManagerApproval { get; set; }
        public bool RequiresItHeadApproval { get; set; }
        public bool IsActive { get; set; }
    }

    public class ItHeadConfigDto
    {
        public int Id { get; set; }
        public string ItHeadApproverCode { get; set; }
        public string ItHeadApproverName { get; set; }
        public string ItHeadApproverStatus { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// BLL service for ITSM approval configuration.
    /// All reads use AsNoTracking() + Select projections.
    /// </summary>
    public class ApprovalConfigurationService
    {
        public System.Collections.Generic.List<RequestTypeConfigDto> GetRequestTypes()
        {
            using (var context = new ItsmDbContext())
            {
                return context.RequestTypes
                    .AsNoTracking()
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Code)
                    .Select(r => new RequestTypeConfigDto
                    {
                        Id = r.Id,
                        Code = r.Code,
                        Name = r.Name,
                        RequiresManagerApproval = r.RequiresManagerApproval,
                        RequiresItHeadApproval = r.RequiresItHeadApproval,
                        IsActive = r.IsActive
                    })
                    .ToList();
            }
        }

        /// <summary>
        /// Updates request-type approval flags.
        /// The actor is required so the UI cannot perform an anonymous BLL mutation.
        /// </summary>
        public void UpdateRequestTypeApproval(
            int requestTypeId,
            bool requiresManager,
            bool requiresItHead,
            string actorCode)
        {
            if (string.IsNullOrWhiteSpace(actorCode))
            {
                throw new ArgumentException("Actor code is required.", nameof(actorCode));
            }

            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    EnsureActiveActor(context, actorCode);

                    var requestType = context.RequestTypes
                        .FirstOrDefault(r => r.Id == requestTypeId && r.IsActive);

                    if (requestType == null)
                    {
                        throw new InvalidOperationException(
                            "Request type not found: " + requestTypeId);
                    }

                    requestType.RequiresManagerApproval = requiresManager;
                    requestType.RequiresItHeadApproval = requiresItHead;
                    context.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public ItHeadConfigDto GetActiveItHead()
        {
            using (var context = new ItsmDbContext())
            {
                return context.ApprovalConfigurations
                    .AsNoTracking()
                    .Where(ac => ac.IsActive)
                    .Join(
                        context.UserMasters.AsNoTracking(),
                        ac => ac.ItHeadApproverCode,
                        um => um.EmployeeCode,
                        (ac, um) => new ItHeadConfigDto
                        {
                            Id = ac.Id,
                            ItHeadApproverCode = ac.ItHeadApproverCode,
                            ItHeadApproverName = um.EmployeeName,
                            ItHeadApproverStatus = um.EmployeeStatus,
                            IsActive = ac.IsActive
                        })
                    .FirstOrDefault();
            }
        }

        public System.Collections.Generic.List<ItHeadConfigDto> GetAllItHeadConfigs()
        {
            using (var context = new ItsmDbContext())
            {
                return context.ApprovalConfigurations
                    .AsNoTracking()
                    .OrderByDescending(ac => ac.IsActive)
                    .ThenByDescending(ac => ac.CreatedOn)
                    .Join(
                        context.UserMasters.AsNoTracking(),
                        ac => ac.ItHeadApproverCode,
                        um => um.EmployeeCode,
                        (ac, um) => new ItHeadConfigDto
                        {
                            Id = ac.Id,
                            ItHeadApproverCode = ac.ItHeadApproverCode,
                            ItHeadApproverName = um.EmployeeName,
                            ItHeadApproverStatus = um.EmployeeStatus,
                            IsActive = ac.IsActive
                        })
                    .ToList();
            }
        }

        /// <summary>
        /// Sets the active IT Head approver.
        /// Existing active rows are committed inactive before a new/existing row
        /// is activated, preventing the filtered unique index from being hit by
        /// EF6 update ordering in a single SaveChanges call.
        /// </summary>
        public void SetActiveItHead(string employeeCode, string actorCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                throw new ArgumentException("Employee code is required.", nameof(employeeCode));
            }

            if (string.IsNullOrWhiteSpace(actorCode))
            {
                throw new ArgumentException("Actor code is required.", nameof(actorCode));
            }

            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    EnsureActiveActor(context, actorCode);

                    var employee = context.UserMasters
                        .AsNoTracking()
                        .FirstOrDefault(u => u.EmployeeCode == employeeCode);

                    if (employee == null)
                    {
                        throw new InvalidOperationException(
                            "Employee not found: " + employeeCode);
                    }

                    if (!string.Equals(
                        employee.EmployeeStatus,
                        "Active",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "Employee is not active: " + employeeCode
                            + " (" + employee.EmployeeName + ")");
                    }

                    var activeConfig = context.ApprovalConfigurations
                        .FirstOrDefault(ac => ac.IsActive);

                    if (activeConfig != null
                        && string.Equals(
                            activeConfig.ItHeadApproverCode,
                            employeeCode,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        transaction.Commit();
                        return;
                    }

                    if (activeConfig != null)
                    {
                        activeConfig.IsActive = false;
                        activeConfig.ModifiedOn = DateTime.Now;
                        context.SaveChanges();
                    }

                    var existingForEmployee = context.ApprovalConfigurations
                        .FirstOrDefault(ac => ac.ItHeadApproverCode == employeeCode);

                    if (existingForEmployee != null)
                    {
                        existingForEmployee.IsActive = true;
                        existingForEmployee.ModifiedOn = DateTime.Now;
                    }
                    else
                    {
                        context.ApprovalConfigurations.Add(new ApprovalConfiguration
                        {
                            ItHeadApproverCode = employeeCode,
                            IsActive = true,
                            CreatedOn = DateTime.Now
                        });
                    }

                    context.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public System.Collections.Generic.List<AgentLookupDto> GetActiveEmployees()
        {
            using (var context = new ItsmDbContext())
            {
                return context.UserMasters
                    .AsNoTracking()
                    .Where(u => u.EmployeeStatus == "Active")
                    .OrderBy(u => u.EmployeeName)
                    .Select(u => new AgentLookupDto
                    {
                        EmployeeCode = u.EmployeeCode,
                        EmployeeName = u.EmployeeName
                    })
                    .ToList();
            }
        }

        private static void EnsureActiveActor(ItsmDbContext context, string actorCode)
        {
            var actorExists = context.UserMasters
                .AsNoTracking()
                .Any(u => u.EmployeeCode == actorCode
                          && u.EmployeeStatus == "Active");

            if (!actorExists)
            {
                throw new InvalidOperationException(
                    "The configuration actor is not an active employee.");
            }
        }
    }
}
