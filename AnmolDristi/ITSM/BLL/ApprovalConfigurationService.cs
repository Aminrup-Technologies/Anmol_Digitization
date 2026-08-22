// ========================================================================
// When: 2026-08-22
// What: ApprovalConfigurationService — manages request-type approval flags
//       and the global IT Head approver configuration.
// Why: Prompt 1.6 — IT Head is administratively configurable, not hard-coded.
// ========================================================================

using System;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// DTO for request-type approval configuration display/edit.
    /// </summary>
    public class RequestTypeConfigDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool RequiresManagerApproval { get; set; }
        public bool RequiresItHeadApproval { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for IT Head configuration display/edit.
    /// </summary>
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
        // --- Request Type Configuration ---

        /// <summary>
        /// Returns all request types with approval flags for admin display.
        /// </summary>
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
        /// Updates approval flags for a specific request type.
        /// </summary>
        public void UpdateRequestTypeApproval(int requestTypeId, bool requiresManager, bool requiresItHead)
        {
            using (var context = new ItsmDbContext())
            {
                var requestType = context.RequestTypes.FirstOrDefault(r => r.Id == requestTypeId);
                if (requestType == null)
                {
                    throw new InvalidOperationException("Request type not found: " + requestTypeId);
                }

                requestType.RequiresManagerApproval = requiresManager;
                requestType.RequiresItHeadApproval = requiresItHead;
                context.SaveChanges();
            }
        }

        // --- IT Head Configuration ---

        /// <summary>
        /// Returns the currently active IT Head configuration.
        /// Returns null if no active configuration exists.
        /// </summary>
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

        /// <summary>
        /// Returns all IT Head configurations (for admin history view).
        /// </summary>
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
        /// Sets the active IT Head approver. Deactivates any previously active configuration.
        /// Validates that the employee is active in MST_UserMaster.
        /// </summary>
        public void SetActiveItHead(string employeeCode, string actorCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                throw new ArgumentException("Employee code is required.", nameof(employeeCode));
            }

            using (var context = new ItsmDbContext())
            {
                // Validate the employee exists and is active.
                var employee = context.UserMasters
                    .AsNoTracking()
                    .FirstOrDefault(u => u.EmployeeCode == employeeCode);

                if (employee == null)
                {
                    throw new InvalidOperationException(
                        "Employee not found: " + employeeCode);
                }

                if (!string.Equals(employee.EmployeeStatus, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Employee is not active: " + employeeCode + " (" + employee.EmployeeName + ")");
                }

                // Deactivate all existing configurations.
                var existing = context.ApprovalConfigurations
                    .Where(ac => ac.IsActive)
                    .ToList();

                foreach (var config in existing)
                {
                    config.IsActive = false;
                    config.ModifiedOn = DateTime.Now;
                }

                // Check if a configuration for this employee already exists.
                var existingForEmployee = context.ApprovalConfigurations
                    .FirstOrDefault(ac => ac.ItHeadApproverCode == employeeCode);

                if (existingForEmployee != null)
                {
                    // Reactivate existing row.
                    existingForEmployee.IsActive = true;
                    existingForEmployee.ModifiedOn = DateTime.Now;
                }
                else
                {
                    // Create new configuration.
                    context.ApprovalConfigurations.Add(new ApprovalConfiguration
                    {
                        ItHeadApproverCode = employeeCode,
                        IsActive = true,
                        CreatedOn = DateTime.Now
                    });
                }

                context.SaveChanges();
            }
        }

        /// <summary>
        /// Returns active employees for the IT Head dropdown.
        /// </summary>
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
    }
}
