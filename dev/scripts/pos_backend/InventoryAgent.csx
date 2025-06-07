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

    result = db.Prompt("SELECT PartitionKey, id, Sale_detail, Storage_id FROM sale WHERE IsInventoryAffected = 0", true);

    DataTable sales = db.GetDataTable(result);

    foreach (DataRow sale in sales.Rows)
    {

        if (sale["Storage_id"] is DBNull)
        {
            sale["Storage_id"] = GetParameter("storage", api);
        }

        result = db.Prompt($"SELECT * FROM storage WHERE id = '{sale["Storage_id"]}'", true);

        if( result == "[]")
        {
            Storage storage = new()
            {
                Id = sale["Storage_id"].ToString(),
                Description = sale["Storage_id"].ToString(),
                Type = "General",
                Location = "Main Warehouse",
                Capacity = 1000000,
                CurrentUsage = 0
            };

            result = db.UpsertInto("Storage", storage, storage.Id);

            if (result.StartsWith("Error:"))
            {
                return result + " (0)";
            }
        }

        List<Sale_detail> saleDetails = db.jSonDeserialize<List<Sale_detail>>(sale["Sale_detail"].ToString());

        foreach (Sale_detail saleDetail in saleDetails)
        {

            result = db.Prompt($"SELECT Requires_inventory FROM sku WHERE id = '{saleDetail.Sku_id}'", true);

            DataTable requiresInventoryTable = db.GetDataTable(result);

            if (requiresInventoryTable is null || requiresInventoryTable.Columns["Requires_inventory"].DataType != typeof(bool) || !Convert.ToBoolean(requiresInventoryTable.Rows[0]["Requires_inventory"]))
            {
                continue;
            }

            result = db.Prompt($"SELECT stock FROM inventory PARTITION KEY {sale["Storage_id"]} WHERE Sku_id = '{saleDetail.Sku_id}'", true);

            if (result == "[]")
            {
                Inventory new_inventory = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    User_id = sale["User_id"].ToString(),
                    Sku_id = saleDetail.Sku_id,
                    Description = saleDetail.Description,
                    Storage_id = sale["Storage_id"].ToString(),
                    Stock = 0
                };

                result = db.UpsertInto("Inventory", new_inventory, new_inventory.Id);

                if (result.StartsWith("Error:"))
                {
                    return result + " (0)";
                }

                result = db.Prompt($"SELECT stock FROM inventory PARTITION KEY {sale["Storage_id"]} WHERE Sku_id = '{saleDetail.Sku_id}'", true);

            }

            DataTable inventory = db.GetDataTable(result);
            Decimal stock = 0;

            if (inventory.Rows[0]["stock"] is not DBNull)
            {
                stock = Convert.ToDecimal(inventory.Rows[0]["stock"]);
            }

            stock -= saleDetail.Qty;

            Inventory inventoryUpdate = new()
            {
                Id = inventory.Rows[0]["id"].ToString(),
                DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                User_id = sale["User_id"].ToString(),
                Sku_id = saleDetail.Sku_id,
                Description = saleDetail.Description,
                Storage_id = sale["Storage_id"].ToString(),
                Stock = stock
            };

            result = db.UpsertInto("Inventory", inventoryUpdate, inventoryUpdate.Id);   

            if (result.StartsWith("Error:"))
            {
                return result + " (1)";
            }

            Kardex kardex = new()
            {
                Id = saleDetail.Id,
                EntryOrExit = "Exit",
                ReferenceID = sale["id"].ToString(),
                ReferenceType = "Sale",
                Sku_id = saleDetail.Sku_id,
                Sku_description = saleDetail.Description,
                Quantity = saleDetail.Qty,
                Cost = saleDetail.Cost,
                Price = saleDetail.Price,
                DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Stock = inventoryUpdate.Stock
            };

            result = db.UpsertInto("Kardex", kardex, kardex.DateTime[..7]);

            if (result.StartsWith("Error:"))
            {
                return result + " (3)";
            }

        }

        db.Prompt($"UPDATE sale PARTITION KEY {sale["PartitionKey"]} SET IsInventoryAffected = 1 WHERE id = '{sale["id"]}'", true);

    }

    return "Ok.";
}


