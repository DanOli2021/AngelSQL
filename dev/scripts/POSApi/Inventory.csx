using System.Collections.Generic;

public class Kardex
{
    // Inventory identifier
    public string Id { get; set; }
    // Entry or exit
    public string EntryOrExit { get; set; }
    // Souce identifier
    public string ReferenceID { get; set; }
    // Source type
    public string ReferenceType { get; set; }
    // Sku identifier    
    public string Sku_id { get; set; }
    // Sku description
    public string Sku_description { get; set; }
    // Sku Quantity
    public decimal Quantity { get; set; }
    // Sku cost
    public decimal Cost { get; set; }
    // Sku price
    public decimal Price { get; set; }
    // Sku cost price
    public string DateTime { get; set; }
    // Existence
    public decimal Stock { get; set; }    

}


public class Storage
{
    // Storage identifier
    public string Id { get; set; }
    // Storage description
    public string Description { get; set; }
    // Storage type
    public string Type { get; set; }
    // Storage location
    public string Location { get; set; }
    // Storage capacity
    public decimal Capacity { get; set; }
    // Storage current usage
    public decimal CurrentUsage { get; set; }
}


public class Inventory
{
    // Inventory identifier
    public string Id { get; set; }
    // Date and time of the inventory
    public string DateTime { get; set; }
    // User identifier who created the inventory
    public string User_id { get; set; }
    // User name who created the inventory
    public string Sku_id { get; set; }
    public string Description { get; set; }
    public string Storage_id { get; set; }
    public decimal Stock { get; set; }
}

public class PhysicalInventory 
{
    // Physical inventory identifier
    public string Id { get; set; }
    // Date and time of the physical inventory
    public string DateTime { get; set; }
    // User identifier who created the physical inventory
    public string User_id { get; set; }
    // User name who created the physical inventory
    public string User_name { get; set; }
    // Description of the physical inventory
    public string Description { get; set; }
    // If the physical inventory is closed
    public int Closed { get; set; }
    public List<PhysicalInventoryDetail> Details { get; set; } = [];
}


public class PhysicalInventoryDetail
{
    // Sku identifier
    public string Sku_id { get; set; }
    // Sku description
    public string Sku_description { get; set; }
    // Quantity in physical inventory
    public decimal Count1 { get; set; }
    public decimal Count2 { get; set; }
    public decimal FinalCount { get; set; }
    public decimal PreviousExistence { get; set; }
    public decimal Adjustment { get; set; }
    // Cost of the sku
    public decimal Cost { get; set; }
    public string Observation { get; set; }
    // Date and time of the physical inventory detail
    public string DateTime { get; set; }
    // User identifier
    public string User_id { get; set; }
    // User name
    public string User_name { get; set; }
}