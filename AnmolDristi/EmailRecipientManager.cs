using System;
using System.Collections.Generic;
using System.Configuration;
using Newtonsoft.Json;

namespace AnmolDristi
{
    public class EmailRecipientManager
    {
        public static List<string> GetRecipients(string purpose, string group = null)
        {
            // Fetch the JSON string from app settings
            var recipientJson = ConfigurationManager.AppSettings["EmailRecipients"];

            // Deserialize to a dictionary structure
            var recipientDictionary = JsonConvert.DeserializeObject<Dictionary<string, PurposeGroup>>(recipientJson);

            // Check if the specified purpose exists
            if (recipientDictionary != null && recipientDictionary.ContainsKey(purpose))
            {
                var purposeGroup = recipientDictionary[purpose];

                // If no specific group is provided, return all recipients across groups
                if (string.IsNullOrEmpty(group))
                {
                    var allRecipients = new HashSet<string>();
                    foreach (var groupRecipients in purposeGroup.Groups.Values)
                    {
                        allRecipients.UnionWith(groupRecipients);
                    }
                    return new List<string>(allRecipients);
                }
                else
                {
                    // Return recipients for the specific group if it exists
                    if (purposeGroup.Groups.ContainsKey(group))
                    {
                        return purposeGroup.Groups[group];
                    }
                }
            }

            // Return an empty list if no matching purpose or group is found
            return new List<string>();
        }
    }

    // Helper class to represent the structure for purpose and groups
    public class PurposeGroup
    {
        public Dictionary<string, List<string>> Groups { get; set; }
    }
}
