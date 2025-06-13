// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

#load "..\POSApi\Sales.csx"

using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;

string ConnectionString = GetVariable("ANGELSQL_MYBUSINESSPOS", @"Provider=SQLNCLI;Data Source=.\MYBUSINESSPOSV24;Initial catalog=MyBusiness2024;User Id=sa;Password=12345678;Persist Security Info=True;");

string GetVariable(string name, string default_value)
{
    if (Environment.GetEnvironmentVariable(name) == null) return default_value;
    return Environment.GetEnvironmentVariable(name);
}

AngelDB.DB sqlserver_db = new();
string resut = sqlserver_db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local");

//Console.WriteLine(result);

string result = "";

if (result.StartsWith("Error:"))
{
    return "Error: mybusiness_import " + result.Replace("Error: ", "");
}

sqlserver_db.Prompt($"SQL SERVER EXEC ALTER TABLE ventas ADD angelimport NVARCHAR(1) CONNECTION ALIAS local");
sqlserver_db.Prompt($"SQL SERVER EXEC CREATE INDEX ventas_angelimport ON ventas (angelimport) CONNECTION ALIAS local");

for (int i = 0; i < 100; i++)
{
    result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT TOP 1000 * FROM ventas WHERE estado = 'CO' AND angelimport IS NULL ORDER BY F_EMISION, USUHORA CONNECTION ALIAS local", true);

    if (result.StartsWith("Error:")) return result;

    DataTable dt = sqlserver_db.GetDataTable(result);
    List<Sale> sales = new();
    
    result = ProcessSales(sqlserver_db, dt);

    if (result.StartsWith("Error:"))
    {
        return "Error: ImportSales " + result.Replace("Error:", "");
    }

    sales.Clear();

}




