// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

#undef DEBUG

// Process to send messages to user
// Daniel() Oliver Rojas
// 2024-08-25

// This script works as an API so that different applications
// can affect sales, purchases, inventory entries and exits,
// physical inventories, accounts receivable and payable.

#load "translations.csx"
#load "..\AngelComm\AngelComm.csx"
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

// Create required tables
CreateTables(db);

// This is the main function that will be called by the API
return api.OperationType switch
{
    "SaveImport" => SaveImport(api, translation),
    "Get" => Get(api, translation),
    "GetMany" => GetMany(api, translation),
    "UpsertCurrency" => UpsertCurrency(api, translation),
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

    List<Currency> currencies = [];

    try
    {

        int n = 0;

        // Recorrer el DataTable
        foreach (DataRow row in dt.Rows)
        {

            if (row["currency_id"].ToString().Trim() == "")
            {
                return "Error: Currency is empty in row " + n.ToString() + " " + row["description"].ToString();
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

            Currency currency = new()
            {
                Id = row["currency_id"].ToString().Trim().ToUpper(),
                Description = row["Description"].ToString(),
                Symbol = row["Symbol"].ToString(),
                Exchange_rate = decimal.TryParse(row["Exchange_rate"].ToString(), out decimal parsedExchangeRate) ? parsedExchangeRate : 0,
                User_id = api.User,
                User_name = ""
            };

            currencies.Add(currency);

            ++n;

        }

        Object clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(currencies);
        result = db.UpsertInto("Currency", clone);

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
        return result + "(1)";
    }

    string data = api.DataMessage.ToString().Trim();

    if( data == ":ALL")
    {
        return db.Prompt($"SELECT * FROM Currency ORDER BY id", true);
    }

    if( data == ":TODAY")
    {
        string today = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd");
        return db.Prompt($"SELECT * FROM Currency WHERE timestamp >= '" + today +  " 00:00' ORDER BY timestamp DESC", true);
    }

    result = db.Prompt($"SELECT * FROM Currency WHERE id LIKE '%{api.DataMessage.ToString()}%' OR description LIKE '%{api.DataMessage.ToString().Replace(" ", "%")}%' ORDER BY description LIMIT 25", true);
    return result;

}



string UpsertCurrency(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api,"POS_DATA_UPSERT, STAKEHOLDER, SUPERVISOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString();

    if (data.StartsWith("{") && data.EndsWith("}"))
    {
        data = "[" + data + "]";
    }

    List<Currency> currencies = db.jSonDeserialize<List<Currency>>(data);

    foreach (Currency currency in currencies)
    {

        if (currency.Id == null || currency.Id.Trim() == "")
        {
            return "Error: Sku id is empty.";
        }

        if (currency.Description == null || currency.Description.Trim() == "")
        {
            return "Error: Currency description is empty. " + currency.Id;
        }

        Object currency_clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(currency);
        result = db.UpsertInto("Currency", currency_clone);

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

    result = db.Prompt($"SELECT * FROM Currency WHERE id = '" + data + "'", true);

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

    result = db.Prompt($"SELECT * FROM Currency WHERE id = '{data}'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get("Currency not found", api.UserLanguage);
    }

    result = db.Prompt($"DELETE FROM currency PARTITION KEY main WHERE id = '{data}'", true);   

    return result;

}

private string CreateTables(AngelDB.DB db)
{
    Currency currency = new();
    db.CreateTable(currency, "Currency", false, "", true);

    string result = db.Prompt($"SELECT COUNT(*) AS 'Count' FROM Currency", true);

    if (result == "[]")
    {

        result = File.ReadAllText(server_db.Prompt("VAR db_scripts_directory") + "/pos_backend/Currency.json");

        DataTable dt = db.GetDataTable(result);

        List<Currency> currencies = [];

        foreach( DataRow r in dt.Rows ) 
        {
            Currency c = new()
            {
                Id = r["cc"].ToString(),
                Description = r["name"].ToString(),
                Symbol = r["symbol"].ToString(),
                Exchange_rate = decimal.TryParse(r["exchange_rate"].ToString(), out decimal parsedExchangeRate) ? parsedExchangeRate : 0,
            };

            currencies.Add(c);

        } 
        
        result = db.UpsertInto("Currency", currencies );

        if (result.StartsWith("Error:"))
        {
            return result + " (Currencys.json)";
        }
    }

    return "Ok.";

}


