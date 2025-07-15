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
    "GetBasicBusinessInfo" => GetBasicBusinessInfo(api, translation),
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
            string directory = server_db.Prompt($"VAR db_wwwroot", true) + $"/images/{api.account}";

            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            string path = AngelDB.Base64Helper.SaveBase64ToAutoNamedFile(bi.Logo, directory, "logo");

            if (path == "Error:")
            {
                return "Error: Unable to save image";
            }

            bi.LogoBase64 = bi.Logo;
            bi.Logo = $"../images/{api.account}/logo" + Path.GetExtension(path);

        }
    }

    if (bi.CentralLogo != null || bi.CentralLogo != "")
    {
        if (bi.CentralLogo.Contains("base64"))
        {
            string directory = server_db.Prompt($"VAR db_wwwroot", true) + $"/images/{api.account}";

            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            string path = AngelDB.Base64Helper.SaveBase64ToAutoNamedFile(bi.CentralLogo, directory, "CentralLogo");

            if (path == "Error:")
            {
                return "Error: Unable to save image";
            }

            bi.CentralLogoBase64 = bi.CentralLogo;
            bi.CentralLogo = $"../images/{api.account}/CentralLogo" + Path.GetExtension(path);

        }
    }


    if (bi.BackgroundImage != null || bi.BackgroundImage != "")
    {
        if (bi.BackgroundImage.Contains("base64"))
        {
            string directory = server_db.Prompt($"VAR db_wwwroot", true) + $"/images/{api.account}";

            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            string path = AngelDB.Base64Helper.SaveBase64ToAutoNamedFile(bi.BackgroundImage, directory, "background");

            if (path == "Error:")
            {
                return "Error: Unable to save image";
            }
 
            bi.BackgroundImage = $"../images/{api.account}/background" + Path.GetExtension(path);
        }
    }

    for (int i = 1; i <= 6; i++)
    {
        PropertyInfo advantageImageProp = typeof(BusinessInfo).GetProperty($"Advantage{i}Image");
        PropertyInfo advantageImageBase64Prop = typeof(BusinessInfo).GetProperty($"Advantage{i}ImageBase64");

        if (advantageImageProp != null && advantageImageBase64Prop != null)
        {
            string advantageImage = (string)advantageImageProp.GetValue(bi);
            if (!string.IsNullOrEmpty(advantageImage) && advantageImage.Contains("base64"))
            {
                string directory = server_db.Prompt($"VAR db_wwwroot", true) + $"/images/{api.account}";

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string path = AngelDB.Base64Helper.SaveBase64ToAutoNamedFile(advantageImage, directory, $"advantage{i}");

                if (path == "Error:")
                {
                    return $"Error: Unable to save image for Advantage {i}";
                }

                advantageImageBase64Prop.SetValue(bi, advantageImage);
                advantageImageProp.SetValue(bi, $"../images/{api.account}/advantage{i}" + Path.GetExtension(path));
            }
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


string GetBasicBusinessInfo(AngelApiOperation api, Translations translation)
{

    string result = db.Prompt("SELECT id,Address,Name,Phone,Email,Website,Logo,Slogan,Description FROM BusinessInfo WHERE Id = '1'");

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
