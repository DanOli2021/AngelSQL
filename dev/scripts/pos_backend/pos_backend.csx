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
#load "..\POSApi\Sku.csx"
#load "..\POSApi\BusinessInfo.csx"
#load "..\POSApi\Inventory.csx"
#load "..\POSApi\Series.csx"

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using System.Reflection;
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
    "UpsertSale" => UpsertSale(),
    "GetSale" => GetSale(),
    "GetPayment" => GetPayment(),
    "SavePayment" => SavePayment(),
    "DeletePayment" => DeletePayment(),
    "GetCreditSales" => GetCreditSales(),
    "GetSales" => GetSales(),
    "PosLive" => PosLive(),
    "GetCustomers" => GetCustomers(),
    "GetCustomer" => GetCustomer(),
    "GetBusinessLines" => GetBusinessLines(),
    "SaveCustomer" => SaveCustomer(),
    "GetCurrencies" => GetCurrencies(), 
    _ => $"Error: No service found 1 {api.OperationType}",
}; 


string PosLive()
{

    string result = IsTokenValid(api, "POS_DATA_UPSERT, CASHIER, STEAKHOLDER, ADMINISTRATIVE, SUPERVISORS");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    POS_Status pos_status;

    try
    {
        pos_status = db.jSonDeserialize<POS_Status>(api.DataMessage.ToString());
    }
    catch (System.Exception e)
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


string GetSale()
{

    string result = "";
    string data = api.DataMessage.ToString().Trim();

    if (data.Length < 36)
    {
        return "Error: " + translation.Get("Data is not GUID", api.UserLanguage);
    }

    result = db.Prompt($"SELECT * FROM Sale_index WHERE id = '" + data + "'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get($"Sale not found on index {data}", api.UserLanguage);
    }

    DataTable dt = db.jSonDeserialize<DataTable>(result);
    result = db.Prompt($"SELECT * FROM Sale PARTITION KEY {dt.Rows[0]["TargetPartition"]} WHERE id = '" + data + "'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get($"Sale not found on table sale {data}", api.UserLanguage);
    }

    DataTable dtSale = db.jSonDeserialize<DataTable>(result);

    dtSale.Columns.Add("Logo", typeof(string));
    dtSale.Rows[0]["Logo"] = DBNull.Value;

    result = db.Prompt($"SELECT * FROM BusinessInfo WHERE id = '1' AND logo IS NOT NULL");

    if (!result.StartsWith("Error:"))
    {
        if (result != "[]")
        {
            DataTable dt2 = db.jSonDeserialize<DataTable>(result);
            dtSale.Rows[0]["Logo"] = dt2.Rows[0]["Logo"].ToString();
        }
    }

    result = db.GetJson(dtSale);

    return result[1..^1]; // Remove the first and last brackets

}


string GetPayment()
{
    string result = IsTokenValid(api,  "STEAKHOLDER, ADMINISTRATIVE, SUPERVISORS");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    dynamic data = api.DataMessage;

    if (data.payment_id == null || data.payment_id == "")
    {
        return "Error: " + translation.Get("Data is not GUID", api.UserLanguage);
    }

    if (data.sale_id == null || data.sale_id == "")
    {
        return "Error: " + translation.Get("Sale ID is required", api.UserLanguage);
    }

    result = db.Prompt($"SELECT * FROM Sale_index WHERE id = '" + data.sale_id + "'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get($"Sale not found on index {data}", api.UserLanguage);
    }

    DataTable dt = db.jSonDeserialize<DataTable>(result);

    result = db.Prompt($"SELECT * FROM Payments PARTITION KEY {dt.Rows[0]["TargetPartition"]} WHERE id = '" + data.payment_id + "' AND Sale_id = '" + data.sale_id + "'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get($"Payment not found on table payments {data.payment_id}", api.UserLanguage);
    }

    DataTable dtPayment = db.jSonDeserialize<DataTable>(result);

    List<Payments> payment = db.jSonDeserialize<List<Payments>>(result);

    return db.GetJson(payment[0]);

}


string SavePayment()
{
    // Validar el token del usuario
    string result = IsTokenValid(api, "STEAKHOLDER, ADMINISTRATIVE, SUPERVISORS");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    // Deserializar los datos recibidos
    dynamic data = api.DataMessage;

    if (data.id == null)
    {
        return "Error: " + translation.Get("Payment ID is required", api.UserLanguage);
    }

    // Validar los campos requeridos
    if (data.Sale_id == null || data.Sale_id == "")
    {
        return "Error: " + translation.Get("Sale ID is required", api.UserLanguage);
    }

    if (data.DateTime == null || data.DateTime == "")
    {
        return "Error: " + translation.Get("Payment date is required", api.UserLanguage);
    }

    if (data.Type == null || data.Type == "")
    {
        return "Error: " + translation.Get("Payment type is required", api.UserLanguage);
    }

    if (data.Amount == null || data.Amount <= 0)
    {
        return "Error: " + translation.Get("Payment amount must be greater than 0", api.UserLanguage);
    }

    // Verificar si la venta existe
    result = db.Prompt($"SELECT * FROM Sale_index WHERE id = '{data.Sale_id}'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get($"Sale not found with ID {data.Sale_id}", api.UserLanguage);
    }

    DataTable saleIndex = db.jSonDeserialize<DataTable>(result);
    string partitionKey = saleIndex.Rows[0]["TargetPartition"].ToString();

    // Obtener la partición de la venta
    result = db.Prompt($"SELECT * FROM sale PARTITION KEY {partitionKey} WHERE id = '" + data.Sale_id + "'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get($"Sale not found on table sale {data.Sale_id}", api.UserLanguage);
    }

    result = result[1..^1]; // Remove the first and last brackets

    Sale sale = db.DeserializeDBResult<Sale>(result);

    sale.Payments ??= [];

    Payments payment;

    if (data.id == "new" || data.id.ToString().ToLower() == "new")
    {
        data.id = Guid.NewGuid().ToString();

        // Crear el objeto de pago
        payment = new()
        {
            Id = data.id,
            Sale_id = data.Sale_id,
            DateTime = data.DateTime,
            Description = data.Description ?? "",
            Type = data.Type,
            Amount = Convert.ToDecimal(data.Amount),
            Account_id = api.account,
            User_id = api.User,
            User_name = sale.User_name,
            Currency_id = data.Currency_id,
            Exchange_rate = data.Exchange_rate,
        };

        sale.Payments.Add(payment);

    }
    else
    {
        payment = sale.Payments.FirstOrDefault(p => p.Id == data.id.ToString());

        if (payment == null)
        {
            return "Error: " + translation.Get($"Payment not found with ID {data.id}", api.UserLanguage);
        }

        payment.DateTime = data.DateTime;
        payment.Description = data.Description;
        payment.Type = data.Type;
        payment.Amount = Convert.ToDecimal(data.Amount);
        payment.Account_id = api.account;
        payment.User_id = api.User;
        payment.User_name = sale.User_name;
        payment.Currency_id = data.Currency_id;
        payment.Exchange_rate = data.Exchange_rate;
    }

    decimal total_payments = 0;

    foreach (Payments p in sale.Payments)
    {
        total_payments += p.Amount * p.Exchange_rate;
    }

    if (sale.For_credit == 1 && total_payments > sale.Total)
    {
        return "Error: " + translation.Get("Payment exceeds the total amount of the sale", api.UserLanguage);
    }

    sale.Credit_balance = sale.Total - total_payments;

    if (sale.Receipt_serie == null || sale.Receipt_serie == "")
    {
        sale.Receipt_serie = sale.DateTime[..7];
    }

    if (sale.Receipt_number == null || sale.Receipt_number == "")
    {
        sale.Receipt_number = GetSerie(sale.Receipt_serie).ToString();
    }

    Dictionary<string, object> clone = ObjectCloner.CreateDictionaryFromObject(sale);
    result = db.UpsertInto("Sale", clone, partitionKey);

    db.Prompt($"DELETE FROM Payments PARTITION KEY {partitionKey} WHERE Sale_id = '{sale.Id}'", true);

    result = db.UpsertInto("Payments", sale.Payments, partitionKey);

    if (result.StartsWith("Error:"))
    {
        return result + " (6)";
    }

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    return "Ok.: Payment saved successfully.";

}


string DeletePayment()
{

    string result = IsTokenValid(api,  "STEAKHOLDER, ADMINISTRATIVE, SUPERVISORS");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    dynamic data = api.DataMessage;

    if (data.Payment_id == null || data.Payment_id == "")
    {
        return "Error: " + translation.Get("Payment ID is required", api.UserLanguage);
    }

    if (data.Sale_id == null || data.Sale_id == "")
    {
        return "Error: " + translation.Get("Sale ID is required", api.UserLanguage);
    }

    // Verificar si la venta existe
    result = db.Prompt($"SELECT * FROM Sale_index WHERE id = '{data.Sale_id}'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get($"Sale not found with ID {data.Sale_id}", api.UserLanguage);
    }

    DataTable saleIndex = db.jSonDeserialize<DataTable>(result);
    string partitionKey = saleIndex.Rows[0]["TargetPartition"].ToString();

    // Obtener la partición de la venta
    result = db.Prompt($"SELECT * FROM sale PARTITION KEY {partitionKey} WHERE id = '" + data.Sale_id + "'", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get($"Sale not found on table sale {data.Sale_id}", api.UserLanguage);
    }

    result = result[1..^1]; // Remove the first and last brackets

    Sale sale = db.DeserializeDBResult<Sale>(result);

    sale.Payments ??= [];

    Payments payment = sale.Payments.FirstOrDefault(p => p.Id == data.Payment_id.ToString());

    if (payment == null)
    {
        return "Error: " + translation.Get($"Payment not found with ID {data.Payment_id}", api.UserLanguage);
    }

    // Eliminar el pago de la lista de pagos
    sale.Payments.Remove(payment);
    decimal total_payments = 0;

    foreach (Payments p in sale.Payments)
    {
        total_payments += p.Amount * p.Exchange_rate;
    }

    sale.Credit_balance = sale.Total - total_payments;

    Dictionary<string, object> clone = ObjectCloner.CreateDictionaryFromObject(sale);
    result = db.UpsertInto("Sale", clone, partitionKey);

    db.Prompt($"DELETE FROM Payments PARTITION KEY {partitionKey} WHERE Sale_id = '{sale.Id}'", true);

    result = db.UpsertInto("Payments", sale.Payments, partitionKey);

    if (result.StartsWith("Error:"))
    {
        return result + " (6)";
    }

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    return "Ok.: Payment deleted successfully.";

}



// This function update or insert a sale
// It is used by the POS to send sales to the server
string UpsertSale()
{

    string result = IsTokenValid(api, "POS_DATA_UPSERT, CASHIER, STEAKHOLDER, ADMINISTRATIVE, SUPERVISORS");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
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

            result = db.Prompt($"SELECT id, cost FROM sku WHERE id = '{item.Sku_id}'", true);

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
                    SkuClassification = [],
                    Sku_dictionary = []
                };

                item.Cost = 0;

                skus.Add(sku);

            }
            else
            {
                DataTable dt = db.jSonDeserialize<DataTable>(result);
                item.Cost = decimal.Parse(dt.Rows[0]["Cost"].ToString());
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

        result = db.Prompt($"SELECT * FROM customer WHERE id = '{sale.Customer_id}'", true);

        if (result.StartsWith("Error:"))
        {
            return result + " (2.1)";
        }

        Console.WriteLine($"Customer: {result}");

        Customer sale_customer;

        if (result == "[]")
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

            sale_customer = customer;

        }
        else
        {
            Console.WriteLine($"Customer: {result}");
            sale_customer = db.jSonDeserialize<List<Customer>>(result)[0];
        }

        if (sale.For_credit == 1)
        {
            if (sale.Customer_id == null || sale.Customer_id == "")
            {
                return "Error: " + translation.Get("Customer is required for credit sales", api.UserLanguage);
            }

            if (sale.Customer_id == "SYS")
            {
                return "Error: " + translation.Get("Customer name is required for credit sales", api.UserLanguage);
            }

            Console.WriteLine($"Customer ID: {sale.Customer_id}");
            Console.WriteLine($"Customer Credit: {sale_customer.Credit_limit}");

            if (sale_customer.Credit_limit <= 0)
            {
                return "Error: " + translation.Get("Customer credit limit is not set", api.UserLanguage);
            }

            if (sale_customer.Credit_status != "Active")
            {
                return "Error: " + translation.Get("Customer credit status is not active, status: " + sale_customer.Credit_status, api.UserLanguage);
            }

            if (sale_customer.Credit_balance + sale.Total > sale_customer.Credit_limit)
            {
                return "Error: " + translation.Get("Customer credit limit exceeded", api.UserLanguage);
            }

        }


        List<Dictionary<string, object>> sale_detail_clone = [];
        List<Sale_index> sale_index = [];

        foreach (Sale_detail item in sale.Sale_detail)
        {
            Dictionary<string, object> sd = ObjectCloner.CreateDictionaryFromObject(item);
            sd["Sale_id"] = sale.Id;
            sale_detail_clone.Add(sd);

            Sale_index si = new()
            {
                Id = sale.Id,
                TargetPartition = sale.DateTime[..7],
            };

            sale_index.Add(si);

        }

        result = db.UpsertInto("Sku", skus);

        if (result.StartsWith("Error:"))
        {
            return result + " (2)";
        }


        if (result.StartsWith("Error:"))
        {
            return result + " (2.1)";
        }

        result = db.UpsertInto("Sale_detail", sale_detail_clone, sale.DateTime[..7]);

        if (result.StartsWith("Error:"))
        {
            return result + " (3)";
        }

        result = db.Prompt($"SELECT * FROM BusinessInfo WHERE id = '1'");

        if (!result.StartsWith("Error:"))
        {
            if (result != "[]")
            {
                DataTable dt = db.jSonDeserialize<DataTable>(result);

                sale.Business_data = "";
                sale.Business_data += "\n<h1>" + dt.Rows[0]["Name"].ToString() + "</h1>";
                sale.Business_data += "\n" + dt.Rows[0]["Address"].ToString();
                sale.Business_data += "\n" + dt.Rows[0]["Phone"].ToString();
                sale.Business_data += "\n" + dt.Rows[0]["Email"].ToString();
                sale.Business_data += "\n" + dt.Rows[0]["Website"].ToString();
            }
        }

        decimal total_payments = 0;

        foreach (Payments p in sale.Payments)
        {
            total_payments += p.Amount * p.Exchange_rate;
        }

        if (sale.Total > total_payments)
        {
            sale.For_credit = 1;
            sale.Credit_balance = sale.Total - total_payments;
            sale.Credit_due_date = DateTime.ParseExact(sale.DateTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).AddDays(sale_customer.Credit_days).ToString("yyyy-MM-dd");
        }
        else
        {
            sale.Credit_balance = 0;
        }

        result = db.Prompt($"DELETE FROM payments PARTITION KEY {sale.DateTime[..7]} WHERE Sale_id = '{sale.Id}'", true);

        if (result.StartsWith("Error:"))
        {
            return result + " (7)";
        }

        foreach (Payments p in sale.Payments)
        {
            Dictionary<string, object> payment_clone = ObjectCloner.CreateDictionaryFromObject(p);
            payment_clone["Sale_id"] = sale.Id;
            payment_clone["Account_id"] = sale.Account_id;
            result = db.UpsertInto("payments", payment_clone, sale.DateTime[..7]);

            if (result.StartsWith("Error:"))
            {
                return result + " (6)";
            }
        }

        string sale_serie = GetParameter("sale_series", api);

        if (sale_serie.StartsWith("Error:"))
        {
            return sale_serie + " (2)";
        }

        if (sale.Receipt_serie == null || sale.Receipt_serie == "")
        {
            sale.Receipt_serie = sale_serie;
        }

        if (sale.Receipt_number == null || sale.Receipt_number == "")
        {
            sale.Receipt_number = GetSerie(sale_serie, "Initial_receipt_number").ToString();
        }

        string warehouse = GetParameter("storage", api);

        if (warehouse.StartsWith("Error:"))
        {
            return warehouse + " (3)";
        }

        sale.Storage_id = warehouse;

        Dictionary<string, object> sale_clone = ObjectCloner.CreateDictionaryFromObject(sale);
        sale_clone["Account_id"] = api.account;

        result = db.UpsertInto("Sale", sale_clone, sale.DateTime[..7]);

        if (result.StartsWith("Error:"))
        {
            return result + " (4)";
        }

        result = db.UpsertInto("Sale_index", sale_index, sale.DateTime[..4]);

        if (result.StartsWith("Error:"))
        {
            return result + " (5)";
        }

    }

    return "Ok.";

}


