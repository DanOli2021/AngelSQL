// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

using System;


// This script shows how to connect to a SQL Server database, query, insert, update and delete data
// using the AngelDB.DB class
// db object already exists in the global scope

// Connection string to the SQL Server database
string ConnectionString = @"Data Source=.\MYBUSINESSPOSV24;Initial catalog=MyBusiness2024;User Id=sa;Password=12345678;Persist Security Info=True;";

// Connect to the SQL Server database
db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS mybusinesspos", true);

// Check if the product line 'EJEMPLO' exists
string result = db.Prompt("SQL SERVER QUERY SELECT * FROM lineas WHERE linea = 'EJEMPLO' CONNECTION ALIAS mybusinesspos", true);
Console.WriteLine(result);

if( result.StartsWith("Error:") )
{
    return result;
}

// If the product line does not exist, insert it
if( result == "[]" )
{
    Lineas linea = new Lineas();
    linea.linea = "EJEMPLO";
    linea.descrip = "LINEA DE PRODUCTOS DE EJEMPLO";
    linea.usuario = "SUP";
    linea.usufecha = DateTime.Now;
    linea.usuhora = DateTime.Now.ToString("HH:MM:SS");    

    result = db.Prompt("SQL SERVER INSERT INTO lineas CONNECTION ALIAS mybusinesspos VALUES " +  db.GetJson(linea), true);

    if( result.StartsWith("Error:") )
    {
        return "Error: Inserting product line: " + result.Replace("Error: ", "");
    }
}
else
{
    // If the product line exists, update it
    Lineas linea = new Lineas();
    linea.linea = "EJEMPLO";
    linea.descrip = "LINEA DE PRODUCTOS DE EJEMPLO, ACTUALIZADA";
    linea.usuario = "SUP";
    linea.usufecha = DateTime.Now;
    linea.usuhora = DateTime.Now.ToString("HH:MM:SS");    

    result = db.Prompt("SQL SERVER UPDATE lineas WHERE linea = 'EJEMPLO' CONNECTION ALIAS mybusinesspos VALUES " +  db.GetJson(linea), true);

    if( result.StartsWith("Error:") )
    {
        return "Error: Inserting product line: " + result.Replace("Error: ", "");
    }

}

// Check if the product line 'EJEMPLO' exists
result = db.Prompt("SQL SERVER QUERY SELECT * FROM lineas WHERE linea = 'EJEMPLO' CONNECTION ALIAS mybusinesspos", true);
Console.WriteLine(result);

// If the product line exists, delete it
result = db.Prompt("SQL SERVER EXEC DELETE FROM lineas WHERE linea = 'EJEMPLO' CONNECTION ALIAS mybusinesspos", true);

if( result.StartsWith("Error:") )
{
    return "Error: Deleting product line: " + result.Replace("Error: ", "");
}


// Class to represent the 'lineas' table
class Lineas 
{
    public string linea { get; set; }
    public string descrip { get; set; }
    public string usuario { get; set; }
    public DateTime usufecha { get; set; }
    public string usuhora { get; set; }
}

