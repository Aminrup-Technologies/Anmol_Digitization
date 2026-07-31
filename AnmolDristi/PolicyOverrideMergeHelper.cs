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
        private static readonly Dictionary<string, string> JsonKeyAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "MaxKM", "Resolved_MaxKM" },
            { "RatePerKm", "Resolved_RatePerKm" },
            { "DA", "Resolved_DA" },
            { "HotelCap", "Resolved_HotelCap" },
            { "OutstationDA", "Resolved_OutstationDA" },
            { "Outstation_DA", "Resolved_OutstationDA" },
            { "OutstationDa", "Resolved_OutstationDA" },
            { "OwnArrangementDA", "Resolved_OwnAllowance" },
            { "OwnArrangementAllowance", "Resolved_OwnAllowance" },
            { "OwnArrangementDa", "Resolved_OwnAllowance" },
            { "OwnArrangement_DA", "Resolved_OwnAllowance" },
            { "Remarks", "Resolved_Remarks" },
            { "resolved_remarks", "Resolved_Remarks" }
        };

        private static readonly Type ResolvedPolicyType = typeof(ResolvedPolicyDto);

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

            foreach (JProperty jsonProperty in json.Properties())
            {
                if (jsonProperty.Value == null || jsonProperty.Value.Type == JTokenType.Null) continue;

                PropertyInfo property = ResolveTargetProperty(jsonProperty.Name);
                if (property == null || !property.CanWrite) continue;

                object converted = ConvertToken(jsonProperty.Value, property.PropertyType);
                if (converted == null && property.PropertyType != typeof(string)) continue;

                property.SetValue(target, converted, null);
            }
        }

        private static PropertyInfo ResolveTargetProperty(string jsonKey)
        {
            string aliasName;
            if (JsonKeyAliases.TryGetValue(jsonKey, out aliasName))
                return ResolvedPolicyType.GetProperty(aliasName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            PropertyInfo direct = ResolvedPolicyType.GetProperty(jsonKey, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (direct != null) return direct;

            string resolvedName = "Resolved_" + jsonKey;
            return ResolvedPolicyType.GetProperty(resolvedName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        }

        private static object ConvertToken(JToken token, Type targetType)
        {
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