int GetSerie(string serie, string parameter = "")
{

    int start_number = 1;

    string result;

    string partitionKey = serie.ToLower();
    db.Prompt($"LOCK TABLE Series PARTITION KEY {partitionKey}", true);
    result = db.Prompt($"SELECT id, ConsecValue FROM Series PARTITION KEY {partitionKey} WHERE id = '{serie}'", true);

    if (result == "[]")
    {

        if (!string.IsNullOrEmpty(parameter))
        {
            if (parameter == "Initial_receipt_number")
            {
                result = GetParameter(parameter, api);
                start_number = int.TryParse(result, out int parsedValue) ? parsedValue : 1;
            }
        }

        Series newSerie = new()
        {
            Id = serie,
            ConsecValue = start_number
        };

        db.Prompt($"INSERT INTO Series PARTITION KEY {partitionKey} VALUES {db.GetJson(newSerie)}", true);
        db.Prompt($"UNLOCK TABLE Series PARTITION KEY {partitionKey}", true);
        return start_number;
    }

    DataTable dt = db.jSonDeserialize<DataTable>(result);
    int value = Convert.ToInt32(dt.Rows[0]["ConsecValue"]);
    value++;

    db.Prompt($"UPDATE Series PARTITION KEY {partitionKey} SET ConsecValue = {value} WHERE id = '{serie}'", true);
    db.Prompt($"UNLOCK TABLE Series PARTITION KEY {partitionKey}", true);

    return value;

}


