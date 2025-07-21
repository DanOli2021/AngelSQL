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
#load "..\POSApi\Sku.csx"

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
    "SaveImport" => SaveImport(api, translation),
    "Get" => Get(api, translation),
    "GetMany" => GetMany(api, translation),
    "UpsertClassification" => UpsertClassification(api, translation),
    "Delete" => Delete(api, translation),
    _ => $"Error: No service found {api.OperationType}",
};



string SaveImport(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    if (string.IsNullOrEmpty(data))
    {
        return "Error: No data provided.";
    }

    if (data.StartsWith("{") == true)
    {
        data = "[" + data + "]";
    }

    DataTable dt = db.GetDataTable(data);

    List<SkuClassification> classifications = [];

    try
    {

        int n = 0;

        // Recorrer el DataTable
        foreach (DataRow row in dt.Rows)
        {

            if (row["classification_id"].ToString().Trim() == "")
            {
                return "Error: Classification is empty in row " + n.ToString() + " " + row["description"].ToString();
            }

            if (row["description"].ToString().Trim() == "")
            {
                return "Error: Description is empty in row " + n.ToString();
            }

            if (row["Type"].ToString().Trim() == "")
            {
                return "Error: Type is empty in row " + n.ToString();
            }

            string base64Image = "";

            if (row["Image"].ToString().Trim().Contains("base64") == true)
            {
                string directory = server_db.Prompt($"VAR db_wwwroot", true) + $"/images/classification/{api.account}";

                if (Directory.Exists(directory) == false)
                {
                    Directory.CreateDirectory(directory);
                }

                string path = AngelDB.Base64Helper.SaveBase64ToAutoNamedFile(row["Image"].ToString(), directory, row["classification_id"].ToString().Trim().ToUpper());

                if (path == "Error:")
                {
                    return "Error: Unable to save image";
                }

                if (row["Image"].ToString().Contains("?t"))
                {
                    row["Image"] = row["Image"].ToString().Split("?t=")[0];
                }

                row["Image"] = $"../images/classification/{api.account}/" + row["classification_id"].ToString().Trim().ToUpper() + Path.GetExtension(path);

                base64Image = row["Image"].ToString();
            }

            SkuClassification classification = new()
            {
                Id = row["classification_id"].ToString().Trim().ToUpper(),
                Description = row["Description"].ToString(),
                Type = row["Type"].ToString(),
                Image = row["Image"].ToString(),
                ImageBase64 = base64Image,
            };

            classifications.Add(classification);

            ++n;

        }

        result = db.UpsertInto("SkuClassification", classifications);

        if (result.StartsWith("Error:"))
        {
            return result + " (2)";
        }

        return result;

    }
    catch (Exception ex)
    {
        return "Error: " + ex.Message + " " + ex.StackTrace;
    }

}


string GetMany(AngelApiOperation api, Translations translation)
{

    //string result = IsTokenValid(api, translation, "ANY");

    //if (result.StartsWith("Error:"))
    //{
    //    return result;
    //}

    string data = api.DataMessage.ToString().Trim();

    if (data.Length < 3)
    {
        return "Error: " + translation.Get("Data is too short", api.UserLanguage);
    }

    string fields = "id, timestamp, Type, Description, Image";
    if (data == ":ALL")
    {
        return db.Prompt($"SELECT {fields} FROM SkuClassification WHERE id IS NOT NULL AND id <> '' ORDER BY id", true);
    }

    if (data == ":TODAY")
    {
        string today = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd");
        return db.Prompt($"SELECT {fields} FROM SkuClassification WHERE timestamp >= '" + today + " 00:00' ORDER BY timestamp DESC", true);
    }

    string result = db.Prompt($"SELECT {fields} FROM SkuClassification WHERE id LIKE '%{api.DataMessage.ToString()}%' OR description LIKE '%{api.DataMessage.ToString().Replace(" ", "%")}%' ORDER BY description LIMIT 25", true);
    return result;

}



string UpsertClassification(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "POS_DATA_UPSERT, STAKEHOLDER, SUPERVISOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString();

    if (data.StartsWith("{") && data.EndsWith("}"))
    {
        data = "[" + data + "]";
    }

    List<SkuClassification> classifications = db.jSonDeserialize<List<SkuClassification>>(data);

    foreach (SkuClassification classification in classifications)
    {

        if (classification.Id == null || classification.Id.Trim() == "")
        {
            return "Error: Classification id is empty.";
        }

        if (classification.Description == null || classification.Description.Trim() == "")
        {
            return "Error: Classification description is empty. " + classification.Id;
        }

        result = db.UpsertInto("SkuClassification", classification);

        if (result.StartsWith("Error:"))
        {
            return result + " (2)";
        }

    }

    return "Ok.";

}




string Get(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api,  "CASHIER, STAKEHOLDER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM SkuClassification WHERE id = '" + data + "'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: " + translation.Get("No data found.", api.UserLanguage);
    }

    result = result[1..^1];

    return result;

}


string Delete(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "STAKEHOLDER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT id FROM SkuClassification WHERE id = '{data}'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get("Classification not found", api.UserLanguage);
    }

    result = db.Prompt($"DELETE FROM SkuClassification PARTITION KEY main WHERE id = '{data}'", true);

    return result;

}

