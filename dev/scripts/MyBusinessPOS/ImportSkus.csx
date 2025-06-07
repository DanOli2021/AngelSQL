// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

#load "..\POSApi\Sku.csx"

using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.IO;

string ConnectionString = GetVariable("ANGELSQL_MYBUSINESSPOS", @"Data Source=.\MYBUSINESSPOSV24;Initial catalog=MyBusiness2024;User Id=sa;Password=12345678;Persist Security Info=True;");

string GetVariable(string name, string default_value)
{
    if (Environment.GetEnvironmentVariable(name) == null) return default_value;
    return Environment.GetEnvironmentVariable(name);
}

AngelDB.DB sqlserver_db = new();
result = sqlserver_db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local");

//Console.WriteLine(result);

Data_import mybusiness_Import = new();
server_db.CreateTable(mybusiness_Import, "data_import");
string result = server_db.Prompt($"SELECT * FROM data_import WHERE id = 'MyBusiness2024-prods'", true);

if (result.StartsWith("Error:"))
{
    return "Error: mybusiness_import " + result.Replace("Error: ", "");
}

byte[] timestamp = [];

if (result == "[]")
{
    // Si no hay un timestamp previo, obten los primeros 100 registros
    result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT TOP 100 * FROM prods ORDER BY SSMA_TimeStamp CONNECTION ALIAS local", true);
}
else
{
    // Obtén el valor Base64 de "last_timestamp"
    string base64Timestamp = sqlserver_db.GetDataRow(result)["last_timestamp"].ToString();
    // Decodifica el Base64 a bytes
    timestamp = Convert.FromBase64String(base64Timestamp);
    // Convierte los bytes a hexadecimal
    string hexTimestamp = BitConverter.ToString(timestamp).Replace("-", "");
    // Utiliza el timestamp hexadecimal en el query
    result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT TOP 100 * FROM prods WHERE SSMA_TimeStamp > 0x{hexTimestamp} ORDER BY SSMA_TimeStamp CONNECTION ALIAS local", true);
}

if (result.StartsWith("Error:")) return result;

DataTable dt = sqlserver_db.GetDataTable(result);

List<Sku> skus = new();

ProcessSkus(sqlserver_db, dt);

