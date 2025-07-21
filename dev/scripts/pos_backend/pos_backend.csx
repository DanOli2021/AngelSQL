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

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

public class AngelApiOperation
{
    public string OperationType { get; set; }
    public string Account { get; set; }
    public string Token { get; set; }
    public string User { get; set; }
    public string UserLanguage { get; set; }
    public dynamic DataMessage { get; set; }
}

private AngelApiOperation api = JsonConvert.DeserializeObject<AngelApiOperation>(message);

//Server parameters
private Dictionary<string, string> parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Environment.GetEnvironmentVariable("ANGELSQL_PARAMETERS"));
private Translations translation = new();
translation.SpanishValues();

// Create required tables
CreateTables(db);

// This is the main function that will be called by the API
return api.OperationType switch
{
    "UpsertSale" => UpsertSale(api, translation),
    "PosLive" => PosLive(api, translation),
    "GetSkus" => GetSkus(api, translation),
    "GetSku" => GetSku(api, translation),
    "UpsertSku" => UpsertSku(api, translation),
    "GetCustomers" => GetCustomers(api, translation),
    "GetCustomer" => GetCustomer(api, translation),
    "GetBusinessLines" => GetBusinessLines(),
    "SaveCustomer" => SaveCustomer(api, translation),
    _ => $"Error: No service found {api.OperationType}",
};


string PosLive(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, translation, "POS_DATA_UPSERT");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    POS_Status pos_status;

    try
    {
        pos_status = db.jSonDeserialize<POS_Status>(api.DataMessage.ToString());
    }
    catch (Exception e)
    {
        return "Error: " + translation.Get("Error parsing data", api.UserLanguage) + " " + e.ToString() + " (1)";
    }

    result = db.UpsertInto("POS_Status", pos_status);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    return "Ok.";
}


string UpsertSale(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, translation, "POS_DATA_UPSERT");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString();

    if (data.StartsWith("{") && data.EndsWith("}"))
    {
        data = "[" + data + "]";
    }

    List<Sale> sales = db.jSonDeserialize<List<Sale>>(data);

    foreach (Sale sale in sales)
    {

        List<Sku> skus = [];

        foreach (Sale_detail item in sale.Sale_detail)
        {

            result = db.Prompt($"SELECT id FROM sku WHERE id = '{item.Sku_id}'", true);

            if (result == "[]")
            {
                Sku sku = new()
                {
                    Id = item.Sku_id,
                    Description = item.Description,
                    Price = item.Price,
                    Cost = item.Cost,
                    Requires_inventory = true,
                    Url_media = "",
                    It_is_for_sale = true,
                    Sale_in_bulk = true,
                    Require_series = false,
                    Require_lots = false,
                    Its_kit = false,
                    Sell_below_cost = false,
                    Locked = false,
                    Weight_request = false,
                    Weight = 1,
                    ClaveProdServ = item.ClaveProdServ,
                    ClaveUnidad = item.ClaveUnidad,
                    From_cfdi = false,
                    Analized = false,
                    Universal_id = "",
                    Deleted = false,
                    DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    User_id = item.User_id,
                    User_name = "",
                    Currency_id = "",
                    Maker_id = "",
                    Location_id = "",
                    Price_code_id = "",
                    //Classifications = new List<SkuClassification>(),
                    Sku_dictionary = new List<Sku_dictionary>()
                };

                skus.Add(sku);

            }

        }

        if (skus.Count > 0)
        {
            result = db.UpsertInto("Sku", skus);

            if (result.StartsWith("Error:"))
            {
                return result + " (2)";
            }
        }

        result = db.Prompt($"SELECT id FROM customer WHERE id = '{sale.Customer_id}'", true);

        if( result == "[]")
        {
            Customer customer = new()
            {
                Id = sale.Customer_id,
                Name = sale.Customer_name,
                RFC = "",
                CP = "",
                Email = "",
                Phone = "",
                Address = "",
                City = "",
                State = "",
                Country = "",
                Credit_limit = 0,
                Credit_balance = 0,
                Credit_days = 0,
                Discount = 0,
                Discount_type = "",
                DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                User_id = sale.User_id,
                User_name = "",
                Currency_id = "",
                Credit_status = ""
            };

            result = db.UpsertInto("Customer", customer);

            if (result.StartsWith("Error:"))
            {
                return result + " (3)";
            }

            result = db.UpsertInto("Customer_search", customer);

            if (result.StartsWith("Error:"))
            {
                return result + " (3.1)";
            }


        }

        List<Dictionary<string, object>> sale_detail_clone = [];

        foreach (Sale_detail item in sale.Sale_detail)
        {
            Dictionary<string, object> sd = ObjectCloner.CreateDictionaryFromObject(item);

            sd["Sale_id"] = sale.Id;
            sale_detail_clone.Add(sd);
        }

        result = db.UpsertInto("Sale_detail", sale_detail_clone, sale.DateTime[..7]);

        if (result.StartsWith("Error:"))
        {
            return result + " (3)";
        }

        Dictionary<string, object> sale_clone = ObjectCloner.CreateDictionaryFromObject(sale);

        result = db.UpsertInto("Sale", sale_clone, sale.DateTime[..7]);

        if (result.StartsWith("Error:"))
        {
            return result + " (4)";
        }

    }

    return "Ok.";

}


