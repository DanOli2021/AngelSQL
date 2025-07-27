/// <summary>
/// Class library that contains the classes, that are used define 
/// the structure of the data that is sent or received from the POS
/// And other systems that interact with the POS
/// </summary>

#load "Sku.csx"

using System;
using System.Collections.Generic;

/// <summary>
/// This class represents the sale, it is the main object that is sent or received from the POS
/// </summary>
public class Sale
{
    // Primary sale identifier
    public string Id { get; set; }
    // Date and time of the sale, the format is "yyyy-MM-dd HH:mm:ss"
    public string DateTime { get; set; }
    // Sale serie
    public string Receipt_serie { get; set; }
    // Sale number
    public string Receipt_number { get; set; }
    // Sale type
    public string Sale_type { get; set; }
    // Currency identifier
    public string Currency_id { get; set; }
    // Currency name
    public string Currency_name { get; set; }
    // Exchange rate
    public decimal Exchange_rate { get; set; }
    // Total amount
    public decimal Total { get; set; }
    // Change to return to the customer
    public decimal Change { get; set; } 
    // Subtotal amount
    public decimal Subtotal { get; set; }
    // Cost
    public decimal Cost { get; set; }
    // Consumption tax    
    public decimal Consumption_tax { get; set; }
    // For credit
    public int For_credit { get; set; }
    // Credit
    public decimal Credit_balance { get; set; }
    // Credit due date
    public string Credit_due_date { get; set; }  
    // Reference identifier
    public string ReferenceID { get; set; }
    // Reference type
    public string ReferenceType { get; set; }
    // User identifier
    public string User_id { get; set; }
    // User name
    public string User_name { get; set; }
    // Customer identifier
    public string Customer_id { get; set; }
    // Customer name
    public string Customer_name { get; set; }
    // Customer RFC
    public string Seller_id { get; set; }
    // Seller name
    public string Seller_name { get; set; }
    // POS identifier
    public string POS_ID { get; set; }
    // POS description
    public string Account_id { get; set; }
    // POS location
    public string Location { get; set; }
    // POS business data
    public string Business_data { get; set; }
    // The number of items in the sale 
    public int Number_of_items { get; set; }
    // Free column to any data
    public string ExtraData { get; set; }
    // Total of payments
    public decimal Total_payments { get; set; }
    // Storage identifier, if the sale is in a storage 
    public string Storage_id { get; set; }
    // The name of Store Branch, if the sale is in a store branch
     // Store Branch identifier
    public string StoreBranch { get; set; }
     // Workstation identifier, if the sale is in a workstation
    public string WorkStation { get; set; }    
    // If inventory is affected
    public bool IsInventoryAffected { get; set; } // If the sale is already in the inventory
    public string Status { get; set; } = "PENDING"; // Sale status, default is PENDING
    // Sale observations
    public string Observations { get; set; } = "";
    // Payment status
    public string Payment_status { get; set; }
    // Pendig reason
    public string Pending_reason { get; set; }
    public string Reason_code { get; set; }
    public List<Sale_detail> Sale_detail { get; set; } = [];
    // Payment methods
    public List<Payments> Payments { get; set; } = [];

}


/// <summary>
/// This class is used to store the unaltered string that originates the sale.
/// </summary>
public class Sale_index
{
    // Sale identifier
    public string Id { get; set; }
    // Sale blob
    public string TargetPartition { get; set; }
}


