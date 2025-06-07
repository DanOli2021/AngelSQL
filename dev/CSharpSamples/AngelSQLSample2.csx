// GLOBALS
// These lines of code go in each script
// El proposito de estas lineas es simplemente evitar que Visual Studio Code muestre errores en el editor
// Durante la ejecucion de los scripts, estas lineas son ignoradas
#load "Globals.csx"
// END GLOBALS

using System;
using System.Collections.Generic;
using System.Data;

// Ejemplo de cómo importar datos desde SQL Server
// Se usa una cadena de conexión por defecto, pero se puede cambiar al usar una variable de entorno
// Por seguridad, no se recomienda poner la cadena de conexión en el script
string ConnectionString = GetVariable("ANGELSQL_MYBUSINESSPOS", @"Data Source=.\MYBUSINESSPOS;Initial catalog=MyBusiness2024;User Id=sa;Password=12345678;Persist Security Info=True;");

string result = "";

// Creamos un objeto de la clase AngelDB.DB, no usamos el objeto db que se recibe en el script, por razones de concurrencia
AngelDB.DB sqlserver = new();

// Conectamos a la base de datos
result = db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);

Console.WriteLine("Conectado a la base de datos: " + result);

// Ejecutamos una consulta
result = db.Prompt($"SQL SERVER QUERY SELECT articulo, descrip, precio1, impuesto, imagen FROM prods CONNECTION ALIAS local", true);

// Mostramos el resultado
// Console.WriteLine("Resultado de la consulta: " + result);
// Convertimos el resultado a un datatable
DataTable dt = db.GetDataTable(result);

// El segundo parámetro del método Prompt si es true creara una excepción si hay un error
db.Prompt("DB USER db PASSWORD db DATA DIRECTORY /mydata/prods", true);


// Vemos si la cuenta ya existe y la creamos si no existe
result = db.Prompt("GET ACCOUNTS WHERE account = 'prodstest'", true);

// Si se devuelve un string con llaves vacias, la cuenta no existe
if( result == "[]" )
{
    db.Prompt("CREATE ACCOUNT prodstest SUPERUSER daniel PASSWORD &&/DJSDJSDññ", true);
}

db.Prompt("USE ACCOUNT prodstest", true);

// Si la base existe y la tratamos de crear de nuevo no nos va marcar ningun error  
db.Prompt("CREATE DATABASE prodstest", true);
db.Prompt("USE DATABASE prodstest", true);

//Creamos un objeto productos
class Producto
{
    public string Id { get; set; }
    public string Descrip { get; set; }
    public decimal Precio1 { get; set; }
}


// Creamos una lista de productos
List<Producto> productos = [];

// Recorremos el dataset
foreach (DataRow row in dt.Rows)
{
    productos.Add(new Producto
    {
        Id = row["articulo"].ToString(),
        Descrip = row["descrip"].ToString(),
        Precio1 = Convert.ToDecimal(row["precio1"])
    });
}


// Creamos la tabla productos
result = db.CreateTable(new Producto(), "productos");
Console.WriteLine("Tabla productos creada: " + result);

// Salvamos los datos en la base de datos
db.UpsertInto("productos", productos);

// Mostramos los registros de la tabla productos
result = db.Prompt("SELECT * FROM productos WHERE descrip LIKE '%arroz%'");

Console.WriteLine("Registros de la tabla productos: " + result);


string GetVariable(string name, string default_value)
{
    if (Environment.GetEnvironmentVariable(name) == null) return default_value;
    Console.WriteLine($"Variable {name} found");
    return Environment.GetEnvironmentVariable(name);
}
