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
#load "..\AngelComm\AngelComm.csx"
#load "translations.csx"
#load "..\POSApi\BusinessInfo.csx"
#load "..\POSApi\Inventory.csx"
#load "..\POSApi\Sales.csx"
#load "..\POSApi\Series.csx"
#load "..\POSApi\Sku.csx"
#load "..\POSApi\Parameters.csx"

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

private AngelApiOperation api = JsonConvert.DeserializeObject<AngelApiOperation>(message);
api.db = db;
api.server_db = server_db;

// This is the main function that will be called by the API
return api.OperationType switch
{
    "UpdateDatabase" => UpdateDatabase(),
    _ => $"Error: No service found",
};


private string UpdateDatabase()
{
    Sale sale = new();
    db.CreateTable(sale, "Sale", false, "Sale_detail, Payments", true);

    Sale_index sale_index = new();
    db.CreateTable(sale_index, "Sale_index", false, "", true);

    Sale_detail sale_detail = new();
    db.CreateTable(sale_detail, "Sale_detail", false, "", true);

    CashFlow cashFlow = new();
    db.CreateTable(cashFlow, "CashFlow", false, "", true);

    POS_Status pos_status = new();
    db.CreateTable(pos_status, "POS_Status", false, "", true);

    Payments payment_method = new();
    db.CreateTable(payment_method, "Payments", false, "", true);

    Sale_customer sale_customer = new();
    db.CreateTable(sale_customer, "Sale_customer", false, "", true);

    Customer customer = new();
    db.CreateTable(customer, "Customer", false, "", true);
    db.CreateTable(customer, "Customer_search", true, "", true);

    BusinessLine businessLine = new();
    db.CreateTable(businessLine, "BusinessLine", false, "", true);

    Series series = new();
    db.CreateTable(series, "Series", false, "", true);

    Kardex kardex = new();
    db.CreateTable(kardex, "Kardex", false, "", true);

    Storage storage = new();
    db.CreateTable(storage, "Storage", false, "", true);

    string result = db.Prompt($"SELECT * FROM Storage LIMIT 1", true);

    if( result == "[]" )
    {
        Storage mainStorage = new()
        {
            Id = "MainWarehouse",
            Description = "Main Warehouse",
            Type = "Warehouse",
            Location = "Headquarters",
            Capacity = 1000000, // Set a default capacity
            CurrentUsage = 0 // Initialize current usage to 0
        };

        result = db.UpsertInto("Storage", mainStorage);

        if (result.StartsWith("Error:"))
        {
            return result + " (MainWarehouse)";
        }
    }

    Inventory inventory = new();
    db.CreateTable(inventory, "Inventory", false, "", true);

    PhysicalInventory physicalInventory = new();
    db.CreateTable(physicalInventory, "PhysicalInventory", false, "Details", true);

    PhysicalInventoryDetail physicalInventoryDetail = new();
    db.CreateTable(physicalInventoryDetail, "PhysicalInventoryDetail", false, "", true);

    BusinessInfo business = new();
    db.CreateTable(business, "BusinessInfo", false, "", true);

    SkuClassification classification = new();
    db.CreateTable(classification, "SkuClassification", false, "", true);

    KioskoParameters kioskoParameters = new();
    db.CreateTable(kioskoParameters, "KioskoParameters", false, "", true);

    AddParameter("Version", "1.0.0", true);
    AddParameter("storebranch", "Headquarters", true);
    AddParameter("description", "Kiosko for sales and orders", true);
    AddParameter("storage", "MainWarehouse", true);
    AddParameter("sale_series", "TICKET", true);
    AddParameter("Initial_receipt_number", "1", true);
    AddParameter("Currency", "USD", true);
    AddParameter("Payment_method", "Cash", true);
    AddParameter("WorkStation", "WorkStation001", true);
    AddParameter("MasterAccount", "Master", true);
    
    Currency currency = new();
    db.CreateTable(currency, "Currency", false, "", true);

    result = db.Prompt($"SELECT COUNT(*) AS 'Count' FROM Currency", true);

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

    Inventory Inventory = new();
    db.CreateTable(Inventory, "Inventory", false, "", true);

    OrderUser orderuser = new();
    db.CreateTable(orderuser, "OrderUser", false, "", true);
    
    CustomerOrder customerOrder = new();
    db.CreateTable(customerOrder, "CustomerOrder", false, "", true);

    Sku sku = new();
    db.CreateTable(sku, "Sku", false, "SkuClassification, Sku_dictionary ", true);

    Consumption_tax consumption_tax = new();
    db.CreateTable(consumption_tax, "Consumption_tax", false, "", true);

    Sku_dictionary sku_dictionary = new();
    db.CreateTable(sku_dictionary, "Sku_dictionary", false, "", true);

    SkuClassification skuclassification = new();
    db.CreateTable(skuclassification, "SkuClassification", false, "Image", true);

    SkuChange skuChange = new();
    db.CreateTable(skuChange, "SkuChange", false, "", true);

    return "Ok. Database updated successfully.";

}



public string AddParameter(string id, string value, bool AcceptUnique = true)
{

    string result = "";

    if( AcceptUnique )
    {
        result = db.Prompt($"SELECT * FROM KioskoParameters WHERE Id = '{id}'", true);

        if( result != "[]" )
        {
            return $"Error: Parameter with ID '{id}' already exists.";
        }
    }

    KioskoParameters parameter = new()
    {
        Id = id,
        Value = value
    };

    result = db.UpsertInto("KioskoParameters", parameter);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    return "Ok. Parameter added successfully.";
}