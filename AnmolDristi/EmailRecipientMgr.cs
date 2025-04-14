using System.Collections.Generic;
using System.Configuration;
using Newtonsoft.Json;

public static class EmailRecipientManager
{
    public static List<string> GetRecipients(string purpose)
    {
        var recipientJson = ConfigurationManager.AppSettings["EmailRecipients"];
        var recipientDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(recipientJson);

        if (recipientDictionary != null && recipientDictionary.ContainsKey(purpose))
        {
            return recipientDictionary[purpose];
        }

        return new List<string>(); // Return empty list if no recipients found
    }
}
