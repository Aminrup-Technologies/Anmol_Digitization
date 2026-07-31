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
        private static readonly Dictionary<string, string> JsonKeyAliases;

        private static readonly Type ResolvedPolicyType = typeof(ResolvedPolicyDto);

        static PolicyOverrideMergeHelper()
        {
            JsonKeyAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AddAlias("MaxKM", "Resolved_MaxKM");
            AddAlias("RatePerKm", "Resolved_RatePerKm");
            AddAlias("DA", "Resolved_DA");
            AddAlias("HotelCap", "Resolved_HotelCap");
            AddAlias("OutstationDA", "Resolved_OutstationDA");
            AddAlias("Outstation_DA", "Resolved_OutstationDA");
            AddAlias("OutstationDa", "Resolved_OutstationDA");
            AddAlias("OwnArrangementDA", "Resolved_OwnAllowance");
            AddAlias("OwnArrangementAllowance", "Resolved_OwnAllowance");
            AddAlias("OwnArrangementDa", "Resolved_OwnAllowance");
            AddAlias("OwnArrangement_DA", "Resolved_OwnAllowance");
            AddAlias("Remarks", "Resolved_Remarks");
            AddAlias("resolved_remarks", "Resolved_Remarks");
        }

        private static void AddAlias(string jsonKey, string propertyName)
        {
            if (!JsonKeyAliases.ContainsKey(jsonKey))
                JsonKeyAliases[jsonKey] = propertyName;
        }

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
