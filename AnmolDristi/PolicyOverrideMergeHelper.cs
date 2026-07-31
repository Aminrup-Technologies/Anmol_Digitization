using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace AnmolDristi
{
    /// <summary>
    /// Merges PolicyException override JSON onto an existing ResolvedPolicyDto without replacing the whole object.
    /// Only JSON properties that are present and non-null are applied.
    /// </summary>
    public static class PolicyOverrideMergeHelper
    {
        private static readonly HashSet<string> ExcludedMetadataProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Source",
            "RowId",
            "EmployeeCode",
            "RegionId",
            "BranchId",
            "GradeId",
            "DesignationId",
            "PolicyJson",
            "Priority",
            "EffectiveFrom",
            "EffectiveTo",
            "IsActive",
            "Specificity",
            "IsPolicyExceptionApplied",
            "PolicyExceptionId",
            "PolicySource",
            "AppliedPriority"
        };

        private static readonly Dictionary<string, PropertyInfo> JsonKeyPropertyMap = BuildJsonKeyPropertyMap();

        public static void ApplyPartialOverrides(ResolvedPolicyDto target, string overrideJson)
        {
            if (target == null || string.IsNullOrWhiteSpace(overrideJson)) return;

            JObject json;
            try
            {
                json = JObject.Parse(overrideJson);
            }
            catch
            {
                return;
            }

            if (json == null) return;

            foreach (JProperty jsonProperty in json.Properties())
            {
                if (jsonProperty == null || jsonProperty.Value == null || jsonProperty.Value.Type == JTokenType.Null)
                    continue;

                PropertyInfo property;
                if (!TryResolveTargetProperty(jsonProperty.Name, out property))
                    continue;

                object converted = ConvertToken(jsonProperty.Value, property.PropertyType);
                if (converted == null && property.PropertyType != typeof(string))
                    continue;

                SafeSetProperty(property, target, converted);
            }
        }

        private static Dictionary<string, PropertyInfo> BuildJsonKeyPropertyMap()
        {
            var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            PropertyInfo[] properties = typeof(ResolvedPolicyDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (!IsMergeableProperty(property)) continue;

                map[property.Name] = property;

                const string resolvedPrefix = "Resolved_";
                if (property.Name.StartsWith(resolvedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string shortName = property.Name.Substring(resolvedPrefix.Length);
                    if (!map.ContainsKey(shortName))
                        map[shortName] = property;
                }
            }

            AddAlias(map, "Outstation_DA", "Resolved_OutstationDA", properties);
            AddAlias(map, "OutstationDa", "Resolved_OutstationDA", properties);
            AddAlias(map, "OwnArrangementDA", "Resolved_OwnAllowance", properties);
            AddAlias(map, "OwnArrangementAllowance", "Resolved_OwnAllowance", properties);
            AddAlias(map, "OwnArrangementDa", "Resolved_OwnAllowance", properties);
            AddAlias(map, "OwnArrangement_DA", "Resolved_OwnAllowance", properties);
            AddAlias(map, "resolved_remarks", "Resolved_Remarks", properties);

            return map;
        }

        private static void AddAlias(Dictionary<string, PropertyInfo> map, string jsonKey, string propertyName, PropertyInfo[] properties)
        {
            if (map.ContainsKey(jsonKey)) return;

            for (int i = 0; i < properties.Length; i++)
            {
                if (string.Equals(properties[i].Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    map[jsonKey] = properties[i];
                    return;
                }
            }
        }

        private static bool IsMergeableProperty(PropertyInfo property)
        {
            return property != null
                && property.CanWrite
                && !ExcludedMetadataProperties.Contains(property.Name);
        }

        private static bool TryResolveTargetProperty(string jsonKey, out PropertyInfo property)
        {
            property = null;
            if (string.IsNullOrWhiteSpace(jsonKey) || JsonKeyPropertyMap == null) return false;
            return JsonKeyPropertyMap.TryGetValue(jsonKey.Trim(), out property) && property != null;
        }

        private static void SafeSetProperty(PropertyInfo property, ResolvedPolicyDto target, object value)
        {
            if (property == null || target == null) return;

            try
            {
                Type propertyType = property.PropertyType;

                if (value == null)
                {
                    if (!propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null)
                        property.SetValue(target, null, null);
                    return;
                }

                Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                if (!underlyingType.IsInstanceOfType(value))
                    value = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);

                property.SetValue(target, value, null);
            }
            catch
            {
                // Ignore individual property assignment failures and continue merging.
            }
        }

        private static object ConvertToken(JToken token, Type targetType)
        {
            if (token == null || targetType == null) return null;

            Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying == typeof(string))
                return Convert.ToString(token);

            if (underlying == typeof(decimal))
            {
                decimal value;
                if (decimal.TryParse(Convert.ToString(token), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                    return value;
                return null;
            }

            if (underlying == typeof(int))
            {
                int value;
                if (int.TryParse(Convert.ToString(token), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                    return value;
                return null;
            }

            if (underlying == typeof(bool))
            {
                bool value;
                if (bool.TryParse(Convert.ToString(token), out value))
                    return value;
                return null;
            }

            if (underlying == typeof(DateTime))
            {
                DateTime value;
                if (DateTime.TryParse(Convert.ToString(token), CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
                    return value;
                return null;
            }

            return null;
        }
    }
}
