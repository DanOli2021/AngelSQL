// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

// Process to send messages to user
// Daniel() Oliver Rojas
// 2024-08-25

// This script works as an API so that different applications
// can affect sales, purchases, inventory entries and exits,
// physical inventories, accounts receivable and payable.

#load "translations.csx"
#load "..\AngelComm\AngelComm.csx"
#load "..\POSApi\BusinessInfo.csx"

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using System.Reflection;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;

private AngelApiOperation api = JsonConvert.DeserializeObject<AngelApiOperation>(message);
api.db = db;
api.server_db = server_db;

//Server parameters
private Dictionary<string, string> parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Environment.GetEnvironmentVariable("ANGELSQL_PARAMETERS"));
private Translations translation = new();
translation.SpanishValues();

// This is the main function that will be called by the API
return api.OperationType switch
{
    "SaveBusinessInfo" => SaveBusinessInfo(api, translation),
    "GetBusinessInfo" => GetBusinessInfo(api, translation),
    _ => $"Error: No service found {api.OperationType}",
};



string SaveBusinessInfo(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "STAKEHOLDER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    if (string.IsNullOrEmpty(data))
    {
        return "Error: No data provided.";
    }

    BusinessInfo bi = db.jSonDeserialize<BusinessInfo>(data);
    bi.Id = "1"; // Set a default ID for the business info

    if (bi == null)
    {
        return "Error: Invalid data format.";
    }

    if (bi.Name == null || bi.Name == "")
    {
        return "Error: Name is required.";
    }

    if (bi.Logo != null || bi.Logo != "")
    {
        if (bi.Logo.Contains("base64"))
        {
            string directory = server_db.Prompt($"VAR db_wwwroot", true) + "/images";

            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            string path = AngelDB.Base64Helper.SaveBase64ToAutoNamedFile(bi.Logo, directory, "logo");

            if (path == "Error:")
            {
                return "Error: Unable to save image";
            }

            bi.Logo = "../images/logo" + Path.GetExtension(path);

        }
    }

    result = db.UpsertInto("BusinessInfo", bi);
    return result;

}



string GetBusinessInfo(AngelApiOperation api, Translations translation)
{

    string result = db.Prompt("SELECT * FROM BusinessInfo WHERE Id = '1'");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: No data found.";
    }

    List<BusinessInfo> businessInfoList = db.jSonDeserialize<List<BusinessInfo>>(result);
    BusinessInfo businessInfo = businessInfoList[0];

    return db.GetJson(businessInfo);

}
