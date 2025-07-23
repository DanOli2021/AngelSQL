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
using System.IO;

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
    "SaveImportSkus" => SaveImportSkus(),
    "SaveStock" => SaveStock(),
    "GetSku" => GetSku(),
    "GetSkus" => GetSkus(),
    "GetStock" => GetStock(),
    "GetPublicSkus" => GetPublicSkus(),
    "UpsertSku" => UpsertSku(),
    "DeleteSku" => DeleteSku(),
    "GetSkuDocs" => GetSkuDocs(),
    "GetSkuDoc" => GetSkuDoc(),
    "GetSkuMainDoc" => GetSkuMainDoc(),
    "SaveSkuDoc" => SaveSkuDoc(),
    "GetSkuProperties" => GetSkuProperties(),
    "SaveSkuProperties" => SaveSkuProperties(),
    _ => $"Error: No service found {api.OperationType}",
};


string SaveSkuProperties()
{
    string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    try
    {

        if (api.DataMessage == null || api.DataMessage.ToString().Trim() == "")
        {
            return "Error: No data provided.";
        }

        if (!api.DataMessage.ContainsKey("Id") || string.IsNullOrEmpty(api.DataMessage["Id"].ToString().Trim()))
        {
            return "Error: Sku Id is required.";
        }

        if (!api.DataMessage.ContainsKey("Requires_inventory") || !api.DataMessage.ContainsKey("It_is_for_sale"))
        {
            return "Error: Missing required properties.";
        }

        if (!api.DataMessage.ContainsKey("Sale_in_bulk"))
        {
            api.DataMessage["Sale_in_bulk"] = false;
        }

        if (!api.DataMessage.ContainsKey("Require_series"))
        {
            api.DataMessage["Require_series"] = false;
        }

        if (!api.DataMessage.ContainsKey("Require_lots"))
        {
            api.DataMessage["Require_lots"] = false;
        }

        if (!api.DataMessage.ContainsKey("Its_kit"))
        {
            api.DataMessage["Its_kit"] = false;
        }

        if (!api.DataMessage.ContainsKey("Sell_below_cost"))
        {
            api.DataMessage["Sell_below_cost"] = false;
        }

        if (!api.DataMessage.ContainsKey("Locked"))
        {
            api.DataMessage["Locked"] = false;
        }

        if (!api.DataMessage.ContainsKey("Weight_request"))
        {
            api.DataMessage["Weight_request"] = false;
        }

        dynamic data = api.DataMessage;

        Sku sku = new Sku();
        sku.Id = data.Id.ToString().Trim();
        sku.Requires_inventory = Convert.ToBoolean(data.Requires_inventory);
        sku.It_is_for_sale = Convert.ToBoolean(data.It_is_for_sale);
        sku.Sale_in_bulk = Convert.ToBoolean(data.Sale_in_bulk);
        sku.Require_series = Convert.ToBoolean(data.Require_series);
        sku.Require_lots = Convert.ToBoolean(data.Require_lots);
        sku.Its_kit = Convert.ToBoolean(data.Its_kit);
        sku.Sell_below_cost = Convert.ToBoolean(data.Sell_below_cost);
        sku.Locked = Convert.ToBoolean(data.Locked);
        sku.Weight_request = Convert.ToBoolean(data.Weight_request);

        result = db.UpsertInto("sku", sku);

        return result;

    }
    catch (Exception ex)
    {
        return "Error: " + ex.Message + " " + ex.StackTrace;
    }
}


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

        try
        {
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

        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message + " " + ex.StackTrace;
        }

        try
        {
            List<SkuClassification> classifications = db.jSonDeserialize<List<SkuClassification>>(dt.Rows[0]["SkuClassification"].ToString());

            sku.Classification = "";

            if (classifications != null && classifications.Count > 0)
            {
                sku.Classification = classifications[0].Description;
            }
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message + " " + ex.StackTrace;
        }

        try
        {
            List<Sku_dictionary> sku_dictionaries = db.jSonDeserialize<List<Sku_dictionary>>(dt.Rows[0]["sku_dictionary"].ToString());

            sku.Sku_alternativ = "";

            if (sku_dictionaries != null && sku_dictionaries.Count > 0)
            {
                sku.Sku_alternativ = sku_dictionaries[0].Id;

            }
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message + " " + ex.StackTrace;
        }


        sku.Sku_image = dt.Rows[0]["Image"].ToString();

        return db.GetJson(sku);

    }
    catch (Exception ex)
    {
        return "Error: " + ex.Message + " " + ex.StackTrace;
    }
}



string SaveImportSkus()
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
                Requires_inventory = true,
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

                        if (classString.Trim() == "")
                        {
                            continue;
                        }

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


string SaveStock()
{

    string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage;

    return "Ok.";

}


string GetStock()
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



string GetSkus()
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

    string fields = "id, description, price, Consumption_taxes, ClaveUnidad, Universal_id, Image";

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


string SaveSkuDoc()
{

    string result = IsTokenValid(api, "STAKEHOLDER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    dynamic data = JsonConvert.DeserializeObject(api.DataMessage.ToString());

    if (data == null || data.Sku_id == null || data.Sku_id.ToString().Trim() == "")
    {
        return "Error: Sku_id is required.";
    }

    if (data.Id == null || data.Id.ToString().Trim() == "")
    {
        data.Id = Guid.NewGuid().ToString();
    }

    if (data.Content_id == null || data.Content_id.ToString().Trim() == "")
    {
        return "Error: Content is required.";
    }

    string skuId = data.Sku_id.ToString().Trim().ToUpper();
    string contentId = data.Content_id.ToString().Trim();

    SkuDocs skuDocs = new()
    {
        Id = data.Id,
        Sku_id = skuId,
        Content_id = contentId,
        User_id = api.User,
        Type = data.Type?.ToString() ?? "document",
        Description = data.Description?.ToString() ?? "",
        Url = data.Url?.ToString() ?? "",
        For_main_page = data.For_main_page != null ? Convert.ToBoolean(data.For_main_page) : false,
        In_banner_list = data.In_banner_list != null ? Convert.ToBoolean(data.In_banner_list) : false
    };

    result = db.UpsertInto("SkuDocs", skuDocs);

    return result;

}


string GetPublicSkus()
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



string UpsertSku()
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




string GetSku()
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


string GetSkuProperties()
{

    string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    string data = api.DataMessage.ToString().Trim();

    string fields = "id, description, Requires_inventory, It_is_for_sale, Sale_in_bulk, Require_series, Require_lots, Its_kit, Sell_below_cost, Locked, Weight_request";

    result = db.Prompt($"SELECT {fields} FROM sku WHERE id = '" + data + "'", true);

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


string DeleteSku()
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

    result = db.Prompt($"DELETE FROM SkuDocs PARTITION KEY main WHERE Sku_id = '{data}'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    result = db.Prompt($"DELETE FROM sku PARTITION KEY main WHERE id = '{data}'", true);

    return result;

}


string GetSkuDocs()
{

    string result = IsTokenValid(api, "CASHIER, STAKEHOLDER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM SkuDocs WHERE Sku_id = '" + data + "'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return result;
    }

    return result;

}


string GetSkuDoc()
{

    string result = IsTokenValid(api, "ANY");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM SkuDocs WHERE id = '" + data + "'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return result;
    }

    result = result[1..^1];

    return result;

}


string GetSkuMainDoc()
{

    string data = api.DataMessage.ToString().Trim();

    string result = db.Prompt($"SELECT * FROM SkuDocs WHERE Sku_id = '" + data + "' AND For_main_page = true", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return result;
    }

    result = result[1..^1];

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

