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
#load "..\POSApi\Sales.csx"
#load "..\POSApi\Sku.csx"
#load "..\AngelComm\AngelComm.csx"

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using OpenSSL;

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
    "SalesPerMonth" => SalesPerMonth(api, translation),
    "SalesByClassification" => SalesByClassification(api, translation),
    "ProfitabilityAnalysis" => ProfitabilityAnalysis(api, translation),
    _ => $"Error: No service found {api.OperationType}"
};



string SalesPerMonth(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, translation, "STAKEHOLDER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    Dates dates = JsonConvert.DeserializeObject<Dates>(api.DataMessage.ToString());

    if (dates.StartDate == null || dates.EndDate == null)
    {
        return "Error: " + translation.Get("Dates are required", api.UserLanguage);
    }

    result = db.Prompt($"SELECT PartitionKey As Month, SUM( total ) AS 'Total' FROM sale WHERE DateTime >= '{dates.StartDate}' AND DateTime <= '{dates.EndDate}' HAVING Month IS NOT NULL ORDER BY DateTime");

    return result;

}


// Profitability analysis
string ProfitabilityAnalysis(AngelApiOperation api, Translations translation)
{
    string result = IsTokenValid(api, translation, "STAKEHOLDER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    Dates dates = JsonConvert.DeserializeObject<Dates>(api.DataMessage.ToString());

    if (dates.StartDate == null || dates.EndDate == null)
    {
        return "Error: " + translation.Get("Dates are required", api.UserLanguage);
    }


    // Sale analysis
    result = db.Prompt($"SAVE TO GRID SELECT COUNT(*) AS Receipts, SUM( subtotal ) As Total, SUM( cost ) AS Cost, SUM( subtotal - cost ) AS Utility, SUM( Number_of_items ) AS 'Number_of_items' FROM sale WHERE DateTime >= '{dates.StartDate}' AND DateTime <= '{dates.EndDate}' AS TABLE sale_data", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    result = db.Prompt($"GRID SELECT SUM(Receipts) AS Receipts, SUM(Total) AS Total, SUM(Cost) AS Cost, SUM(Utility) AS Utility, SUM(Number_of_items) AS Number_of_items FROM sale_data AS JSON");
    DataTable dt = JsonConvert.DeserializeObject<DataTable>(result);

    string result_sales_summary = db.Prompt($"SELECT substr(DateTime, 1, 10) AS Day, SUM( total ) AS 'Total', case cast (strftime('%w', DateTime) as integer) when 0 then 'Sunday' when 1 then 'Monday' when 2 then 'Tuesday' when 3 then 'Wednesday'  when 4 then 'Thursday' when 5 then 'Friday' else 'Saturday' end as weekday FROM sale WHERE DateTime >= '{dates.StartDate}' AND DateTime <= '{dates.EndDate}' GROUP BY substr(DateTime, 1, 10) HAVING Day IS NOT NULL ORDER BY DateTime");        

    if (result_sales_summary.StartsWith("Error:"))
    {
        return result_sales_summary;
    }

    result = db.Prompt("PYTHON FILE dev/scripts/pos_backend/sale_forecast.py MESSAGE " + result_sales_summary);
    //result = db.Prompt("PYTHON FILE dev/py/sample.py MESSAGE Hola mundo");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    Console.WriteLine(result);

    if( db.Prompt("PYTHON GET LAST ERROR").Length > 0)
    {
        Console.WriteLine( "Pthon Error: " + db.Prompt("PYTHON GET LAST ERROR") );
    }

    var d = new
    {
        Receipts = dt.Rows[0]["Receipts"],
        Total = dt.Rows[0]["Total"],
        Cost = dt.Rows[0]["Cost"],
        Utility = dt.Rows[0]["Utility"],
        Number_of_items = dt.Rows[0]["Number_of_items"],
        SaleByDay = JsonConvert.DeserializeObject<dynamic>(result_sales_summary),
        Forecast = JsonConvert.DeserializeObject<dynamic>(result)
    };

    //File.WriteAllText("c:/Daniel/profitability.json", result);

    return JsonConvert.SerializeObject(d, Formatting.Indented);

}

// Salves by category
public string SalesByClassification(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, translation, "STAKEHOLDER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    Dates dates = JsonConvert.DeserializeObject<Dates>(api.DataMessage.ToString());

    if (dates.StartDate == null || dates.EndDate == null)
    {
        return "Error: " + translation.Get("Dates are required", api.UserLanguage);
    }

    result = db.Prompt($"SELECT Preferential_Classification AS 'Classification', SUM( qty * price ) AS 'total' FROM sale_detail WHERE DateTime >= '{dates.StartDate}' AND DateTime <= '{dates.EndDate}' GROUP BY Preferential_Classification");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    result = db.Prompt($"SAVE TO GRID JSON {result} AS TABLE clasification");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    result = db.Prompt($"GRID SELECT Classification, SUM( total ) AS 'Total' FROM clasification GROUP BY Classification ORDER BY Classification AS JSON");

    return result;

}


class Dates
{
    public string StartDate { get; set; }
    public string EndDate { get; set; }
}


string IsTokenValid(AngelApiOperation api, Translations translation, string group = "STAKEHOLDER")
{
    string result = GetGroupsUsingTocken(api.Token, api.UserLanguage);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    dynamic user_data = JsonConvert.DeserializeObject<dynamic>(result);

    if (user_data.groups == null)
    {
        return "Error: " + translation.Get("No groups found", api.UserLanguage);
    }

    if (!user_data.groups.ToString().Contains(group))
    {
        return "Error: " + translation.Get("User does not have permission to edit", api.UserLanguage);
    }

    return "Ok.";

}


private string GetGroupsUsingTocken(string token, string language)
{

    var d = new
    {
        TokenToObtainPermission = token
    };

    string result = SendToAngelPOST("tokens/admintokens", api.User, api.Token, "GetGroupsUsingTocken", api.UserLanguage, d);

    if (result.StartsWith("Error:"))
    {
        return $"Error: {result}";
    }

    return result;

}


private string SendToAngelPOST(string api_name, string user, string token, string OPerationType, string Language, dynamic object_data)
{

    string db_account = user.Split("@")[1];

    var d = new
    {
        api = api_name,
        account = db_account,
        language = "C#",
        message = new
        {
            OperationType = OPerationType,
            Token = token,
            UserLanguage = Language,
            DataMessage = object_data
        }
    };

    string result = db.Prompt($"POST {server_db.Prompt("VAR server_tokens_url")} MESSAGE {JsonConvert.SerializeObject(d, Formatting.Indented)}", true);
    AngelDB.AngelResponce responce = JsonConvert.DeserializeObject<AngelDB.AngelResponce>(result);
    return responce.result;

}
