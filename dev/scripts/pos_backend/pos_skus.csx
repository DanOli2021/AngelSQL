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
    "GetSimpleSku" => GetSimpleSku(),
    "SaveImportSkus" => SaveImportSkus(api, translation),
    "SaveStock" => SaveStock(api, translation),
    "GetSku" => GetSku(api, translation),
    "GetSkus" => GetSkus(api, translation),
    "GetStock" => GetStock(api, translation),
    "GetPublicSkus" => GetPublicSkus(api, translation),
    "UpsertSku" => UpsertSku(api, translation),
    "DeleteSku" => DeleteSku(api, translation),
    _ => $"Error: No service found {api.OperationType}",
};


string GetSimpleSku()
{
    string result = IsTokenValid(api, "STAKEHOLDER, CASHIER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    if (string.IsNullOrEmpty(data))
    {
        return "Error: No data provided.";
    }

    try
    {

        result = db.Prompt($"SELECT * FROM sku WHERE id = '{data}'", true);

        if (result == "[]")
        {

            result = db.Prompt($"SELECT * FROM sku_dictionary WHERE id = '{data}'", true);

            if (result == "[]")
            {
                return "Error: No data found.";
            }

            DataTable dt1 = db.GetDataTable(result);
            string skuId = dt1.Rows[0]["sku_id"].ToString();

            result = db.Prompt($"SELECT * FROM sku WHERE id = '{skuId}'", true);

            if (result == "[]")
            {
                return "Error: No data found.";
            }

        }

        // Convert the result to a DataTable
        DataTable dt = db.GetDataTable(result);

        SimpleSku sku = new()
        {
            Id = dt.Rows[0]["id"].ToString(),
            Description = dt.Rows[0]["description"].ToString(),
            Price = Convert.ToDecimal(dt.Rows[0]["price"]),
            Cost = Convert.ToDecimal(dt.Rows[0]["cost"])
        };

        List<Consumption_tax> taxes = db.jSonDeserialize<List<Consumption_tax>>(dt.Rows[0]["consumption_taxes"].ToString());

        if (taxes != null && taxes.Count > 0)
        {
            sku.Tax = taxes[0].Rate;
            sku.Tax_name = taxes[0].Description;

            if (taxes.Count > 1)
            {
                sku.Tax2 = taxes[1].Rate;
                sku.Tax_name2 = taxes[1].Description;
            }
        }

        List<SkuClassification> classifications = db.jSonDeserialize<List<SkuClassification>>(dt.Rows[0]["SkuClassification"].ToString());

        sku.Classification = "";

        if (classifications != null && classifications.Count > 0)
        {
            sku.Classification = classifications[0].Description;
        }

        List<Sku_dictionary> sku_dictionaries = db.jSonDeserialize<List<Sku_dictionary>>(dt.Rows[0]["sku_dictionary"].ToString());

        sku.Sku_alternativ = "";

        if (sku_dictionaries != null && sku_dictionaries.Count > 0)
        {
            sku.Sku_alternativ = sku_dictionaries[0].Id;

        }

        sku.Sku_image = dt.Rows[0]["Image"].ToString();

        return db.GetJson(sku);

    }
    catch (Exception ex)
    {
        return "Error: " + ex.Message + " " + ex.StackTrace;
    }
}



string SaveImportSkus(AngelApiOperation api, Translations translation)
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

    List<Sku> skus = [];
    List<SkuChange> skuchages = [];

    try
    {

        int n = 0;

        // Recorrer el DataTable
        foreach (DataRow row in dt.Rows)
        {

            if (row["sku"].ToString().Trim() == "")
            {
                return "Error: Sku is empty in row " + n.ToString() + " " + row["description"].ToString();
            }

            if (row["description"].ToString().Trim() == "")
            {
                return "Error: Description is empty in row " + n.ToString() + " " + row["sku"].ToString();
            }

            if (row["price"].ToString().Trim() == "")
            {
                return "Error: Price is empty in row " + n.ToString() + " " + row["sku"].ToString();
            }

            decimal price = decimal.TryParse(row["price"].ToString(), out decimal parsedPrice) ? parsedPrice : 0;

            if (price < 0)
            {
                return "Error: Price is lower than zero in row " + n.ToString() + " " + row["sku"].ToString();
            }

            Sku sku = new()
            {
                Id = row["sku"].ToString().Trim().ToUpper(),
                Description = row["description"].ToString(),
                Price = price,
                Cost = 0,
                Requires_inventory = false,
                It_is_for_sale = true,
                Sale_in_bulk = true,
                Require_series = false,
                Require_lots = false,
                Its_kit = false,
                Sell_below_cost = false,
                Locked = false,
                Weight_request = false,
                Weight = 0,
                ClaveProdServ = "",
                ClaveUnidad = "",
                From_cfdi = false,
                Analized = false,
                Universal_id = "",
                Deleted = false,
                Currency_id = "",
                Sku_dictionary = []
            };

            Console.WriteLine(db.GetJson(sku));

            if (row.Table.Columns.Contains("Cost"))
            {
                decimal cost = decimal.TryParse(row["cost"].ToString(), out decimal parsedCost) ? parsedCost : 0;
                sku.Cost = cost;
            }

            if (row.Table.Columns.Contains("Classification"))
            {
                if (row["Classification"].ToString().Trim() != "")
                {

                    string[] classification_string = row["Classification"].ToString().Split(',');

                    foreach (string classString in classification_string)
                    {

                        result = db.Prompt($"SELECT * FROM SkuClassification WHERE id = '{classString.Trim().ToUpper()}'", true);

                        if (result.StartsWith("Error:"))
                        {
                            return result;
                        }

                        if (result != "[]")
                        {
                            DataTable dt2 = db.GetDataTable(result);

                            SkuClassification classification = new()
                            {
                                Id = classString.Trim().ToUpper(),
                                Description = dt2.Rows[0]["description"].ToString(),
                                Type = dt2.Rows[0]["type"].ToString(),
                                Image = dt2.Rows[0]["image"].ToString()
                            };

                            sku.SkuClassification.Add(classification);
                        }
                        else
                        {
                            SkuClassification classification = new()
                            {
                                Id = classString.Trim().ToUpper(),
                                Description = classString.Trim().ToUpper(),
                                Type = ""
                            };

                            sku.SkuClassification.Add(classification);

                        }

                    }
                }

            }

            if (row.Table.Columns.Contains("Sku_alternativ"))
            {
                if (row["Sku_alternativ"].ToString().Trim() != "")
                {

                    result = db.Prompt($"SELECT * FROM sku_dictionary WHERE id = '{row["Sku_alternativ"].ToString().Trim().ToUpper()}' AND sku_id <> '" + sku.Id.Trim().ToUpper() + "'", true);

                    if (result.StartsWith("Error:"))
                    {
                        return result;
                    }

                    if (result != "[]")
                    {
                        DataTable dt2 = db.GetDataTable(result);
                        return "Error: Sku dictionary entry already exists: " + dt2.Rows[0]["description"].ToString() + " " + dt2.Rows[0]["sku_id"].ToString();
                    }

                    Sku_dictionary sku_dictionary = new()
                    {
                        Id = row["Sku_alternativ"].ToString().ToUpper(),
                        Sku_id = sku.Id,
                        Description = row["Sku_alternativ"].ToString().ToUpper(),
                        Equivalence = 1
                    };

                    sku.Sku_dictionary.Add(sku_dictionary);

                }
            }

            if (row.Table.Columns.Contains("Tax"))
            {

                if (row["Tax"].ToString().Trim() != "")
                {
                    Consumption_tax tax = new()
                    {
                        Id = row["TaxName"].ToString(),
                        Rate = decimal.TryParse(row["Tax"].ToString(), out decimal parsedTax) ? parsedTax : 0,
                        Description = row["TaxName"].ToString(),
                        Type = "" // Asignar un valor por defecto o según tu lógica
                    };

                    sku.Consumption_taxes.Add(tax);
                }

            }

            if (row.Table.Columns.Contains("Tax2"))
            {

                if (row["Tax2"].ToString().Trim() != "")
                {
                    Consumption_tax tax = new()
                    {
                        Id = row["TaxName2"].ToString(),
                        Rate = decimal.TryParse(row["Tax2"].ToString(), out decimal parsedTax2) ? parsedTax2 : 0,
                        Description = row["TaxName2"].ToString(),
                        Type = "" // Asignar un valor por defecto o según tu lógica
                    };

                    sku.Consumption_taxes.Add(tax);
                }
            }

            sku.Image = "";

            if (row.Table.Columns.Contains("Image"))
            {
                if (row["Image"].ToString() != "")
                {
                    if (row["Image"].ToString().Contains("base64"))
                    {
                        string directory = server_db.Prompt($"VAR db_wwwroot", true) + $"/images/skus/{api.account}";

                        if (Directory.Exists(directory) == false)
                        {
                            Directory.CreateDirectory(directory);
                        }

                        string path = AngelDB.Base64Helper.SaveBase64ToAutoNamedFile(row["Image"].ToString(), directory, row["sku"].ToString().Trim().ToUpper());

                        if (path == "Error:")
                        {
                            return "Error: Unable to save image: " + row["sku"].ToString().Trim().ToUpper();
                        }

                        sku.ImageBase64 = row["Image"].ToString();
                        sku.Image = $"../images/skus/{api.account}/" + row["sku"].ToString().Trim().ToUpper() + Path.GetExtension(path);
                    }
                    else
                    {
                        if (row["Image"].ToString().Contains("?t"))
                        {
                            row["Image"] = row["Image"].ToString().Split("?t=")[0];
                        }

                        sku.Image = row["Image"].ToString();
                    }
                }
            }

            sku.Maker_id = "";
            sku.Location_id = "";
            sku.Price_code_id = "";
            sku.DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            sku.User_id = "";
            skus.Add(sku);

            SkuChange skuChange = new()
            {
                Id = sku.Id,
                Description = sku.Description,
                Price = price,
                Cost = sku.Cost,
                User_id = sku.User_id,
                User_name = sku.User_name
            };

            skuchages.Add(skuChange);

            ++n;

        }

        Object clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(skus);
        result = db.UpsertInto("sku", clone);

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        result = db.UpsertInto("sku_dictionary", skus.SelectMany(sku => sku.Sku_dictionary).ToList());

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        result = db.UpsertInto("consumption_tax", skus.SelectMany(sku => sku.Consumption_taxes).ToList());

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        result = db.UpsertInto("SkuClassification", skus.SelectMany(sku => sku.SkuClassification).ToList());

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        result = db.UpsertInto("SkuChange", skuchages);

        return result;

    }
    catch (Exception ex)
    {
        return "Error: " + ex.Message + " " + ex.StackTrace;
    }

}