/// <summary>
/// Detail of the sale, it is the list of products that are sold in the sale
/// </summary>
public class Sale_detail
{
    // Sale detail identifier
    public string Id { get; set; }
    // Sale identifier
    public string Sale_id { get; set; }
    // SKU identifier
    public string Sku_id { get; set; }
    // SKU description
    public string Description { get; set; }
    // Quantity
    public decimal Qty { get; set; }
    // Price
    public decimal Price { get; set; }
    // The original price before discounts are applied
    public decimal Original_price { get; set; }
    // Discount in percentage
    public decimal Discount { get; set; }
    // Amount equal to quantity price minus discount
    public decimal Import { get; set; }
    // Cost
    public decimal Cost { get; set; }
    // Consumption tax percentage
    public decimal Consumption_tax_percentages { get; set; }
    // Consumption tax
    public decimal Consumption_tax { get; set; }
    // Total price with taxes
    public decimal Price_with_taxes { get; set; }
    // Promotion code
    public string PromotionCode { get; set; }
    // Date and time of the Sale_detail
    public string DateTime { get; set; }
    // User identifier
    public string User_id { get; set; }
    // Sku dictionary identifier
    public string Sku_dictionary_id { get; set; }
    // Quantity equivalence
    public decimal Qty_equivalence { get; set; }
    // Description equivalence
    public string Description_equivalence { get; set; }
    // It is a key provided by the Mexican tax administration system.
    public string ClaveProdServ { get; set; }
    // It is a unit of measurement provided by the Mexican tax administration system.
    public string ClaveUnidad { get; set; }
    // Observations
    public string Observations { get; set; }
    // If the sale detail is already in the inventory
    public bool IsInventoryAffected { get; set; } = false; 
    // The most common classification of the sku
    public string Preferential_Classification { get; set; }
    // List of consumption taxes
    public List<Consumption_tax> Consumption_taxes { get; set; } = [];
}


/// <summary>
/// Skus classifications used in each sales detail and in the Skus catalog
/// </summary>
public class Classification
{
    // Classification identifier
    public string Id { get; set; }
    // Classification Type
    public string Type { get; set; }
    // Classification description
    public string Description { get; set; }
}


