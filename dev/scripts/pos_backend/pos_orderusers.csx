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
#load "..\POSAPi\Sales.csx"
#load "..\POSAPi\Sku.csx"
#load "..\POSAPi\BusinessInfo.csx"


using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.CodeAnalysis;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net;
using System.Xml;
using System.Xml.Linq;


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
    "SaveOrderUser" => SaveOrderUser(api, translation),
    "SaveOrder" => SaveOrder(),
    "GetOrderUser" => GetOrderUser(api, translation),
    "SaveOrderFromKiosk" => SaveOrderFromKiosk(api, translation),
    "Login" => Login(api, translation),
    "GetMany" => GetMany(api, translation),
    "GetOrder" => GetOrder(api, translation),
    _ => $"Error: No service found {api.OperationType}",
};


string SaveOrder()
{
    string result = IsTokenValid(api, "CASHIER, STAKEHOLDER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");
    string SaleId = Guid.NewGuid().ToString();

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    result = db.Prompt($"SELECT * FROM businessinfo WHERE Id = '1'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: The business information does not exist.";
    }

    DataTable dtBusinessInfo = db.GetDataTable(result);

    result = db.Prompt($"SELECT * FROM customerorder WHERE Id = '{api.DataMessage.Id}'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result != "[]")
    {
        DataTable dtOrder = db.GetDataTable(result);

        if (dtOrder.Rows[0]["Status"].ToString() == "Confirmed")
        {
            return "Error: The order is already confirmed.";
        }

    }

    CustomerOrder order = new()
    {
        Id = api.DataMessage.Id,
        Sale_id = SaleId,
        Account_id = api.account,
        OrderUserId = api.DataMessage.OrderUserId,// Default to anonymous user if not provided
        OrderName = api.DataMessage.OrderName,
        OrderPhone = api.DataMessage.OrderPhone,
        PaymentMethod = api.DataMessage.PaymentMethod, // Default to Cash if not provided
        Change = api.DataMessage.Change,
        ShippingAddress = api.DataMessage.ShippingAddress,
        OrderDate = api.DataMessage.OrderDate,
        TotalAmount = api.DataMessage.TotalAmount,
        Status = api.DataMessage.Status, // Default to Pending if not provided
        ShippingMethod = api.DataMessage.ShippingMethod, // Default to Pickup if not provided
        Anonymous = api.DataMessage.Anonymous, // Default to true if not provided
        User_id = api.DataMessage.User_id, // This is the user id of the person who placed the order
        User_name = api.DataMessage.User_name // This is the name of the person who placed the order
    };

    bool IsConfirmed = false;

    if (api.DataMessage.Status == "Confirmed")
    {
        IsConfirmed = true;
    }

    List<OrderItem> orderItems = [];

    if (api.DataMessage.OrderItems != null)
    {
        foreach (var item in api.DataMessage.OrderItems)
        {
            OrderItem orderItem = new()
            {
                Id = item.Id ?? Guid.NewGuid().ToString(),
                OrderId = order.Id,
                Sku_Id = item.Sku_Id ?? "",
                Sku_Description = item.Sku_Description ?? "",
                Quantity = item.Quantity ?? 1,
                Price = item.Price ?? 0,
                Notes = item.Notes ?? "" // Optional notes for the order item
            };
            orderItems.Add(orderItem);
        }
    }

    order.OrderItems = orderItems;

    List<Payments> payments = [];

    foreach (var payment in api.DataMessage.Payments)
    {
        Payments newPayment = new()
        {
            Id = payment.Id,
            Account_id = api.account,
            Sale_id = SaleId,
            Description = payment.Description,
            Type = payment.Type,
            ReferenceID = payment.ReferenceID,
            ReferenceType = payment.ReferenceType,
            DateTime = payment.DateTime,
            User_id = payment.User_id,
            User_name = payment.User_name,
            Currency_id = payment.Currency_id,
            Exchange_rate = payment.Exchange_rate ?? 1,
            Amount = payment.Amount,
        };
        payments.Add(newPayment);
    }

    order.Payments = payments;

    decimal totalAmount = 0;

    foreach (OrderItem item in order.OrderItems)
    {
        if (item.Quantity <= 0 || item.Price <= 0)
        {
            return "Error: Invalid quantity or price for order item.";
        }

        totalAmount += item.Price * item.Quantity;

    }

    order.TotalAmount = totalAmount;
    order.OrderDateChange = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    order.User_id = api.User;

    Object clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(order);
    result = db.UpsertInto("CustomerOrder", clone, order.OrderDate[..7]);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (!IsConfirmed)
    {
        return result;
    }

    List<Sale_detail> saleDetails = [];

    decimal subtotal = 0;
    decimal cost = 0;
    decimal consumptionTaxTotal = 0;
    string BusinessData = dtBusinessInfo.Rows[0]["Name"].ToString() +
        " - " + dtBusinessInfo.Rows[0]["Address"].ToString() +
        " - " + dtBusinessInfo.Rows[0]["Phone"].ToString() +
        " - " + dtBusinessInfo.Rows[0]["Email"].ToString() +
        " - " + dtBusinessInfo.Rows[0]["WebSite"].ToString();

    foreach (OrderItem item in order.OrderItems)
    {

        result = db.Prompt($"SELECT * FROM sku WHERE Id = '{item.Sku_Id}'", true);

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        if (result == "[]")
        {
            return $"Error: The SKU with ID {item.Sku_Id} does not exist.";
        }

        DataTable dtSku = db.GetDataTable(result);

        List<Consumption_tax> consumptionTaxes = db.jSonDeserialize<List<Consumption_tax>>(dtSku.Rows[0]["Consumption_taxes"].ToString());

        decimal price = item.Price;
        decimal consumptionTax_percent = 0;
        decimal consumptionTax = 0;

        foreach (Consumption_tax tax in consumptionTaxes)
        {
            if (tax.Rate < 0)
            {
                return $"Error: Invalid tax rate for SKU {item.Sku_Id}.";
            }

            price = price / (1 + (tax.Rate / 100));
            consumptionTax_percent += tax.Rate;
            consumptionTax += price * (tax.Rate / 100);
        }

        Sale_detail saleDetail = new()
        {
            Id = item.Id,
            Sale_id = SaleId,
            Sku_id = item.Sku_Id,
            Description = item.Sku_Description,
            Qty = item.Quantity,
            Price_with_taxes = item.Price,
            Price = price,
            Original_price = decimal.Parse(dtSku.Rows[0]["Price"].ToString()),
            Discount = 0, // Assuming no discount for now
            Import = item.Price * item.Quantity,
            Cost = decimal.Parse(dtSku.Rows[0]["Cost"].ToString()),
            Consumption_tax_percentages = consumptionTax_percent,
            Consumption_tax = consumptionTax,
            PromotionCode = "", // Assuming no promotion code for now
            DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            User_id = order.User_id,
            Sku_dictionary_id = "", // Assuming no SKU dictionary ID for now
            Qty_equivalence = 1, // Assuming no equivalence for now
            Description_equivalence = "",
            ClaveProdServ = dtSku.Rows[0]["ClaveProdServ"].ToString(),
            ClaveUnidad = dtSku.Rows[0]["ClaveUnidad"].ToString(),
            Observations = item.Notes,
            Preferential_Classification = "",

            Consumption_taxes = consumptionTaxes.Select(t => new Consumption_tax
            {
                Id = t.Id,
                Description = t.Description,
                Rate = t.Rate,
                Type = t.Type
            }).ToList()
        };

        subtotal += saleDetail.Import;
        cost += saleDetail.Cost;
        consumptionTaxTotal += saleDetail.Consumption_tax;

        saleDetails.Add(saleDetail);

    }

    result = db.Prompt($"SELECT * FROM currency WHERE Id = '{dtBusinessInfo.Rows[0]["Currency_id"]}'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: The currency information does not exist.";
    }

    DataTable dtCurrency = db.GetDataTable(result);

    Sale sale = new()
    {
        Id = SaleId,
        DateTime = order.OrderDate,
        Receipt_number = "",
        Sale_type = "From Order",
        Currency_id = dtCurrency.Rows[0]["Id"].ToString(),
        Currency_name = dtCurrency.Rows[0]["Description"].ToString(),
        Exchange_rate = 1,
        Total = order.TotalAmount,
        Change = 0,
        Subtotal = subtotal,
        Cost = cost,
        Consumption_tax = consumptionTaxTotal,
        For_credit = 0,
        Credit_balance = 0,
        Credit_due_date = "",
        ReferenceID = order.Id,
        ReferenceType = "CustomerOrder",
        User_id = order.User_id,
        User_name = order.User_name,
        Seller_id = order.User_id, // Assuming the seller is the same as the user
        Seller_name = order.User_name, // Assuming the seller is the same as the user
        POS_ID = "",
        Account_id = api.account,
        Location = "",
        Business_data = BusinessData,
        Number_of_items = saleDetails.Count,
        ExtraData = "",
        Customer_id = order.OrderPhone,
        Customer_name = order.OrderName,
        Sale_detail = saleDetails,
        Total_payments = order.Payments.Sum(p => p.Amount),
        Payments = payments,
    };

    //string message = db.GetJson(sale);

    result = SendToAngelPOST("pos_backend/pos_backend", "UpsertSale", sale, server_db.Prompt("VAR server_sales_url"), api);

    return result;

}



string GetOrder(AngelApiOperation api, Translations translation)
{

    dynamic data = api.DataMessage;

    if (data.Length < 3)
    {
        return "Error: " + translation.Get("Data is too short", api.UserLanguage);
    }

    string result = db.Prompt($"SELECT * FROM customerorder PARTITION KEY {data.PartitionKey} WHERE Id = '{data.Id}'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    DataTable dt = db.GetDataTable(result);

    CustomerOrder customerOrder = new()
    {
        Id = dt.Rows[0]["id"].ToString(),
        OrderName = dt.Rows[0]["OrderName"].ToString(),
        OrderPhone = dt.Rows[0]["OrderPhone"].ToString(),
        PaymentMethod = dt.Rows[0]["PaymentMethod"].ToString(),
        Change = decimal.Parse(dt.Rows[0]["Change"].ToString()),
        OrderDate = dt.Rows[0]["OrderDate"].ToString(),
        TotalAmount = decimal.Parse(dt.Rows[0]["TotalAmount"].ToString()),
        Status = dt.Rows[0]["Status"].ToString(),
        ShippingMethod = dt.Rows[0]["ShippingMethod"].ToString(),
        Anonymous = dt.Rows[0]["Anonymous"].ToString(),
        Payments = db.jSonDeserialize<List<Payments>>(dt.Rows[0]["Payments"].ToString()),
        OrderItems = db.jSonDeserialize<List<OrderItem>>(dt.Rows[0]["OrderItems"].ToString()),
        User_id = dt.Rows[0]["User_id"].ToString(),
        User_name = dt.Rows[0]["User_name"].ToString(),
        OrderDateChange = dt.Rows[0]["OrderDateChange"].ToString(),
        ShippingAddress = dt.Rows[0]["ShippingAddress"].ToString()
    };

    return db.GetJson(customerOrder);

}


string GetMany(AngelApiOperation api, Translations translation)
{

    string result = IsTokenValid(api, "CASHIER, STAKEHOLDER, POS_DATA_UPSERT, POS_DATA_GET, SUPERVISOR, ADMINISTRATOR");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    dynamic data = api.DataMessage;

    if (data.searchValue.ToString().Length < 3)
    {
        return "Error: " + translation.Get("Data is too short", api.UserLanguage);
    }

    data.startDate = data.startDate + " 00:00:00";
    data.endDate = data.endDate + " 23:59:59";

    if (data.searchValue == ":PENDING")
    {
        return db.Prompt($"SELECT * FROM customerorder WHERE Status NOT IN ('Cancelled', 'Confirmed') AND OrderDate >= '{data.startDate}' AND OrderDate <= '{data.endDate}' ORDER BY timestamp", true);
    }

    if (data.searchValue == ":ALL")
    {
        return db.Prompt($"SELECT * FROM customerorder WHERE OrderDate >= '{data.startDate}' AND OrderDate <= '{data.endDate}' ORDER BY timestamp", true);
    }

    if (data.searchValue == ":TODAY")
    {
        string today = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd");
        return db.Prompt($"SELECT * FROM customerorder WHERE timestamp >= '" + today + " 00:00' ORDER BY timestamp", true);
    }

    result = db.Prompt($"SELECT * FROM customerorder WHERE OrderName LIKE '%{data.searchValue}%' OR OrderItems LIKE '%{data.searchValue.ToString().Replace(" ", "%")}%' ORDER BY timestamp", true);
    return result;

}



string SaveOrderUser(AngelApiOperation api, Translations translation)
{

    string data = api.DataMessage.ToString().Trim();

    if (string.IsNullOrEmpty(data))
    {
        return "Error: No data provided.";
    }

    dynamic jsonData = JsonConvert.DeserializeObject(data);

    string result = db.Prompt($"SELECT * FROM OrderUser WHERE Id = '{jsonData.Email}'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result != "[]")
    {
        return "Error: The user already exists.";
    }

    if (jsonData.Password != jsonData.Retype)
    {
        return "Error: Passwords do not match.";
    }

    if (jsonData.Password == null || jsonData.Password == "")
    {
        return "Error: Password is required.";
    }

    if (jsonData.Email == null || jsonData.Email == "")
    {
        return "Error: Email is required.";
    }

    if (jsonData.Phone == null || jsonData.Phone == "")
    {
        return "Error: Phone is required.";
    }

    if (jsonData.Name == null || jsonData.Name == "")
    {
        return "Error: Name is required.";
    }

    OrderUser ou = new()
    {
        Id = jsonData.Email,
        Password = jsonData.Password,
        Name = jsonData.Name,
        Phone = jsonData.Phone
    };

    if (ou.Name == null || ou.Name == "")
    {
        return "Error: Name is required.";
    }

    return db.UpsertInto("OrderUser", ou);

}



string GetOrderUser(AngelApiOperation api, Translations translation)
{

    string data = api.DataMessage.ToString().Trim();

    if (string.IsNullOrEmpty(data))
    {
        return "Error: No data provided.";
    }

    dynamic jsonData = JsonConvert.DeserializeObject(data);

    string result = db.Prompt($"SELECT * FROM OrderUser WHERE Id = '{jsonData.Email}'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: The user does not exist.";
    }

    DataTable dt = db.GetDataTable(result);

    if (string.IsNullOrEmpty(dt.Rows[0]["password"].ToString()))
    {
        return "Error: The user does not exist.";
    }

    if (dt.Rows[0]["password"].ToString().Trim() != jsonData.Password.Trim())
    {
        return "Error: The password is incorrect.";
    }

    List<OrderUser> orderUserList = db.jSonDeserialize<List<OrderUser>>(result);
    OrderUser orderUser = orderUserList[0];

    return db.GetJson(orderUser);

}


string Login(AngelApiOperation api, Translations translation)
{

    string data = api.DataMessage.ToString().Trim();

    if (string.IsNullOrEmpty(data))
    {
        return "Error: No data provided.";
    }

    dynamic jsonData = JsonConvert.DeserializeObject(data);

    string result = db.Prompt($"SELECT * FROM OrderUser WHERE Id = '{jsonData.Email}'", true);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: The user does not exist.";
    }

    DataTable dt = db.GetDataTable(result);

    if (string.IsNullOrEmpty(dt.Rows[0]["password"].ToString()))
    {
        return "Error: The user does not exist.";
    }

    if (dt.Rows[0]["password"].ToString().Trim() != jsonData.Password.ToString().Trim())
    {
        return "Error: The password is incorrect.";
    }

    return "Ok.";

}


string SaveOrderFromKiosk(AngelApiOperation api, Translations translation)
{

    string data = api.DataMessage.ToString().Trim();

    if (string.IsNullOrEmpty(data))
    {
        return "Error: No data provided.";
    }

    dynamic jsonData = JsonConvert.DeserializeObject(data);

    if (jsonData.Email == null || jsonData.Email == "")
    {
        return "Error: Email is required.";
    }

    string result = "";

    CustomerOrder customerOrder;

    if (jsonData.Anonymous == false)
    {
        result = db.Prompt($"SELECT * FROM OrderUser WHERE Id = '{jsonData.Email}'", true);

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        if (result == "[]")
        {
            return "Error: The user does not exist.";
        }

        DataTable dt = db.GetDataTable(result);

        if (string.IsNullOrEmpty(dt.Rows[0]["password"].ToString()))
        {
            return "Error: The user does not exist.";
        }

        if (dt.Rows[0]["password"].ToString().Trim() != jsonData.Password.ToString().Trim())
        {
            return "Error: The password is incorrect.";
        }

        // Save the order
        customerOrder = new()
        {
            Id = Guid.NewGuid().ToString(),
            OrderUserId = jsonData.Email,
            OrderName = dt.Rows[0]["name"].ToString(),
            OrderPhone = dt.Rows[0]["phone"].ToString(),
            PaymentMethod = jsonData.PaymentMethod ?? "",
            Change = 0,
            ShippingAddress = jsonData.ShippingAddress ?? "",
            OrderDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TotalAmount = 0,
            Status = "Pending",
            ShippingMethod = jsonData.DeliveryType ?? "",
            Anonymous = "false",
        };

    }
    else
    {
        customerOrder = new()
        {
            Id = Guid.NewGuid().ToString(),
            OrderUserId = jsonData.Email,
            OrderName = jsonData.Name,
            OrderPhone = jsonData.Phone,
            PaymentMethod = jsonData.PaymentMethod ?? "",
            Change = 0,
            ShippingAddress = jsonData.ShippingAddress ?? "",
            OrderDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TotalAmount = 0,
            Status = "Pending",
            ShippingMethod = jsonData.DeliveryType ?? "",
            Anonymous = "true"

        };
    }

    decimal totalAmount = 0;

    if (jsonData.OrderItems != null)
    {
        List<OrderItem> orderItems = [];
        foreach (var item in jsonData.OrderItems)
        {
            OrderItem orderItem = new()
            {
                Id = Guid.NewGuid().ToString(),
                OrderId = customerOrder.Id,
                Sku_Id = item.id,
                Sku_Description = item.Description,
                Quantity = item.qty,
                Price = item.Price,
                Notes = item.Notas ?? "" // Optional notes for the order item
            };

            totalAmount += orderItem.Price * orderItem.Quantity;
            orderItems.Add(orderItem);
        }
        customerOrder.OrderItems = orderItems;
    }

    customerOrder.TotalAmount = totalAmount;

    Object clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(customerOrder);
    result = db.UpsertInto("CustomerOrder", clone, customerOrder.OrderDate[..7]);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    result = SendToAngelPOST("pos_backend/pos_businessinfo", "GetBusinessInfo", null, server_db.Prompt("VAR server_configuration_url"), api);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    BusinessInfo businessInfo = db.jSonDeserialize<BusinessInfo>(result);

    string url = server_db.Prompt("VAR db_public_url") + "kiosko/order.html?PartitionId=" + customerOrder.OrderDate[..7] + "&OrderId=" + customerOrder.Id;

    result = db.Prompt($"SELECT * FROM kioskoparameters WHERE id = 'Currency'", true);
    string Currency_id = "USD"; // Default to USD if not found

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result != "[]")
    {
        DataTable dt = db.GetDataTable(result);
        Currency_id = dt.Rows[0]["Value"].ToString();
    }

    string message = $@"
<table style='font-family: Arial, sans-serif; color: #333; width: 100%; max-width: 600px; margin: auto; border-collapse: collapse;'>
    <tr>
        <td style='padding: 20px; background-color: #f8f9fa; border-bottom: 2px solid #007bff; text-align: center;'>
            <h2 style='margin: 0; color: #007bff;'>{businessInfo.Name}</h2>
            <p style='margin: 0; font-size: 14px;'>{businessInfo.Address}</p>
        </td>
    </tr>
    <tr>
        <td style='padding: 20px; background-color: #ffffff;'>
            <p style='font-size: 16px;'>Hello <strong>{customerOrder.OrderName}</strong>,</p>
            <p style='font-size: 16px;'>We are pleased to inform you that your order has been received successfully.</p>
            <table style='width: 100%; margin-top: 15px; font-size: 15px;'>
                <tr>
                    <td style='padding: 8px 0;'><strong>Order ID:</strong></td>
                    <td>{customerOrder.Id}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0;'><strong>Total Amount:</strong></td>
                    <td>{customerOrder.TotalAmount.ToString("C", CultureInfo.CreateSpecificCulture(Currency_id))}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0;'><strong>Payment Method:</strong></td>
                    <td>{customerOrder.PaymentMethod}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0;'><strong>Shipping Method:</strong></td>
                    <td>{customerOrder.ShippingMethod}</td>
                </tr>
            </table>
            <p style='margin-top: 20px; font-size: 16px;'>You can view your order details at the following link:</p>
            <p><a href='{url}' target='_blank' style='color: #007bff; text-decoration: none;'>View Order</a></p>
            <p style='margin-top: 30px; font-size: 16px;'>Thank you for your purchase!<br><br><strong>{businessInfo.Name}</strong></p>
        </td>
    </tr>
    <tr>
        <td style='padding: 10px; text-align: center; font-size: 12px; color: #888;'>
            This is an automated message. Please do not reply.
        </td>
    </tr>
</table>";


    var mail_message = new
    {
        Account = server_db.Prompt("VAR db_proxy_account"),
        jsonData.Email,
        Password = server_db.Prompt("VAR db_proxy_password"),
        Html = message,
        Subject = "Order Confirmation from:" + server_db.Prompt("VAR db_proxy_account"),
    };

    result = SendToAngelPOST("angelsql/SendEmail", "SendEmail", mail_message, "https://angelsql.net/POST", api);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    return url;

}



static async Task<string> SendMailFromSoap(string html, string email, string fromAddress, string password, string default_subject = "AngelSQL Registration Confirmation")
{
    string fromAddressName = fromAddress;
    string fromAddressPass = password;
    string toAddress = email;
    string toAddressName = email;
    string subject = default_subject;
    string alternate = html;
    string trick = "#9LpuR9v3v3Kzs$";

    string soapEnvelope = $@"
            <soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
                <soap12:Body>
                    <EnviaCorreoH xmlns=""http://wsCorreo.mybusinesspos.com/"">
                        <fromAddress>{System.Security.SecurityElement.Escape(fromAddressName)}</fromAddress>
                        <fromAddressPass>{System.Security.SecurityElement.Escape(fromAddressPass)}</fromAddressPass>
                        <fromAddressName>{System.Security.SecurityElement.Escape(fromAddressName)}</fromAddressName>                        
                        <toAddress>{System.Security.SecurityElement.Escape(toAddress)}</toAddress>
                        <toAddressName>{System.Security.SecurityElement.Escape(toAddressName)}</toAddressName>
                        <Subjet>{System.Security.SecurityElement.Escape(subject)}</Subjet>
                        <alternate>{System.Security.SecurityElement.Escape(alternate)}</alternate>                                                
                        <Trick>{trick}</Trick>
                    </EnviaCorreoH>
                </soap12:Body>
            </soap12:Envelope>";

    var url = "https://wscorreoa.mybusinesspos.net/WSCorreo.asmx";
    return await PostSOAPRequestAsync(url, soapEnvelope);

}


private static async Task<string> PostSOAPRequestAsync(string url, string text)
{
    var httpClient = new HttpClient();

    using HttpContent content = new StringContent(text, Encoding.UTF8, "text/xml");
    using HttpRequestMessage request = new(HttpMethod.Post, url);
    request.Headers.Add("SOAPAction", "http://wsCorreo.mybusinesspos.com/EnviaCorreoH");
    request.Content = content;
    using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode(); // throws an Exception if 404, 500, etc.
    return await response.Content.ReadAsStringAsync();
}