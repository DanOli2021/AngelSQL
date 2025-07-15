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

// Create required tables
CreateTables(db);

// This is the main function that will be called by the API
return api.OperationType switch
{
    "SaveImport" => SaveImport(),
    "SaveHeadSite" => SaveHeadSite(),
    "Get" => Get(),
    "GetSiteParameters" => GetSiteParameters(),
    "GetMany" => GetMany(),
    "UpsertCurrency" => UpsertCurrency(),
    "Delete" => Delete(),
    "ImportHtml" => ImportHtml(),
    _ => $"Error: No service found {api.OperationType}",
};



string SaveImport()
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

    List<Pages> pages = new();

    try
    {

        int n = 0;

        // Recorrer el DataTable
        foreach (DataRow row in dt.Rows)
        {

            if (row["page_id"].ToString().Trim() == "")
            {
                return "Error: Page ID is empty in row " + n.ToString() + " " + row["title"].ToString();
            }

            if (row["title"].ToString().Trim() == "")
            {
                return "Error: Title is empty in row " + n.ToString();
            }

            Pages page = new()
            {
                Id = row["page_id"].ToString().Trim().ToUpper(),
                Title = row["title"].ToString(),
                User_id = api.User,
            };

            pages.Add(page);

            ++n;

        }

        Object clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(pages);
        result = db.UpsertInto("Pages", clone);

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


string SaveHeadSite()
{
    string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    dynamic data = api.DataMessage.ToString().Trim();

    if (string.IsNullOrEmpty(data))
    {
        return "Error: No data provided.";
    }

    if (data.Head == null || data.Head.ToString().Trim() == "")
    {
        return "Error: Head is empty.";
    }

    Site site = new()
    {
        Id = "1",
        Head = data.Head.ToString().Trim(),
    };

    result = db.UpsertInto("Site", site);

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    return "Ok.";    
}

string GetMany()
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
        return db.Prompt($"SELECT * FROM pages ORDER BY id", true);
    }

    if( data == ":TODAY")
    {
        string today = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd");
        return db.Prompt($"SELECT * FROM pages WHERE timestamp >= '" + today +  " 00:00' ORDER BY timestamp DESC", true);
    }

    result = db.Prompt($"SELECT * FROM pages WHERE id LIKE '%{api.DataMessage.ToString()}%' OR title LIKE '%{api.DataMessage.ToString().Replace(" ", "%")}%' ORDER BY title LIMIT 25", true);
    return result;

}



string UpsertCurrency()
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

    List<Pages> pages = db.jSonDeserialize<List<Pages>>(data);

    foreach (Pages page in pages)
    {

        if (page.Id == null || page.Id.Trim() == "")
        {
            return "Error: Page id is empty.";
        }

        if (page.Title == null || page.Title.Trim() == "")
        {
            return "Error: Page title is empty. " + page.Id;
        }

        Object page_clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(page);
        result = db.UpsertInto("Pages", page_clone);

        if (result.StartsWith("Error:"))
        {
            return result + " (2)";
        }

    }

    return "Ok.";

}




string Get()
{

    string result = IsTokenValid(api, "CASHIER, STAKEHOLDER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM pages WHERE id = '" + data + "'", true);

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


string GetSiteParameters()
{

    string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    result = db.Prompt($"SELECT * FROM Site WHERE id = '1'", true);

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


string Delete()
{

    string result = IsTokenValid(api, "STAKEHOLDER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM pages WHERE id = '{data}'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get("Page not found", api.UserLanguage);
    }

    result = db.Prompt($"DELETE FROM pages PARTITION KEY main WHERE id = '{data}'", true);

    return result;

}

private string CreateTables(AngelDB.DB db)
{
    Pages pages = new();
    db.CreateTable(pages, "Pages", false, "", true);

    Site site = new();
    db.CreateTable(site, "Site", false, "", true);

    return "Ok.";
}

public class Pages
{
    public string Id { get; set; }
    public string User_id { get; set; }
    public string Title { get; set; }
    public string Html_code { get; set; }
    public string Javascript { get; set; }
}


public class Site
{
    public string Id { get; set; }
    public string User_id { get; set; }
    public string Head { get; set; }
    public string Css { get; set; }
    public string Body { get; set; }
    public string Navigation_bar { get; set; }
}



string ImportHtml()
{
    string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    dynamic html = api.DataMessage.ToString().Trim();

    if (html == null || html.ToString().Trim() == "")
    {
        return "Error: HTML is empty.";
    }

    try
    {
        // Assuming html is a string containing the HTML content to be imported
        string htmlJson = AngelDB.HtmlParser.GetHtmlSectionsAsJson(html.ToString());
        return htmlJson;
    }
    catch (Exception ex)
    {
        return "Error: " + ex.Message;
    }

}