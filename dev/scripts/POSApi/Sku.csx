using System;
using System.Collections.Generic;

/// <summary>
/// Sku class, used to store the products that are sold in the POS and in inventory movements
/// </summary>
public class Sku
{
    // SKU identifier, or code, or barcode
    public string Id { get; set; }
    // SKU description
    public string Description { get; set; }
    // SKU Unit of measurement
    public string Unit_of_measurement { get; set; }
    // SKU Price
    public decimal Price { get; set; }
    // SKU Cost
    public decimal Cost { get; set; }
    // If the sku requires inventory
    public bool Requires_inventory { get; set; }
    // If the sku has an associated web url
    public string Url_media { get; set; }
    // If the sku is for sale
    public bool It_is_for_sale { get; set; }
    // If the sku is for sale in bulk
    public bool Sale_in_bulk { get; set; }
    // If the sku requires serial number control
    public bool Require_series { get; set; }
    // If the sku requires lot number control
    public bool Require_lots { get; set; }
    // If the sku is a kit, or a set of skus
    public bool Its_kit { get; set; }
    // If the sku can be sold below cost
    public bool Sell_below_cost { get; set; }
    // If the sku is locked
    public bool Locked { get; set; }
    // If the sku requires weight
    public bool Weight_request { get; set; }
    // SKU weight
    public decimal Weight { get; set; }
    // ClaveProdServ is a key assigned by the tax administration system in Mexico
    public string ClaveProdServ { get; set; }
    // ClaveUnidad is a unit of measurement assigned by the tax administration system in Mexico
    public string ClaveUnidad { get; set; }
    // If the sku is from a CFDI
    public bool From_cfdi { get; set; }
    // If the sku has been analized
    public bool Analized { get; set; }
    // It is a GUID assigned by MyBusiness POS, which uniquely identifies the SKU
    public string Universal_id { get; set; }
    // If the sku is deleted
    public bool Deleted { get; set; }
    // Date and time of the sku
    public string DateTime { get; set; }
    // User identifier
    public string User_id { get; set; }
    // User name
    public string User_name { get; set; }
    // Currency identifier
    public string Currency_id { get; set; }
    // Maker identifier
    public string Maker_id { get; set; }
    // Brand identifier
    public string Brand_id { get; set; }
    // Location identifier
    public string Location_id { get; set; }
    // Price code identifier
    public string Price_code_id { get; set; }
    // Image 
    public string Image { get; set; }
    // List of classifications
    public List<SkuClassification> SkuClassification { get; set; } = [];
    // List of sku dictionaries
    public List<Sku_dictionary> Sku_dictionary { get; set; } = [];
    // List of consumption taxes
    public List<Consumption_tax> Consumption_taxes { get; set; } = [];

}


public class Media 
{
    // Media identifier
    public string Id { get; set; }
    // Sku identifier
    public string Sku_id { get; set; }
    // Media description
    public string Description { get; set; }
    // Media type
    public string Type { get; set; }
    // Media url
    public string Src { get; set; }
    // Media size
    public decimal Size { get; set; }
    // Media date and time
    public string DateTime { get; set; }
}


/// <summary>
/// Skus classifications used in each sales detail and in the Skus catalog
/// </summary>
public class SkuClassification
{
    // Classification identifier
    public string Id { get; set; }
    // Classification type
    public string Type { get; set; }
    // Classification description    
    public string Description { get; set; }
    // Classification identifier as image in the POS
    public string Image { get; set; }
}


/// <summary>
/// Other codes by which a Sku can be located
/// </summary>
public class Sku_dictionary
{
    // Sku dictionary identifier
    public string Id { get; set; }
    // Sku identifier
    public string Sku_id { get; set; }
    // Description of the dictionary code
    public string Description { get; set; }
    // Equivalence of the dictionary code with the sku
    public decimal Equivalence { get; set; }
}


/// <summary>
/// Consumption taxes used in each sale detail and in the Skus catalog
/// </summary>
public class Consumption_tax
{
    // Consumption tax identifier
    public string Id { get; set; }
    // Consumption tax description
    public string Description { get; set; }
    // Consumption tax percentage
    public decimal Rate { get; set; }
    // Consumption tax type
    public string Type { get; set; }
}



/// <summary>
/// Stores information on how a sku that is made up of other parts is assembled, if it is a kit
/// </summary>
public class Component
{
    // Component identifier
    public string Id { get; set; }
    // The identifier of the Sku to be assembled
    public string Sku_id { get; set; }
    // The identifier of the skus that is used to assemble the Sku_id
    public string Component_sku { get; set; }
    // The minimum lot of the component
    public decimal Minimum_lot { get; set; }
    // The quantity of the component
    public decimal Qty { get; set; }
    // The warehouse where the component is stored
    public string Warehouse { get; set; }
    // The number of days it takes to process the component
    public decimal Process_days { get; set; }
    // Observations
    public string Observations { get; set; }
}



public class SkuChange
{
    // Sku identifier
    public string Id { get; set; }
    // Sku description
    public string Description { get; set; }
    // Sku price
    public decimal Price { get; set; }
    // Sku cost
    public decimal Cost { get; set; }
    // User identifier
    public string User_id { get; set; }
    // User name
    public string User_name { get; set; }
}