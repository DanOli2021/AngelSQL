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
#load "..\POSApi\Chat.csx"

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.IO;

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
    db.CreateTable(sale, "Sale", false, "Sale_detail, Payments");

    Sale_index sale_index = new();
    db.CreateTable(sale_index, "Sale_index", false, "");

    Sale_detail sale_detail = new();
    db.CreateTable(sale_detail, "Sale_detail", false, "");

    CashFlow cashFlow = new();
    db.CreateTable(cashFlow, "CashFlow", false, "");

    POS_Status pos_status = new();
    db.CreateTable(pos_status, "POS_Status", false, "");

    Payments payment_method = new();
    db.CreateTable(payment_method, "Payments", false, "");

    Sale_customer sale_customer = new();
    db.CreateTable(sale_customer, "Sale_customer", false, "");

    Customer customer = new();
    db.CreateTable(customer, "Customer", false, "");
    db.CreateTable(customer, "Customer_search", true, "");

    BusinessLine businessLine = new();
    db.CreateTable(businessLine, "BusinessLine", false, "");
 
    Series series = new();
    db.CreateTable(series, "Series", false, "");

    Kardex kardex = new();
    db.CreateTable(kardex, "Kardex", false, "");

    Storage storage = new();
    db.CreateTable(storage, "Storage", false, "");

    string result = db.Prompt($"SELECT * FROM Storage LIMIT 1");

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
    db.CreateTable(inventory, "Inventory", false, "");

    PhysicalInventory physicalInventory = new();
    db.CreateTable(physicalInventory, "PhysicalInventory", false, "Details");

    PhysicalInventoryDetail physicalInventoryDetail = new();
    db.CreateTable(physicalInventoryDetail, "PhysicalInventoryDetail", false, "");

    BusinessInfo business = new();
    db.CreateTable(business, "BusinessInfo", false, "");

    db.Prompt("ALTER TABLE BusinessInfo ADD COLUMN FreeHtml TEXT");

    SkuClassification classification = new();
    db.CreateTable(classification, "SkuClassification", false, "");

    KioskoParameters kioskoParameters = new();
    db.CreateTable(kioskoParameters, "KioskoParameters", false, "");

    AddParameter("Version", "1.0.0");
    AddParameter("storebranch", "Headquarters");
    AddParameter("description", "Kiosko for sales and orders");
    AddParameter("storage", "MainWarehouse");
    AddParameter("sale_series", "TICKET");
    AddParameter("Initial_receipt_number", "1");
    AddParameter("Currency", "USD");
    AddParameter("Payment_method", "Cash");
    AddParameter("WorkStation", "WorkStation001");
    AddParameter("MasterAccount", "Master");
    
    Currency currency = new();
    db.CreateTable(currency, "Currency", false, "");

    result = db.Prompt($"SELECT COUNT(*) AS 'Count' FROM Currency");

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
    db.CreateTable(Inventory, "Inventory", false, "");

    OrderUser orderuser = new();
    db.CreateTable(orderuser, "OrderUser", false, "");
    
    CustomerOrder customerOrder = new();
    db.CreateTable(customerOrder, "CustomerOrder", false, "");

    Sku sku = new();
    db.CreateTable(sku, "Sku", false, "SkuClassification, Sku_dictionary ");

    Consumption_tax consumption_tax = new();
    db.CreateTable(consumption_tax, "Consumption_tax", false, "");

    Sku_dictionary sku_dictionary = new();
    db.CreateTable(sku_dictionary, "Sku_dictionary", false, "");

    SkuClassification skuclassification = new();
    db.CreateTable(skuclassification, "SkuClassification", false, "Image");

    SkuChange skuChange = new();
    db.CreateTable(skuChange, "SkuChange", false, "");

    Chat chat = new();
    db.CreateTable(chat, "Chat", false, "");  

    ContactChat contactChat = new();
    db.CreateTable(contactChat, "ContactChat", false, "");

    ContactMessage contactMessage = new();
    db.CreateTable(contactMessage, "ContactMessage", false, "");

    ChatGroup chatGroup = new();
    db.CreateTable(chatGroup, "ChatGroup", false, "");

    ChatGroupMessage GroupChatMessage = new();
    db.CreateTable(GroupChatMessage, "GroupChatMessage", false, "");

    SkuDocs skuDocs = new();
    db.CreateTable(skuDocs, "SkuDocs", false, "");

    return "Ok. Database updated successfully.";

}



public string AddParameter(string id, string value, bool AcceptUnique = true)
{

    string result = "";

    if( AcceptUnique )
    {
        result = db.Prompt($"SELECT * FROM KioskoParameters WHERE Id = '{id}'");

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