/// <summary>
/// Payment methods used in each sale
/// </summary>
public class Payments
{
    public string Id { get; set; }
    public string Account_id { get; set; }
    public string Sale_id { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public string ReferenceType { get; set; }
    public string ReferenceID { get; set; }
    public string DateTime { get; set; }
    public string User_id { get; set; }
    public string User_name { get; set; }
    public decimal Amount { get; set; }
    public string Currency_id { get; set; }
    public decimal Exchange_rate { get; set; }  
}


/// <summary>
/// Point of sale and overall cash flow
/// </summary>
public class CashFlow
{
    // Cash flow identifier
    public string Id { get; set; } = "";
    // Date and time of the cash flow
    public string DateTime { get; set; } = "";
    // Type of cash flow
    public string Type { get; set; } = "";
    // Reference identifier
    public string ReferenceID { get; set; } = "";
    // Description of the movement that originated the money flow movement
    public string Description { get; set; } = "";
    // Amount of the movement
    public decimal Amount { get; set; } = 0;
    // Currency identifier
    public string Currency_id { get; set; } = "";
    // Currency exchange rate
    public decimal Exchange_rate { get; set; } = 0;
    // If the movement is already summarized
    public string Summarized { get; set; } = "";
    // If the movement is already in z-report
    public string ZReport { get; set; } = "";
    // User identifier
    public string User_id { get; set; } = "";
    // User name
    public string User_Name { get; set; } = "";
    // User who authorized the movement
    public string Authorizer { get; set; } = "";
}


/// <summary>
/// Currency class, used to store the currencies that are used in the POS
/// </summary>
public class Currency
{
    // Currency identifier
    public string Id { get; set; }
    // Currency name or description
    public string Description { get; set; }
    // Currency symbol
    public string Symbol { get; set; }
    // Currency exchange rate
    public decimal Exchange_rate { get; set; }
    public string User_id { get; set; } // User who created the currency
    public string User_name { get; set; } // User name who created the currency
}


/// <summary>
/// Sale customer class, used to store the customers that are used in the POS
/// This class is limited to the basic information of the customer, 
/// for more information use the Customer class
/// </summary>
public class Sale_customer
{
    // Customer identifier
    public string Id { get; set; }
    // Customer name
    public string Name { get; set; }
    // Customer RFC
    public string RFC { get; set; }
    // Customer address
    public string CP { get; set; }    
}


/// <summary>
/// Class used to store the customer information
/// </summary>
public class Customer
{
    // Customer identifier
    public string Id { get; set; }
    // Customer name
    public string Name { get; set; }
    // Customer RFC
    public string RFC { get; set; }
    // Customer postal code
    public string CP { get; set; }
    // Customer email
    public string Email { get; set; }
    // Customer phone
    public string Phone { get; set; }
    // Customer address
    public string Address { get; set; }
    // Customer city
    public string City { get; set; }
    // Customer state
    public string State { get; set; }
    // Customer country
    public string Country { get; set; }
    // Customer credit limit
    public decimal Credit_limit { get; set; }
    // Customer credit days
    public int Credit_days { get; set; }
    // Customer type
    public string Type { get; set; }
    // Customer credit status
    public string Credit_status { get; set; }
    // Customer credit balance
    public decimal Credit_balance { get; set; }
    // Customer discount
    public decimal Discount { get; set; }
    // Customer discount type
    public string Discount_type { get; set; }
    // Date and time of the customer
    public string DateTime { get; set; }
    // User identifier
    public string User_id { get; set; }
    // User name
    public string User_name { get; set; }
    // Currency identifier
    public string Currency_id { get; set; }
    // Seller identifier
    public string Seller_id { get; set; }
    // Business line
    public string BusinessLine_id { get; set; }
    // Business line description
    public string BusinessLine_description { get; set; }
    // Customer observations
    public string Observations { get; set; }
    // Customer status
    public string Status { get; set; }

}


/// <summary>   
/// Class used to store the seller information
/// </summary>
public class Seller
{
    // Seller identifier
    public string Id { get; set; }
    // Seller name
    public string Name { get; set; }
    // Seller email
    public string Email { get; set; }
    // Seller phone
    public string Phone { get; set; }
    // Seller commission
    public decimal Commission { get; set; }
}


/// <summary>
/// POS class, used to store the point of sale information
/// </summary>
public class POSID
{
    // POS identifier
    public string Id { get; set; }
    // POS account associated
    public string Account { get; set; }
    // POS description
    public string Description { get; set; }
    // POS location
    public string Location { get; set; }
    // POS business data
    public string Business_data { get; set; }
}

/// <summary>
/// Class used to store the user information
/// </summary>
public class User
{
    // User identifier
    public string Id { get; set; }
    // User name
    public string Name { get; set; }
}

/// <summary>
/// Class used to store the seller information, this class is limited to the basic information of the seller
/// for more information use the Seller class
/// </summary>
public class Sale_Seller
{
    // Seller identifier
    public string Id { get; set; }
    // Seller name
    public string Name { get; set; }
}


/// <summary>
/// Class used to store the status of the POS
/// </summary>
public class POS_Status
{
    // POS identifier
    public string Id { get; set; }
    // POS description
    public string Description { get; set; }
    // POS status
    public string Status { get; set; }
    // Last communication
    public string LastCommunication { get; set; }
}


public class BusinessLine
{
    // Business line identifier
    public string Id { get; set; }
    // Business line description
    public string Description { get; set; }
    // Business line status
}


public class OrderUser
{
    // El id del usuario es su correo
    public string Id { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Confirmed { get; set; }
}


public class CustomerOrder
{
    public string Id { get; set; }
    public string OrderUserId { get; set; }
    public string Sale_id { get; set; } // This is the sale id that will be used to create the sale
    public string Account_id { get; set; } // This is the account id that will be used to create the sale
    public string OrderName { get; set; }
    public string OrderPhone { get; set; }
    public string PaymentMethod { get; set; }
    public decimal Change { get; set; }
    public string ShippingAddress { get; set; }
    public string OrderDate { get; set; }
    public string OrderDateChange { get; set; } // This is the date when the order was changed
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
    public string ShippingMethod { get; set; }
    public string Anonymous { get; set; }
    public string User_id { get; set; } // This is the user id of the person who placed the order
    public string User_name { get; set; } // This is the name of the person who placed the order
    public List<OrderItem> OrderItems { get; set; } // This should be a list of OrderItem objects
    public List<Payments> Payments { get; set; } = [];
}

public class OrderItem
{
    public string Id { get; set; }
    public string OrderId { get; set; }
    public string Sku_Id { get; set; }
    public string Sku_Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string Notes { get; set; } // Optional notes for the order item
}