string SaveStock(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage;

    return "Ok.";

}


string GetStock(AngelApiOperation api, Translations translation)
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

    string fields = "id, description, price, Consumption_taxes, Stock";

    if (data.ToUpper().Trim() == ":ALL")
    {
        return db.Prompt($"SELECT {fields} FROM sku ORDER BY description", true);
    }

    if (data.ToUpper().Trim() == ":TODAY")
    {
        string today = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd");
        return db.Prompt($"SELECT {fields} FROM sku WHERE timestamp >= '" + today + " 00:00' ORDER BY timestamp DESC", true);
    }

    result = db.Prompt($"SELECT {fields} FROM sku WHERE id LIKE '%{api.DataMessage.ToString()}%' OR description LIKE '%{api.DataMessage.ToString().Replace(" ", "%")}%' ORDER BY description LIMIT 25", true);
    return result;

}



string GetSkus(AngelApiOperation api, Translations translation)
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

    if (data.ToUpper().Trim() == ":ALL")
    {
        return db.Prompt($"SELECT id, description, price, Consumption_taxes FROM sku ORDER BY description", true);
    }

    if (data.ToUpper().Trim() == ":TODAY")
    {
        string today = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd");
        return db.Prompt($"SELECT id, description, price, Consumption_taxes FROM sku WHERE timestamp >= '" + today + " 00:00' ORDER BY timestamp DESC", true);
    }

    result = db.Prompt($"SELECT id, description, price, Consumption_taxes FROM sku WHERE id LIKE '%{api.DataMessage.ToString()}%' OR description LIKE '%{api.DataMessage.ToString().Replace(" ", "%")}%' ORDER BY description LIMIT 25", true);
    return result;

}


