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
    "UpsertInventory" => UpsertInventory(api, translation),
    "Delete" => Delete(api, translation),
    "GetKardex" => GetKardex(),
    _ => $"Error: No service found {api.OperationType}",
};


string GetKardex()
{

    string result = IsTokenValid(api, "STEAKHOLDER, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (api.DataMessage == null)
    {
        return "Error: No data provided.";
    }

    if( api.DataMessage.Search == null)
    {
        api.DataMessage.Search = ":ALL";
    }

    if (api.DataMessage.Search.ToString().Trim() == "")
    {
        api.DataMessage.Search = ":ALL";
    }

    if( api.DataMessage.Start_date == null)
    {
        return "Error: No start date provided.";
    }

    if( api.DataMessage.End_date == null)
    {
        return "Error: No end date provided.";
    }

    if (api.DataMessage.Sku_id == null)
    {
        return "Error: No Sku_id provided.";
    }

    if (api.DataMessage.Storage_id == null)
    {
        return "Error: No Storage_id provided.";
    }

    string search = api.DataMessage.Search.ToString().Trim();
    string start_date = api.DataMessage.Start_date.ToString().Trim() + " 00:00:00";
    string end_date = api.DataMessage.End_date.ToString().Trim() + " 23:59:59";

    string start_partion = start_date.Substring(0, 7);
    string end_partion = end_date.Substring(0, 7);

    string fields = "*";

    if (search == ":ALL")
    {
        return db.Prompt($"SELECT {fields} FROM Kardex PARTITION KEY partition >= '{start_partion}' AND partition <= '{end_partion}' WHERE Sku_id = '{api.DataMessage.Sku_id}' AND Storage_id = '{api.DataMessage.Storage_id}' AND DateTime >= '{start_date}' AND DateTime <= '{end_date}' ORDER BY DateTime DESC", true);
    }

    return db.Prompt($"SELECT {fields} FROM Kardex PARTITION KEY partition >= '{start_partion}' AND partition <= '{end_partion}' WHERE Sku_id = '{api.DataMessage.Sku_id}' AND Storage_id = '{api.DataMessage.Storage_id}' AND (ReferenceDocument LIKE '%{search}%' OR Sku_description LIKE '%{search}%') AND DateTime >= '{start_date}' AND DateTime <= '{end_date}' ORDER BY DateTime DESC", true);

}



string SaveImport(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "STEAKHOLDER, SUPERVISOR, ADMINISTRATOR");

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

    List<Inventory> Inventories = [];

    try
    {

        int n = 0;

        // Recorrer el DataTable
        foreach (DataRow row in dt.Rows)
        {

            if (row["Inventory_id"].ToString().Trim() == "")
            {
                return "Error: Inventory is empty in row " + n.ToString() + " " + row["description"].ToString();
            }

            if (row["description"].ToString().Trim() == "")
            {
                return "Error: Description is empty in row " + n.ToString();
            }

            if (row["exchange_rate"].ToString().Trim() == "")
            {
                return "Error: Exchange rate is empty in row " + n.ToString();
            }

            if (row["symbol"].ToString().Trim() == "")
            {
                return "Error: symbol is empty in row " + n.ToString();
            }

            Inventory Inventory = new()
            {
                Id = row["Inventory_id"].ToString().Trim().ToUpper(),
                Description = row["Description"].ToString(),
            };

            Inventories.Add(Inventory);

            ++n;

        }

        Object clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(Inventories);
        result = db.UpsertInto("Inventory", clone);

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

    string result = IsTokenValid(api, "CASHIER, STAKEHOLDER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (api.DataMessage == null)
    {
        return "Error: No data provided.";
    }

    if (api.DataMessage.Search == null)
    {
        api.DataMessage.Search = ":ALL";
    }

    if (api.DataMessage.Search.ToString().Trim() == "")
    {
        api.DataMessage.Search = ":ALL";
    }

    if (api.DataMessage.Warehouse == null)
    {
        api.DataMessage.Warehouse = ":ALL";
    }

    string search = api.DataMessage.Search.ToString().Trim();
    string Warehouse = api.DataMessage.Warehouse.ToString().Trim();

    string fields = "id, Storage_id, description, stock, dateTime, user_id";

    if (search == ":ALL")
    {
        if (Warehouse != ":ALL")
        {
            return db.Prompt($"SELECT {fields} FROM Inventory WHERE Storage_id = '{api.DataMessage.Warehouse}' ORDER BY id", true);
        }

        return db.Prompt($"SELECT {fields} FROM Inventory ORDER BY id", true);
    }

    if (search == ":TODAY")
    {
        string today = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd");

        if (Warehouse != ":ALL")
        {
            return db.Prompt($"SELECT {fields} FROM Inventory WHERE Storage_id = '{Warehouse}' AND timestamp >= '" + today + " 00:00' ORDER BY timestamp DESC", true);
        }

        return db.Prompt($"SELECT {fields} FROM Inventory WHERE timestamp >= '" + today + " 00:00' ORDER BY timestamp DESC", true);
    }

    if (Warehouse != ":ALL")
    {        
        return db.Prompt($"SELECT {fields} FROM Inventory WHERE Storage_id = '{Warehouse}' AND id LIKE '%{api.DataMessage.ToString()}%' OR description LIKE '%{api.DataMessage.ToString().Replace(" ", "%")}%' ORDER BY id", true);
    }

    result = db.Prompt($"SELECT {fields} FROM Inventory WHERE id LIKE '%{api.DataMessage.ToString()}%' OR description LIKE '%{api.DataMessage.ToString().Replace(" ", "%")}%' ORDER BY description LIMIT 25", true);
    return result;

}



string UpsertInventory(AngelApiOperation api, Translations translation)
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

    List<Inventory> Inventories = db.jSonDeserialize<List<Inventory>>(data);

    foreach (Inventory Inventory in Inventories)
    {

        if (Inventory.Id == null || Inventory.Id.Trim() == "")
        {
            return "Error: Sku id is empty.";
        }

        if (Inventory.Description == null || Inventory.Description.Trim() == "")
        {
            return "Error: Inventory description is empty. " + Inventory.Id;
        }

        Object Inventory_clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(Inventory);
        result = db.UpsertInto("Inventory", Inventory_clone);

        if (result.StartsWith("Error:"))
        {
            return result + " (2)";
        }

    }

    return "Ok.";

}




string Get(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "CASHIER, STAKEHOLDER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM Inventory WHERE id = '" + data + "'", true);

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

    result = db.Prompt($"SELECT * FROM Inventory WHERE id = '{data}'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get("Inventory not found", api.UserLanguage);
    }

    result = db.Prompt($"DELETE FROM Inventory PARTITION KEY main WHERE id = '{data}'", true);

    return result;

}