public string ProcessSkus(AngelDB.DB sqlserver_db, DataTable dt_skus)
{
    List<Sku> skus = [];
    string result = "";
    string last_timestamp_imported = "";

    foreach (DataRow row in dt_skus.Rows)
    {

        var sku = new Sku
        {
            Id = row["articulo"].ToString(),
            Description = row["descrip"].ToString(),
            Unit_of_measurement = row["unidad"].ToString(),
            Price = Convert.ToDecimal(row["precio1"]),
            Cost = Convert.ToDecimal(row["costo_u"]),
            Requires_inventory = ToBool(Convert.ToInt64(row["invent"])),
            Url_media = "",
            It_is_for_sale = ToBool(Convert.ToInt64(row["paraventa"])),
            Sale_in_bulk = ToBool(Convert.ToInt64(row["granel"])),
            Require_series = ToBool(Convert.ToInt64(row["serie"])),
            Require_lots = ToBool(Convert.ToInt64(row["lote"])),
            Its_kit = ToBool(Convert.ToInt64(row["kit"])),
            Sell_below_cost = ToBool(Convert.ToInt64(row["BajoCosto"])),
            Locked = ToBool(Convert.ToInt64(row["bloqueado"])),
            Weight_request = ToBool(Convert.ToInt64(row["peso"])),
            Weight = Convert.ToDecimal(row["speso"]),
            ClaveProdServ = row["claveprodserv"].ToString(),
            ClaveUnidad = row["claveunidad"].ToString(),
            From_cfdi = false,
            Analized = false,
            Universal_id = row["guid"].ToString(),
            Deleted = false,
            DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            User_id = row["usuario"].ToString(),
            Currency_id = "MXN",
            Maker_id = row["fabricante"].ToString(),
            Brand_id = row["marca"].ToString(),
            Location_id = row["ubicacion"].ToString(),
            Price_code_id = "México"
        };

        if (File.Exists(row["imagen"].ToString()))
        {
            string imagePath = row["imagen"].ToString();
            string mimeType = GetMimeType(imagePath); // obtenemos el tipo MIME según la extensión
            string base64Image = Convert.ToBase64String(File.ReadAllBytes(imagePath));
            sku.Image = $"data:{mimeType};base64,{base64Image}";
        }

        var linea = new SkuClassification
        {
            Id = row["linea"].ToString(),
            Type = "Line",
            Description = row["linea"].ToString()
        };

        sku.SkuClassification.Add(linea);

        var marca = new SkuClassification
        {
            Id = row["marca"].ToString(),
            Type = "Brand",
            Description = row["marca"].ToString()
        };
        sku.SkuClassification.Add(marca);

        if (!string.IsNullOrEmpty(row["ubicacion"].ToString()))
        {
            var ubicacion = new SkuClassification
            {
                Id = row["ubicacion"].ToString(),
                Type = "Location",
                Description = row["ubicacion"].ToString()
            };
            sku.SkuClassification.Add(ubicacion);
        }   

        result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT * FROM clavesadd WHERE articulo = '{row["articulo"]}' CONNECTION ALIAS local", true);

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        DataTable clavesadd = sqlserver_db.GetDataTable(result);

        foreach (DataRow claveadd in clavesadd.Rows)
        {
            var dictionary = new Sku_dictionary
            {
                Id = claveadd["clave"].ToString(),
                Description = claveadd["dato1"].ToString(),
                Sku_id = sku.Id,
                Equivalence = Convert.ToDecimal(claveadd["cantidad"])
            };
            sku.Sku_dictionary.Add(dictionary);
        }

        result = sqlserver_db.Prompt($"SQL SERVER QUERY SELECT * FROM impuestos WHERE impuesto = '{row["impuesto"]}' CONNECTION ALIAS local", true);
        DataTable impuesto = sqlserver_db.GetDataTable(result);

        if (impuesto.Rows.Count > 0)
        {
            var consumptionTax = new Consumption_tax
            {
                Id = impuesto.Rows[0]["impuesto"].ToString(),
                Description = impuesto.Rows[0]["descrip"].ToString(),
                Rate = Convert.ToDecimal(impuesto.Rows[0]["valor"]),
                Type = ""
            };
            sku.Consumption_taxes.Add(consumptionTax);
        }

        last_timestamp_imported = row["SSMA_TimeStamp"].ToString();

        skus.Add(sku);
    }

    if (skus.Count == 0)
    {

        return "Ok.";
    }

    AngelDB.AngelComm angelComm = new("http://localhost:11000", "angelsql", "es");
    string token = angelComm.Login("angelsql", "master", "changeme");

    if (token.StartsWith("Error:"))
    {
        return token;
    }

    result = angelComm.Send("pos_backend/pos_backend", "UpsertSku", skus, "C#");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    Data_import data_import = new()
    {
        id = "MyBusiness2024-prods",
        Last_timestamp = last_timestamp_imported
    };

    result = server_db.UpsertInto("data_import", data_import);

    if (result.StartsWith("Error:"))
    {
        return "Error: Upsert data imports " + result.Replace("Error:", "");
    }

    return "Ok.";

}


static string ConvertImageToBase64(string filePath)
{
    if (!File.Exists(filePath))
        throw new FileNotFoundException("El archivo no existe.", filePath);

    byte[] imageBytes = File.ReadAllBytes(filePath);
    return Convert.ToBase64String(imageBytes);
}


public static bool ToBool(Int64 uValue)
{
    return uValue != 0;
}


public static string GetMimeType(string filePath)
{
    string extension = Path.GetExtension(filePath).ToLowerInvariant();

    return extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
}


class Data_import
{
    public string id { get; set; }
    public string Last_timestamp { get; set; }
}


