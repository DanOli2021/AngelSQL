// GLOBALS
// These lines of code go in each script
#r "/AngelSQLNet/AngelSQL/db.dll"
#r "/AngelSQLNet/AngelSQL/Newtonsoft.Json.dll"
// END GLOBALS

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using System.Reflection;
using System.Net;
using System.Net.Http;
using System.Text;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "IDE1006")]

public class AngelApiOperation
{
    public string api { get; set; }
    public string OperationType { get; set; }
    public string account { get; set; }
    public string Token { get; set; }
    public string User { get; set; }
    public string language { get; set; }
    public string UserLanguage { get; set; }
    public dynamic message { get; set; }
    public dynamic DataMessage { get; set; } 
    public AngelDB.DB db { get; set; } = null;
    public AngelDB.DB server_db { get; set; } = null;
}

public string Login(AngelApiOperation api, string password) 
{
    return SendToAngelPOST("tokens/admintokens", "GetTokenFromUser", new { api.User, Password = password }, api.server_db.Prompt("VAR server_tokens_url"), api);
}


string IsTokenValid(AngelApiOperation api, string group = "ANY")
{
    string result = GetGroupsUsingTocken(api);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (group == "ANY") 
    {
        return "Ok.";
    }

    dynamic user_data = JsonConvert.DeserializeObject<dynamic>(result);

    if (user_data.groups == null)
    {
        return "Error: " + "No groups found";
    }

    List<string> groups = [.. group.Split(',')];

    bool found = false;

    foreach (string g in groups)
    {
        if (user_data.groups.ToString().Trim().ToUpper().Contains(g.ToUpper().Trim()))
        {
            found = true;
            break;
        }
    }

    if (!found)
    {
        return "Error: " + "User does not have permission to edit";
    }

    return "Ok.";

}


private string GetGroupsUsingTocken(AngelApiOperation api)
{

    var d = new
    {
        TokenToObtainPermission = api.Token
    };

    string result = SendToAngelPOST("tokens/admintokens", "GetGroupsUsingTocken", d, api.server_db.Prompt("VAR server_tokens_url"), api );

    if (result.StartsWith("Error:"))
    {
        return $"Error: {result}";
    }

    return result;

}


[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "IDE0037")]

public string SendToAngelPOST(string api_name, string OPerationType, dynamic object_data, string url, AngelApiOperation api)
{

    AngelApiOperation d = new()
    {
        api = api_name,
        account = api.account,
        OperationType = OPerationType,
        Token = api.Token,
        User = api.User,
        language = "C#",
        message = new
        {
            OperationType = OPerationType,
            account = api.account,
            Token = api.Token,
            UserLanguage = api.UserLanguage,
            DataMessage = object_data
        }
    };

    string result = SendJsonToUrl(url, JsonConvert.SerializeObject(d, Formatting.Indented) );

    if (result.StartsWith("Error:")) return result;

    AngelDB.AngelResponce responce = JsonConvert.DeserializeObject<AngelDB.AngelResponce>(result);


    return responce.result;

}


string SendJsonToUrl(string url, string json)
{
    try
    {

        HttpClient web = new();
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = web.PostAsync(url, content).Result;

        if (!result.IsSuccessStatusCode)
        {
            StringBuilder sb = new();
            sb.AppendLine("Error: Reason: " + result.ReasonPhrase);
            return sb.ToString();
        }

        var byteArray = result.Content.ReadAsByteArrayAsync().Result;
        return Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);

    }
    catch (Exception e)
    {
        return $"Error: ReadUrl: {e}";
    }

}

string GetParameter(string data, AngelApiOperation api)
{

    string result = api.db.Prompt($"SELECT * FROM KioskoParameters WHERE id = '" + data + "'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: " + "No data found.";
    }

    DataTable dt = api.db.GetDataTable(result);

    return dt.Rows[0]["value"].ToString().Trim();

}