string GetCustomers()
{

    string result = IsTokenValid(api,"CASHIER, STEAKHOLDER, ADMINISTRATIVE, SUPERVISORS");

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



string SaveCustomer()
{

    string result = IsTokenValid(api, "CASHIER, STEAKHOLDER, ADMINISTRATIVE, SUPERVISORS");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    dynamic data = api.DataMessage;

    if (data.Customer_id == null || data.Customer_id == "")
    {
        data.Customer_id = "";
    }

    if (data.BusinessLine_id == null || data.BusinessLine_id == "")
    {
        return "Error: " + translation.Get("Business line is required", api.UserLanguage);
    }

    if (data.BusinessLine_description == null || data.BusinessLine_description == "")
    {
        return "Error: " + translation.Get("Business line description is required", api.UserLanguage);
    }

    if (data.Customer_name == null || data.Customer_name == "")
    {
        return "Error: " + translation.Get("Customer name is required", api.UserLanguage);
    }

    if (data.BusinessLine_description == null || data.BusinessLine_description == "")
    {
        return "Error: " + translation.Get("Business line description is required", api.UserLanguage);
    }

    if (data.Email == null || data.Email == "")
    {
        return "Error: " + translation.Get("Email is required", api.UserLanguage);
    }

    data.Email = data.Email.ToString().ToLower().Trim();

    if (data.Customer_id == "New")
    {
        result = db.Prompt("SELECT id FROM customer WHERE email = '" + data.Email + "'", true);

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        if (result != "[]")
        {
            return "Error: " + translation.Get("Email already exists", api.UserLanguage);
        }

        result = db.Prompt("SELECT id FROM customer WHERE phone = '" + data.Phone + "'", true);

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        if (result != "[]")
        {
            return "Error: " + translation.Get("Phone already exists", api.UserLanguage);
        }

        data.Customer_id = data.Phone;

    }


    data.User_id = api.User;

    Customer customer = new()
    {
        Id = data.Customer_id,
        DateTime = data.DateTime,
        Phone = data.Phone,
        Email = data.Email.ToString().ToLower().Trim(),
        Address = data.Address,
        City = data.City,
        State = data.State,
        Country = data.Country,
        CP = data.CP,
        RFC = data.RFC,
        BusinessLine_id = data.BusinessLine_id,
        BusinessLine_description = data.BusinessLine_description,
        Name = data.Customer_name
    };

    Dictionary<string, object> customer_clone = ObjectCloner.CreateDictionaryFromObject(customer);

    result = db.UpsertInto("Customer", customer_clone);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    result = db.UpsertInto("Customer_search", customer_clone);

    return data.Customer_id;

}


string GetCustomer()
{

    string result = IsTokenValid(api, "CASHIER, STAKEHOLDER, ADMINISTRATIVE, SUPERVISORS");

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

    result = result[1..^1];

    Customer customer = db.jSonDeserialize<Customer>(result);

    var d = new
    {
        Customer_id = customer.Id,
        Customer_name = customer.Name,
        customer.RFC,
        customer.CP,
        customer.Email,
        customer.Phone,
        customer.Address,
        customer.City,
        customer.State,
        customer.Country,
        customer.DateTime,
        customer.User_id,
        customer.User_name,
        customer.Currency_id,
        customer.BusinessLine_id,
        customer.BusinessLine_description,
    };

    return db.GetJson(d);

}


string GetSales()
{

    string result = IsTokenValid(api, "SUPERVISORS, ADMINISTRATIVE, STAKEHOLDER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    dynamic data = api.DataMessage;
    string start_date = data.start_date.ToString().Trim() + " 00:00:00";
    string end_date = data.end_date.ToString().Trim() + " 23:59:59";

    result = db.Prompt($"SELECT id, Account_id, DateTime, User_id, Sale_type, Total  FROM Sale WHERE DateTime >= '{start_date}' AND DateTime <= '{end_date}' ORDER BY DateTime DESC", true);

    if (result == "[]")
    {
        return "Error: " + translation.Get("No sales found", api.UserLanguage);
    }

    return result;

}


string GetCreditSales()
{

    string result = IsTokenValid(api, "SUPERVISORS, ADMINISTRATIVE, STAKEHOLDER");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    dynamic data = api.DataMessage;
    string start_date = data.start_date.ToString().Trim() + " 00:00:00";
    string end_date = data.end_date.ToString().Trim() + " 23:59:59";
    bool only_with_balance = data.OnlyWithBalance;

    string fields = "id, Account_id, DateTime, Total, Credit_balance, Customer_id, Customer_name, Credit_due_date";

    if (only_with_balance)
    {
        result = db.Prompt($"SELECT {fields} FROM Sale WHERE for_credit = 1 AND DateTime >= '{start_date}' AND DateTime <= '{end_date}' AND Credit_balance > 0 ORDER BY DateTime DESC", true);
    }
    else
    {
        result = db.Prompt($"SELECT {fields} FROM Sale WHERE for_credit = 1 AND DateTime >= '{start_date}' AND DateTime <= '{end_date}' ORDER BY DateTime DESC", true);
    }

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: " + translation.Get("No sales found", api.UserLanguage);
    }

    return result;

}


string GetBusinessLines()
{

    string result = IsTokenValid(api,  "ANY");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    result = db.Prompt($"SELECT COUNT(*) AS 'Count' FROM businessLine", true);

    if (result == "[]")
    {
        result = File.ReadAllText(Environment.CurrentDirectory + "/dev/scripts/pos_backend/businessLine.json");

        result = db.Prompt($"INSERT INTO businessLine VALUES {result}", true);

        if (result.StartsWith("Error:"))
        {
            return result + " (businessLine.json)";
        }
    }

    if (string.IsNullOrEmpty(data))
    {
        result = db.Prompt($"SELECT * FROM businessLine ORDER BY id", true);
    }
    else
    {
        result = db.Prompt($"SELECT * FROM businessLine WHERE description LIKE '%" + data + "%' ORDER BY id", true);
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



string GetCurrencies()
{

    string result = IsTokenValid(api, "ANY");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string data = api.DataMessage.ToString().Trim();

    if (string.IsNullOrEmpty(data))
    {
        result = db.Prompt($"SELECT * FROM Currency ORDER BY id", true);
    }
    else
    {
        result = db.Prompt($"SELECT * FROM Currency WHERE description LIKE '%" + data + "%' ORDER BY id", true);
    }

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: " + translation.Get("Currency not found", api.UserLanguage);
    }

    return result;

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
                if (value is System.Collections.IList)
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