string GetSkus(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, translation, "CASHIER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    if (data.Length < 3)
    {
        return "Error: " + translation.Get("Data is too short", api.UserLanguage);
    }

    result = db.Prompt($"SELECT id, description, price, Consumption_taxes FROM sku WHERE id LIKE '%{api.DataMessage.ToString()}%' OR description LIKE '%{api.DataMessage.ToString().Replace(" ", "%")}%' ORDER BY description", true);

    return result;

}


string GetCustomers(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, translation, "CASHIER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    if (data.Length < 3)
    {
        return "Error: " + translation.Get("Data is too short", api.UserLanguage);
    }

    result = db.Prompt($"SELECT * FROM customer_search WHERE customer_search MATCH '{api.DataMessage.ToString()}' ORDER BY name", true);

    if (result == "[]") 
    {
        return "Error: " + translation.Get("Customer not found", api.UserLanguage);
    }

    return result;

}


string GetSku(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, translation, "CASHIER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM sku WHERE id = '" + data + "'", true);

    if( result.StartsWith("Error:") )
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: " + translation.Get("Sku not found", api.UserLanguage);
    }

    result = result.Substring(1, result.Length - 2);

    return result;

}



string SaveCustomer(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, translation, "CASHIER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    dynamic data = api.DataMessage;

    if( data.Currency_id == null || data.Currency_id == "" )
    {
        data.Currency_id = "";
    }

    if( data.BusinessLine_id == null || data.BusinessLine_id == "" )
    {
        data.BusinessLine_id = "1 Otros";
    }

    if( data.BusinessLine_description == null || data.BusinessLine_description == "" )
    {
        return "Error: " + translation.Get("Business line description is required", api.UserLanguage);
    }

    if( data.Customer_name == null || data.Customer_name == "" )
    {
        return "Error: " + translation.Get("Customer name is required", api.UserLanguage);
    }

    if( data.BusinessLine_description == null || data.BusinessLine_description == "" )
    {
        return "Error: " + translation.Get("Business line description is required", api.UserLanguage);
    }

    data.User_id = api.User;

    result = db.Prompt($"SELECT id FROM customer WHERE id = '" + data.Id + "'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get("Customer not found", api.UserLanguage);
    }

    Customer customer = db.jSonDeserialize<Customer>(data);

    Dictionary<string, object> customer_clone = ObjectCloner.CreateDictionaryFromObject(customer);

    result = db.UpsertInto("Customer", customer_clone);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    return "Ok.";

}


string GetCustomer(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, translation, "CASHIER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT * FROM customer WHERE id = '" + data + "'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: " + translation.Get("Customer not found", api.UserLanguage);
    }

    result = result.Substring(1, result.Length - 2);

    Customer customer = db.jSonDeserialize<Customer>(result);

    var d = new
    {
        Customer_id = customer.Id,
        Customer_name = customer.Name,
        RFC = customer.RFC,
        CP = customer.CP,
        Email = customer.Email,
        Phone = customer.Phone,
        Address = customer.Address,
        City = customer.City,
        State = customer.State,
        Country = customer.Country,
        DateTime = customer.DateTime,
        User_id = customer.User_id,
        User_name = customer.User_name,
        Currency_id = customer.Currency_id,
        BusinessLine_id = customer.BusinessLine_id,
        BusinessLine_description = customer.BusinessLine_description,
    };

    return db.GetJson(d);

}


string UpsertSku(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, translation, "POS_DATA_UPSERT");

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
        Dictionary<string, object> sku_clone = ObjectCloner.CreateDictionaryFromObject(sku);
        result = db.UpsertInto("Sku", sku_clone);

        if (result.StartsWith("Error:"))
        {
            return result + " (2)";
        }
    }

    return "Ok.";
}