public string ProcessSales(AngelDB.DB sqlserver_db, DataTable dt_sales)
{
    List<Sale> sales = [];
    string result = "";

    List<Customer> customer = [];

    foreach (DataRow row in dt_sales.Rows)
    {
        var sale = new Sale
        {
            Id = row["GUID"].ToString().Trim()
        };
        
        DateTime fecha = (DateTime)row["f_emision"];
        sale.DateTime = fecha.ToString("yyyy-MM-dd ") + row["usuhora"];
        sale.Receipt_serie = row["seriedocumento"].ToString().Trim();
        sale.Receipt_number = row["no_referen"].ToString();
        sale.Sale_type = row["tipo_doc"].ToString();
        sale.Currency_id = row["moneda"].ToString();
        sale.Exchange_rate = Convert.ToDecimal(row["tipo_cam"]);
        sale.Total = Convert.ToDecimal(row["impuesto"]) + Convert.ToDecimal(row["importe"]);
        sale.Subtotal = Convert.ToDecimal(row["importe"]);
        sale.Consumption_tax = Convert.ToDecimal(row["impuesto"]);
        sale.For_credit = 0;
        sale.ReferenceID = "";
        sale.ReferenceType = "";
        sale.User_id = row["usuario"].ToString();
        sale.Customer_id = row["cliente"].ToString();
        sale.Seller_id = row["vend"].ToString();
        sale.POS_ID = row["sucursal"] + "-" + row["estacion"];
        sale.Account_id = "";

        result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT * FROM monedas WHERE moneda = '{row["moneda"]}' CONNECTION ALIAS local", true);
        DataTable monedaTable = sqlserver_db.GetDataTable(result);
        sale.Currency_name = monedaTable.Rows.Count > 0 ? monedaTable.Rows[0]["Nombre"].ToString() : "";

        result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT * FROM usuarios WHERE usuario = '{row["usuario"]}' CONNECTION ALIAS local", true);
        DataTable usuario = sqlserver_db.GetDataTable(result);
        sale.User_name = usuario.Rows.Count > 0 ? usuario.Rows[0]["nombre"].ToString() : "";

        result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT * FROM clients WHERE cliente = '{row["cliente"]}' CONNECTION ALIAS local", true);
        DataTable cliente = sqlserver_db.GetDataTable(result);
        sale.Customer_name = cliente.Rows.Count > 0 ? cliente.Rows[0]["nombre"].ToString() : "";

        result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT * FROM vends WHERE vend = '{row["vend"]}' CONNECTION ALIAS local", true);
        DataTable vendedor = sqlserver_db.GetDataTable(result);
        sale.Seller_name = vendedor.Rows.Count > 0 ? vendedor.Rows[0]["nombre"].ToString() : "";

        result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT * FROM cfd_datos CONNECTION ALIAS local", true);
        DataTable geo = sqlserver_db.GetDataTable(result);
        sale.Location = geo.Rows.Count > 0 ? geo.Rows[0]["latitud"] + " " + geo.Rows[0]["longitud"] : "";

        result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT * FROM econfig CONNECTION ALIAS local", true);
        DataTable empresa = sqlserver_db.GetDataTable(result);
        sale.Business_data = empresa.Rows.Count > 0 ? empresa.Rows[0]["empresa"].ToString() : "";

        Decimal amount1 = Decimal.TryParse(row["pago1"].ToString(), out amount1) ? amount1 : 0;

        var pago1 = new Payments
        {
            Id = "Payment1 " + sale.User_id,
            Amount = amount1,
            Currency_id = sale.Currency_id,
            Exchange_rate = sale.Exchange_rate,
            Description = row["concepto1"].ToString()
        };

        sale.Payments.Add(pago1);

        Decimal amount2 = Decimal.TryParse(row["pago2"].ToString(), out amount2) ? amount2 : 0;

        if (Convert.ToDecimal(row["pago2"]) > 0)
        {
            var pago2 = new Payments
            {
                Id = "Payment2 " + sale.User_id,
                Amount = amount2,
                Currency_id = sale.Currency_id,
                Exchange_rate = sale.Exchange_rate,
                Description = row["concepto2"].ToString()
            };

            sale.Payments.Add(pago2);
        }

        Decimal amount3 = Decimal.TryParse(row["pago3"].ToString(), out amount3) ? amount3 : 0;

        if (Convert.ToDecimal(row["pago3"]) > 0)
        {
            var pago3 = new Payments
            {
                Id = "Payment3 " + sale.User_id,
                Amount = amount3,
                Currency_id = sale.Currency_id,
                Exchange_rate = sale.Exchange_rate,
                Description = row["concepto3"].ToString()
            };

            sale.Payments.Add(pago3);
        }

        result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT * FROM partvta WHERE venta = {row["venta"]} CONNECTION ALIAS local", true);
        DataTable articulos = sqlserver_db.GetDataTable(result);

        decimal cost = 0;

        foreach (DataRow articulo in articulos.Rows)
        {
            var saleDetail = new Sale_detail
            {
                Id = articulo["puid"].ToString().Trim(),
                Sale_id = sale.Id,
                Sku_id = articulo["articulo"].ToString(),
                Description = articulo["observ"].ToString(),
                Qty = Convert.ToDecimal(articulo["cantidad"]),
                Price = Convert.ToDecimal(articulo["precio"]) * (1 - (Convert.ToDecimal(articulo["descuento"]) / 100)),
                Original_price = Convert.ToDecimal(articulo["preciobase"]),
                Discount = Convert.ToDecimal(articulo["descuento"]),
                Import = Convert.ToDecimal(articulo["cantidad"]) * Convert.ToDecimal(articulo["precio"]),
                Cost = Convert.ToDecimal(articulo["costo_u"]),
                Consumption_tax_percentages = Convert.ToDecimal(articulo["impuesto"]),
                Consumption_tax = Convert.ToDecimal(articulo["cantidad"]) * Convert.ToDecimal(articulo["precio"]) * (Convert.ToDecimal(articulo["impuesto"]) / 100),
                Price_with_taxes = Convert.ToDecimal(articulo["precio"]) * (1 + (Convert.ToDecimal(articulo["impuesto"]) / 100)),
                PromotionCode = "",
                DateTime = sale.DateTime,
                User_id = sale.User_id,
                Sku_dictionary_id = articulo["clave"].ToString(),
                Qty_equivalence = Convert.ToDecimal(articulo["prcantidad"]),
                Description_equivalence = articulo["prdescrip"].ToString()
            };

            cost += saleDetail.Cost * saleDetail.Qty;

            result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT * FROM impuestos WHERE valor = {articulo["impuesto"]} CONNECTION ALIAS local", true);
            DataTable impuesto = sqlserver_db.GetDataTable(result);

            if (impuesto.Rows.Count > 0)
            {
                var imp = new Consumption_tax
                {
                    Id = impuesto.Rows[0]["impuesto"] + " " + saleDetail.Id,
                    Rate = Convert.ToDecimal(impuesto.Rows[0]["valor"]),
                    Description = impuesto.Rows[0]["descrip"].ToString(),
                    Type = ""
                };
                saleDetail.Consumption_taxes.Add(imp);
            }

            result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT * FROM prods WHERE articulo = '{saleDetail.Sku_id}' CONNECTION ALIAS local", true);
            DataTable sku = sqlserver_db.GetDataTable(result);

            if (sku.Rows.Count > 0)
            {
                saleDetail.Preferential_Classification = sku.Rows[0]["linea"].ToString().Trim();

                // saleDetail.Classifications.Add(new Classification
                // {
                //     Id = "Line " + saleDetail.Id,
                //     Type = "line",
                //     Description = sku.Rows[0]["linea"].ToString()
                // });
                // saleDetail.Classifications.Add(new Classification
                // {
                //     Id = "Brand " + saleDetail.Id,
                //     Type = "brand",
                //     Description = sku.Rows[0]["marca"].ToString()
                // });
                // saleDetail.Classifications.Add(new Classification
                // {
                //     Id = "Location " + saleDetail.Id,
                //     Type = "Location",
                //     Description = sku.Rows[0]["ubicacion"].ToString()
                // });
            }

            sale.Sale_detail.Add(saleDetail);

        }

        sale.Cost = cost;
        sale.Number_of_items = sale.Sale_detail.Count;
        sales.Add(sale);
    }

    if (sales.Count == 0)
    {
        return "Ok.";
    }

    AngelDB.AngelComm angelComm = new("http://localhost:11000", "angelsql", "es");
    string token = angelComm.Login("angelsql", "master", "changeme");

    if (token.StartsWith("Error:"))
    {
        return token;
    }

    result = angelComm.Send("pos_backend/pos_backend", "UpsertSale", sales, "C#");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    foreach (Sale sale in sales)
    {
        result = sqlserver_db.Prompt($"SQL SERVER EXEC UPDATE ventas SET angelimport = '1' WHERE guid = '{sale.Id}' CONNECTION ALIAS local", true);

        if (result.StartsWith("Error:"))
        {
            return result + " 2";
        }
    }

    return "Ok.";

}



public static bool ToBool(int uValue)
{
    return uValue != 0;
}

class Data_import
{
    public string id { get; set; }
    public string Last_timestamp { get; set; }
}