string GetPublicSkus(AngelApiOperation api, Translations translation)
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

    string fields = "id, description, price, Consumption_taxes, Image, SkuClassification";

    if (data.ToUpper().Trim() == ":ALL")
    {
        return db.Prompt($"SELECT {fields} FROM sku ORDER BY description", true);
    }

    if (data.ToUpper().Trim() == ":TODAY")
    {
        string today = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd");
        return db.Prompt($"SELECT {fields} FROM sku WHERE timestamp >= '" + today + " 00:00' ORDER BY timestamp DESC", true);
    }

    string result = db.Prompt($"SELECT {fields} FROM sku WHERE SkuClassification LIKE '%{api.DataMessage.ToString().Trim().Replace(" ", "%")}%' OR description LIKE '%{api.DataMessage.ToString().Trim().Replace(" ", "%")}%' ORDER BY description LIMIT 25", true);
    return result;

}



string UpsertSku(AngelApiOperation api, Translations translation)
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

    List<Sku> skus = db.jSonDeserialize<List<Sku>>(data);

    foreach (Sku sku in skus)
    {

        if (sku.Id == null || sku.Id.Trim() == "")
        {
            return "Error: Sku id is empty.";
        }

        if (sku.Description == null || sku.Description.Trim() == "")
        {
            return "Error: Sku description is empty. " + sku.Id;
        }
        if (sku.Price < 0)
        {
            return "Error: Sku price is lower than zero. " + sku.Id;
        }

        if (sku.Cost < 0)
        {
            return "Error: Sku cost is lower than zero. " + sku.Id;
        }

        sku.Sku_dictionary ??= [];
        sku.SkuClassification ??= [];        
        sku.Consumption_taxes ??= [];

        Object sku_clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(sku);
        result = db.UpsertInto("Sku", sku_clone);

        if (result.StartsWith("Error:"))
        {
            return result + " (2)";
        }

        result = db.UpsertInto("sku_dictionary", skus.SelectMany(sku => sku.Sku_dictionary).ToList());

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        result = db.UpsertInto("consumption_tax", skus.SelectMany(sku => sku.Consumption_taxes).ToList());

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        result = db.UpsertInto("skuclassification", skus.SelectMany(sku => sku.SkuClassification).ToList());

        if (result.StartsWith("Error:"))
        {
            return result;
        }



    }

    return "Ok.";
}




string GetSku(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "CASHIER, STAKEHOLDER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM sku WHERE id = '" + data + "'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: " + translation.Get("Sku not found", api.UserLanguage);
    }

    result = result[1..^1];

    return result;

}


string DeleteSku(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "STAKEHOLDER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT id, description, price FROM sku WHERE id = '{data}'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get("Sku not found", api.UserLanguage);
    }

    result = db.Prompt($"DELETE FROM sku PARTITION KEY main WHERE id = '{data}'", true);

    return result;

}


class SimpleSku
{
    public string Id { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public decimal Tax { get; set; }
    public string Tax_name { get; set; }
    public decimal Tax2 { get; set; }
    public string Tax_name2 { get; set; }
    public string Classification { get; set; }
    public string Sku_alternativ { get; set; }
    public string Sku_image { get; set; }
}