string GetBusinessLines() 
{

    string result = IsTokenValid(api, translation, "CASHIER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT COUNT(*) AS 'Count' FROM businessLine", true);

    if (result == "[]")
    {
        result = File.ReadAllText( Environment.CurrentDirectory + "/dev/scripts/pos_backend/businessLine.json");
 
        result = db.Prompt($"INSERT INTO businessLine VALUES {result}", true);

        if (result.StartsWith("Error:"))
        {
            return result + " (businessLine.json)";
        }
    }

    if( string.IsNullOrEmpty(data) ) 
    {
        result = db.Prompt($"SELECT * FROM businessLine ORDER BY description", true);
    } else 
    {
        result = db.Prompt($"SELECT * FROM businessLine WHERE description LIKE '%" + data + "%'", true);
    }
    

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: " + translation.Get("Business line not found", api.UserLanguage);
    }

    return result;

}

string IsTokenValid(AngelApiOperation api, Translations translation, string group = "POS_DATA_UPSERT")
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

    string result = SendToAngelPOST("tokens/admintokens", "GetGroupsUsingTocken", d);

    if (result.StartsWith("Error:"))
    {
        return $"Error: {result}";
    }

    return result;

}


private string SendToAngelPOST(string api_name, string OPerationType, dynamic object_data)
{

    var d = new
    {
        api = api_name,
        account = api.Account,
        language = "C#",
        message = new
        {
            OperationType = OPerationType,
            Token = api.Token,
            UserLanguage = api.UserLanguage,
            DataMessage = object_data
        }
    };

    string result = db.Prompt($"POST {server_db.Prompt("VAR server_tokens_url")} MESSAGE {JsonConvert.SerializeObject(d, Formatting.Indented)}", true);

    AngelDB.AngelResponce responce = JsonConvert.DeserializeObject<AngelDB.AngelResponce>(result);
    return responce.result;

}



private string CreateTables(AngelDB.DB db)
{
    Sale sale = new();
    db.CreateTable(sale, "Sale", false, "Sale_detail, Payment_method", true);

    Sale_detail sale_detail = new();
    db.CreateTable(sale_detail, "Sale_detail", false, "", true);

    CashFlow cashFlow = new();
    db.CreateTable(cashFlow, "CashFlow", false, "", true);

    Sku sku = new();
    db.CreateTable(sku, "Sku", false, "Classifications, Sku_dictionary ", true);

    POS_Status pos_status = new();
    db.CreateTable(pos_status, "POS_Status", false, "", true);

    Payments payment_method = new();
    db.CreateTable(payment_method, "Payment_method", false, "Currency", true);

    Consumption_tax consumption_tax = new();
    db.CreateTable(consumption_tax, "Consumption_tax", false, "", true);

    Sale_customer sale_customer = new();
    db.CreateTable(sale_customer, "Sale_customer", false, "", true);

    Currency currency = new();
    db.CreateTable(currency, "Currency", false, "", true);

    Sku_dictionary sku_dictionary = new();
    db.CreateTable(sku_dictionary, "Sku_dictionary", false, "", true);

    Customer customer = new();
    db.CreateTable(customer, "Customer", false, "", true);
    db.CreateTable(customer, "Customer_search", true, "", true);

    BusinessLine businessLine = new();
    db.CreateTable(businessLine, "BusinessLine", false, "", true);

    return "Ok.";

}



public class ObjectCloner
{
    public static Dictionary<string, object> CreateDictionaryFromObject(object originalObject)
    {

        Dictionary<string, object> dictionary = [];

        // Obtenemos las propiedades del objeto original
        PropertyInfo[] properties = originalObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {

            if (property.CanWrite)
            {

                object value;
                try
                {
                    value = property.GetValue(originalObject);
                }
                catch (Exception)
                {
                    continue;
                }

                // Si la propiedad es un tipo complejo (no string, decimal, ni primitivos), la serializamos
                if (value is System.Collections.IList l)
                {
                    string serializedValue = JsonConvert.SerializeObject(value, Formatting.Indented);

                    if (dictionary.ContainsKey(property.Name))
                    {
                        dictionary[property.Name] = serializedValue;
                    }
                    else
                    {
                        dictionary.Add(property.Name, serializedValue);
                    }
                }
                else
                {
                    if (dictionary.ContainsKey(property.Name))
                    {
                        dictionary[property.Name] = value;
                    }
                    else
                    {
                        dictionary.Add(property.Name, value);
                    }
                }
            }
        }

        return dictionary;
    }
}