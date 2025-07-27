// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

#load "..\POSApi\Sales.csx"
#load "..\POSApi\Inventory.csx"
#load "..\AngelComm\AngelComm.csx"

using System;
using System.Collections.Generic;
using System.Data;


private AngelApiOperation api = new();
api.db = db;
api.server_db = server_db;


string result = AffectSales();

if (result.StartsWith("Error:"))
{
    Console.WriteLine(result);
    return result;
}




string AffectSales()
{
    
    string result = db.Prompt($"GET TABLES WHERE tablename = 'sale'");

    if (result == "[]")
    {
        return "Ok.";
    }

    result = db.Prompt("SELECT id, PartitionKey FROM sale WHERE IsInventoryAffected = 0 ORDER BY DateTime ASC LIMIT 100", true);

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    if (result == "[]")
    {
        return "Ok.";
    }

    DataTable sales = db.GetDataTable(result);

    foreach (DataRow saleRow in sales.Rows)
    {

        result = db.Prompt($"SELECT * FROM sale WHERE id = '{saleRow["id"]}'", true);
       
        if (result.StartsWith("Error:"))
        {
            return result + " (2)";
        }

        if (result == "[]")
        {
            continue; // Skip if no sale found
        }

        result = result.Trim('[', ']'); // Remove brackets from the JSON result

        Sale sale = db.DeserializeDBResult<Sale>(result);

        if (sale.Storage_id == null)
        {
            sale.Storage_id = GetParameter("storage", api);
        }

        result = db.Prompt($"SELECT * FROM storage WHERE id = '{sale.Storage_id}'", true);

        if (result == "[]")
        {
            Storage storage = new()
            {
                Id = sale.Storage_id.ToString(),
                Description = sale.Storage_id.ToString(),
                Type = "General",
                Location = "Main Warehouse",
                Capacity = 1000000,
                CurrentUsage = 0
            };

            result = db.UpsertInto("Storage", storage);

            if (result.StartsWith("Error:"))
            {
                return result + " (0)";
            }
        }

        foreach (Sale_detail saleDetail in sale.Sale_detail)
        {

            if (saleDetail.Qty <= 0)
            {
                continue; // Skip if the quantity is zero or negative
            }

            result = db.Prompt($"SELECT Requires_inventory FROM sku WHERE id = '{saleDetail.Sku_id}'", true);

            DataTable requiresInventoryTable = db.GetDataTable(result);

            if (requiresInventoryTable is null || requiresInventoryTable.Columns["Requires_inventory"].DataType != typeof(bool) || !Convert.ToBoolean(requiresInventoryTable.Rows[0]["Requires_inventory"]))
            {
                continue;
            }

            result = db.Prompt($"SELECT id, stock FROM inventory PARTITION KEY {sale.Storage_id.ToLower()} WHERE id = '{saleDetail.Sku_id}'", true);

            if (result == "[]")
            {
                Inventory new_inventory = new()
                {
                    Id = saleDetail.Sku_id,
                    DateTime = saleDetail.DateTime,
                    User_id = sale.User_id,
                    //Sku_id = saleDetail.Sku_id,
                    Description = saleDetail.Description,
                    Storage_id = sale.Storage_id,
                    Stock = 0
                };

                result = db.UpsertInto("Inventory", new_inventory, sale.Storage_id);

                if (result.StartsWith("Error:"))
                {
                    return result + " (0)";
                }

            }

            result = db.Prompt($"SELECT id, stock FROM inventory PARTITION KEY {sale.Storage_id.ToLower()} WHERE id = '{saleDetail.Sku_id}'", true);

            Console.WriteLine(result);

            if (result == "[]")
            {
                return $"Error: Inventory for SKU {saleDetail.Sku_id} not found in storage {sale.Storage_id}.";
            }            

            DataTable inventory = db.GetDataTable(result);
            decimal stock = 0;

            if (inventory.Rows[0]["stock"] is not DBNull)
            {
                stock = Convert.ToDecimal(inventory.Rows[0]["stock"]);
            }

            stock -= saleDetail.Qty;

            Inventory inventoryUpdate = new()
            {
                Id = saleDetail.Sku_id,
                DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                User_id = sale.User_id,
                //Sku_id = saleDetail.Sku_id,
                Description = saleDetail.Description,
                Storage_id = sale.Storage_id,
                Stock = stock
            };

            result = db.UpsertInto("Inventory", inventoryUpdate, sale.Storage_id.ToLower());

            if (result.StartsWith("Error:"))
            {
                return result + " (1)";
            }

            Kardex kardex = new()
            {
                Id = saleDetail.Id,
                EntryOrExit = "Exit",
                Account_id = sale.Account_id,
                ReferenceID = sale.Id,
                ReferenceType = "Sale",
                Sku_id = saleDetail.Sku_id,
                Sku_description = saleDetail.Description,
                Storage_id = sale.Storage_id,
                Quantity = saleDetail.Qty,
                Cost = saleDetail.Cost,
                Price = saleDetail.Price,
                DateTime = saleDetail.DateTime,
                Stock = inventoryUpdate.Stock,
                ReferenceDocument = sale.Receipt_serie + " " + sale.Receipt_number,
            };

            result = db.UpsertInto("Kardex", kardex, kardex.DateTime[..7]);

            if (result.StartsWith("Error:"))
            {
                return result + " (3)";
            }

            saleDetail.IsInventoryAffected = true; // Mark the sale detail as affected by inventory

        }

        object sale_detail_clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(sale.Sale_detail);
        result = db.UpsertInto("Sale_detail", sale_detail_clone, saleRow["PartitionKey"].ToString());

        sale.IsInventoryAffected = true; // Mark the sale as affected by inventory
        object sale_clone = AngelDB.ObjectConverter.CreateDictionaryOrListFromObject(sale);
        result = db.UpsertInto("Sale", sale_clone, saleRow["PartitionKey"].ToString());

        if (result.StartsWith("Error:"))
        {
            return result + " (2)";
        }

    }

    return "Ok.";
}


