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
#load "..\POSApi\Parameters.csx"
#load "..\POSApi\Sales.csx"

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
    "SaveImport" => Save(api, translation),
    "Get" => Get(api, translation),
    "GetMany" => GetMany(api, translation),
    "Upsertparameter" => Upsertparameter(api, translation),
    "Delete" => Delete(api, translation),
    "GetParameter" => GetParameter(),
    "GetDefaultCurrency" => GetDefaultCurrency(),
    _ => $"Error: No service found {api.OperationType}",
};



string Save(AngelApiOperation api, Translations translation)
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

    List<KioskoParameters> parameters = [];

    try
    {
        // Validar que el DataTable no esté vacío                                                                                                                                             
        int n = 0;

        // Recorrer el DataTable
        foreach (DataRow row in dt.Rows)
        {

            if (row["id"].ToString().Trim() == "")
            {
                return "Error: parameter is empty in row " + n.ToString() + " " + row["id"].ToString();
            }

            if (row["value"].ToString().Trim() == "")
            {
                return "Error: value is empty in row " + n.ToString();
            }

            if( row["id"].ToString() == "Currency" )
            {
                result = db.Prompt($"SELECT * FROM currency WHERE id = '{row["value"].ToString().Trim()}'", true);

                if( result == "[]")
                {
                    return "Error: Currency '" + row["value"].ToString().Trim() + "' not found in row " + n.ToString();
                }

            }

            KioskoParameters parameter = new()
            {
                Id = row["id"].ToString().Trim(),
                Value = row["value"].ToString(),
            };

            parameters.Add(parameter);  

            ++n;

        }

        Object clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(parameters);
        result = db.UpsertInto("KioskoParameters", clone);

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

    string data = api.DataMessage.ToString().Trim();

    if (data.Length < 3)
    {
        return "Error: " + translation.Get("Data is too short", api.UserLanguage);
    }

    if( data == ":ALL")
    {
        return db.Prompt($"SELECT * FROM KioskoParameters ORDER BY id", true);
    }

    if( data == ":TODAY")
    {
        string today = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd");
        return db.Prompt($"SELECT * FROM KioskoParameters WHERE timestamp >= '" + today +  " 00:00' ORDER BY timestamp DESC", true);
    }

    result = db.Prompt($"SELECT * FROM KioskoParameters WHERE id LIKE '%{api.DataMessage.ToString()}%' OR value LIKE '%{api.DataMessage.ToString().Replace(" ", "%")}%' ORDER BY id LIMIT 25", true);
    return result;

}



string Upsertparameter(AngelApiOperation api, Translations translation)
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

    List<KioskoParameters> parameters = db.jSonDeserialize<List<KioskoParameters>>(data);

    foreach (KioskoParameters parameter in parameters)
    {

        if (parameter.Id == null || parameter.Id.Trim() == "")
        {
            return "Error: Sku id is empty.";
        }

        if (parameter.Value == null || parameter.Value.Trim() == "")
        {
            return "Error: parameter description is empty. " + parameter.Id;
        }

        Object parameter_clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(parameter);
        result = db.UpsertInto("parameter", parameter_clone);

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

    result = db.Prompt($"SELECT * FROM KioskoParameters WHERE id = '" + data + "'", true);

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

string GetParameter()
{

    string result = IsTokenValid(api, "CASHIER, STAKEHOLDER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM KioskoParameters WHERE id = '" + data + "'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: " + translation.Get("No data found.", api.UserLanguage);
    }

    DataTable dt = db.GetDataTable(result);

    return dt.Rows[0]["value"].ToString().Trim();     

}


string GetDefaultCurrency()
{

    string result = IsTokenValid(api, "CASHIER, STAKEHOLDER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM KioskoParameters WHERE id = '" + data + "'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: " + translation.Get("No data found.", api.UserLanguage);
    }

    DataTable dt = db.GetDataTable(result);

    result = db.Prompt($"SELECT * FROM currency WHERE id = '{dt.Rows[0]["value"].ToString().Trim()}'", true);
    

    if (result == "[]")
    {
        return "Error: " + translation.Get("Currency not found", api.UserLanguage);
    }

    result = result[1..^1]; // Remove the brackets from the JSON result

    Currency currency = db.DeserializeDBResult<Currency>(result);

    return db.GetJson(currency);     

}

string Delete(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "STAKEHOLDER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM KioskoParameters WHERE id = '{data}'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get("parameter not found", api.UserLanguage);
    }

    result = db.Prompt($"DELETE FROM KioskoParameters PARTITION KEY main WHERE id = '{data}'", true);

    return result;

}