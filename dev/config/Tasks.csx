// GLOBALS
// These lines of code go in each script
#load "Globals.csx"
// END GLOBALS

#r "Newtonsoft.Json.dll"

//using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;

string result = "";

//result = MakeTasks();

if (result.StartsWith("Error:"))
{
    return "Error: " + result.Replace("Error:", "");
}

string MakeTasks()
{
    string scripts_directory = server_db.Prompt("VAR db_scripts_directory");

    result = server_db.Prompt($"SELECT * FROM accounts");

    DataTable accounts = server_db.GetDataTable(result);

    foreach (DataRow row in accounts.Rows)
    {
        AngelDB.DB local_db = new();
        result = local_db.Prompt(row["connection_string"].ToString());

        if (result.StartsWith("Error:"))
        {
            return "Error: " + result.Replace("Error:", "");
        }

        result = local_db.Prompt("USE ACCOUNT " + row["account"]);

        if (result.StartsWith("Error:"))
        {
            return "Error: " + result.Replace("Error:", "");
        }

        result = local_db.Prompt("USE DATABASE " + row["database"]);

        if (result.StartsWith("Error:"))
        {
            return "Error: " + result.Replace("Error:", "");
        }

        result = local_db.Prompt($"SCRIPT FILE {scripts_directory}/pos_backend/InventoryAgent.csx", true, local_db);

        if (result.StartsWith("Error:"))
        {
            return "Error: " + result.Replace("Error:", "");
        }

    }

    return "Ok.";

}

//string scripts_directory = server_db.Prompt("VAR db_scripts_directory");
//string result = server_db.Prompt($"SCRIPT FILE {scripts_directory}/MyBusinessPOS/ImportSkus.csx", true, db);

//if( result.StartsWith("Error:"))
//{
//    return "Error: ImportSkus " + result.Replace("Error:", "");
//}

//result = server_db.Prompt($"SCRIPT FILE {scripts_directory}/MyBusinessPOS/ImportSales.csx", true, db);

//if (result.StartsWith("Error:"))
//{    
//    return "Error: ImportSales " + result.Replace("Error:", "");
//}

return "Ok.";
