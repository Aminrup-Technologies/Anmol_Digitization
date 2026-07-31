using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnmolDristi
{
    public static class PolicyExceptionSerializer
    {
        public static PolicyExceptionDto Deserialize(string policyJson)
        {
            var dto = new PolicyExceptionDto();
            if (string.IsNullOrWhiteSpace(policyJson)) return dto;

            try
            {
                JObject json = JObject.Parse(policyJson);
                dto.MaxKM = ReadDecimal(json, "MaxKM");
                dto.RatePerKm = ReadDecimal(json, "RatePerKm");
                dto.DA = ReadDecimal(json, "DA");
                dto.HotelCap = ReadDecimal(json, "HotelCap");
                dto.OutstationDA = ReadDecimal(json, "OutstationDA", "Outstation_DA", "OutstationDa");
                dto.OwnAllowance = ReadDecimal(json, "OwnArrangementDA", "OwnArrangementAllowance", "OwnArrangementDa", "OwnArrangement_DA");
                dto.Remarks = ReadString(json, "Remarks", "resolved_remarks");
            }
            catch
            {
                // Return empty DTO when JSON is invalid; caller validates on save.
            }

            return dto;
        }

        public static string Serialize(PolicyExceptionDto dto)
        {
            if (dto == null) return "{}";

            var json = new JObject();
            WriteDecimal(json, "MaxKM", dto.MaxKM);
            WriteDecimal(json, "RatePerKm", dto.RatePerKm);
            WriteDecimal(json, "DA", dto.DA);
            WriteDecimal(json, "HotelCap", dto.HotelCap);
            WriteDecimal(json, "OutstationDA", dto.OutstationDA);
            WriteDecimal(json, "OwnArrangementDA", dto.OwnAllowance);
            if (!string.IsNullOrWhiteSpace(dto.Remarks))
                json["Remarks"] = dto.Remarks.Trim();

            return json.ToString(Formatting.None);
        }

        private static decimal? ReadDecimal(JObject json, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (json[key] == null || json[key].Type == JTokenType.Null) continue;
                decimal value;
                if (decimal.TryParse(Convert.ToString(json[key]), out value)) return value;
            }
            return null;
        }

        private static string ReadString(JObject json, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (json[key] == null || json[key].Type == JTokenType.Null) continue;
                string value = Convert.ToString(json[key]);
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            }
            return null;
        }

        private static void WriteDecimal(JObject json, string key, decimal? value)
        {
            if (value.HasValue) json[key] = value.Value;
        }
    }
}
