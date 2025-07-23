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
#load "..\POSApi\Inventory.csx"

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;

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
    "UpsertStorage" => UpsertStorage(api, translation),
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

    List<Storage> storages = [];

    try
    {

        int n = 0;

        // Recorrer el DataTable
        foreach (DataRow row in dt.Rows)
        {

            if (row["storage_id"].ToString().Trim() == "")
            {
                return "Error: storage is empty in row " + n.ToString();
            }

            if (row["Description"].ToString().Trim() == "")
            {
                return "Error: Description is empty in row " + n.ToString();
            }

            Storage storage = new()
            {
                Id = row["storage_id"].ToString().Trim(),
                Description = row["Description"].ToString().Trim(),
                Type = row["Type"].ToString().Trim(),
                Location = row["Location"].ToString().Trim(),
                Capacity = decimal.TryParse(row["Capacity"].ToString().Trim(), out decimal capacity) ? capacity : 0,
                CurrentUsage = decimal.TryParse(row["CurrentUsage"].ToString().Trim(), out decimal currentUsage) ? currentUsage : 0,
                User_id = api.User,
            };

            storages.Add(storage);

            ++n;

        }

        result = db.UpsertInto("storage", storages);

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

    string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    if (data.Length < 3)
    {
        return "Error: " + translation.Get("Data is too short", api.UserLanguage);
    }
    
    string fields = "id, Description, Type, Location, Capacity, CurrentUsage, User_id";

    if (data == ":ALL")
    {
        return db.Prompt($"SELECT {fields} FROM storage ORDER BY id", true);
    }

    if (data == ":TODAY")
    {
        string today = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd");
        return db.Prompt($"SELECT {fields} FROM storage WHERE timestamp >= '" + today + " 00:00' ORDER BY timestamp DESC", true);
    }

    result = db.Prompt($"SELECT {fields} FROM storage WHERE id LIKE '%{api.DataMessage.ToString()}%' OR description LIKE '%{api.DataMessage.ToString().Replace(" ", "%")}%' ORDER BY description LIMIT 25", true);
    return result;

}



string UpsertStorage(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString();

    if (data.StartsWith("{") && data.EndsWith("}"))
    {
        data = "[" + data + "]";
    }

    List<Storage> storages = db.jSonDeserialize<List<Storage>>(data);

    foreach (Storage storage in storages)
    {

        if (storage.Id == null || storage.Id.Trim() == "")
        {
            return "Error: Sku id is empty.";
        }

        if (storage.Description == null || storage.Description.Trim() == "")
        {
            return "Error: storage description is empty. " + storage.Id;
        }

        Object storage_clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(storage);
        result = db.UpsertInto("storage", storage_clone);

        if (result.StartsWith("Error:"))
        {
            return result + " (2)";
        }

    }

    return "Ok.";

}




string Get(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM storage WHERE id = '" + data + "'", true);

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

    result = db.Prompt($"SELECT * FROM storage WHERE id = '{data}'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get("storage not found", api.UserLanguage);
    }

    result = db.Prompt($"DELETE FROM storage PARTITION KEY main WHERE id = '{data}'", true);   

    return result;

}

