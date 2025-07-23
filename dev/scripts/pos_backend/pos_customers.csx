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
#load "..\POSApi\Sales.csx"

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
    "Upsertcustomer" => UpsertCustomer(api, translation),
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

    List<Customer> customers = [];

    try
    {

        int n = 0;

        // Recorrer el DataTable
        foreach (DataRow row in dt.Rows)
        {

            if (row["Customer_id"].ToString().Trim() == "")
            {
                return "Error: customer is empty in row " + n.ToString();
            }

            if (row["Name"].ToString().Trim() == "")
            {
                return "Error: Name is empty in row " + n.ToString();
            }

            Customer customer = new()
            {
                Id = row["Customer_id"].ToString().Trim().ToUpper(),
                Name = row["Name"].ToString(),
                RFC = row["RFC"].ToString(),
                CP = row["CP"].ToString(),
                Email = row["Email"].ToString(),
                Phone = row["Phone"].ToString(),
                Address = row["Address"].ToString(),
                City = row["City"].ToString(),
                State = row["State"].ToString(),
                Country = row["Country"].ToString(),
                Credit_limit = decimal.TryParse(row["Credit_limit"].ToString(), out decimal parsedCreditLimit) ? parsedCreditLimit : 0,
                Credit_days = int.TryParse(row["Credit_days"].ToString(), out int parsedCreditDays) ? parsedCreditDays : 0,
                Type = row["Type"].ToString(),
                Discount = decimal.TryParse(row["Discount"].ToString(), out decimal parsedDiscount) ? parsedDiscount : 0,
                Discount_type = row["Discount_type"].ToString(),
                DateTime = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                User_id = api.User,
                User_name = api.User,
                Currency_id = row["Currency_id"].ToString(),
                Seller_id = row["Seller_id"].ToString(),
                Status = row["Status"].ToString(),
                BusinessLine_id = row["Business_line"].ToString(),
                Observations = row["Observations"].ToString(),
                Credit_status = row["Credit_status"].ToString(),
            };

            customers.Add(customer);

            ++n;

        }

        result = db.UpsertInto("customer", customers);

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
        return db.Prompt($"SELECT id, Name, email, phone FROM customer ORDER BY id", true);
    }

    if( data == ":TODAY")
    {
        string today = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd");
        return db.Prompt($"SELECT id, Name, email, phone FROM customer WHERE timestamp >= '" + today +  " 00:00' ORDER BY timestamp DESC", true);
    }

    result = db.Prompt($"SELECT id, Name, email, phone FROM customer WHERE id LIKE '%{api.DataMessage.ToString()}%' OR name LIKE '%{api.DataMessage.ToString().Replace(" ", "%")}%' ORDER BY description LIMIT 25", true);
    return result;

}



string UpsertCustomer(AngelApiOperation api, Translations translation)
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

    List<Customer> customers = db.jSonDeserialize<List<Customer>>(data);

    foreach (Customer customer in customers)
    {

        if (customer.Id == null || customer.Id.Trim() == "")
        {
            return "Error: Sku id is empty.";
        }

        if (customer.Name == null || customer.Name.Trim() == "")
        {
            return "Error: customer description is empty. " + customer.Id;
        }

        Object sku_clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(customer);
        result = db.UpsertInto("Sku", sku_clone);

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

    result = db.Prompt($"SELECT * FROM customer WHERE id = '" + data + "'", true);

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

    result = db.Prompt($"SELECT * FROM customer WHERE id = '{data}'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get("customer not found", api.UserLanguage);
    }

    result = db.Prompt($"DELETE FROM customer PARTITION KEY main WHERE id = '{data}'", true);   

    return result;

}

