// GLOBALS
// These lines of code go in each script
#load "Globals.csx"
// END GLOBALS

// Process to send messages to user
// Daniel() Oliver Rojas
// 2023-05-19

#load "translations.csx"

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class AngelApiOperation
{
    public string OperationType { get; set; }
    public string Token { get; set; }
    public string User { get; set; }
    public string UserLanguage { get; set; }
    public dynamic DataMessage { get; set; }
}

private AngelApiOperation api = JsonConvert.DeserializeObject<AngelApiOperation>(message);

//Server parameters
private Dictionary<string, string> parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Environment.GetEnvironmentVariable("ANGELSQL_PARAMETERS"));
private Translations translation = new();
translation.SpanishValues();
 
// Create required tables
CreateTables(db);

//private string ConnectionString = GetVariable("ANGELSQL_MYBUSINESSPOS", @"Data Source=.\MYBUSINESSPOS;Initial catalog=MyBusiness2024;User Id=sa;Password=12345678;Persist Security Info=True;");
private string ConnectionString = GetVariable("ANGELSQL_MYBUSINESSPOS", @"Data Source=192.168.1.20\MYBUSINESSPOS,49784;Initial catalog=MyBusiness2024;User Id=sa;Password=12345678;Persist Security Info=True;");

// This is the main function that will be called by the API
return api.OperationType switch
{
    "GetTablesFromWaiter" => GetTablesFromWaiter(api, translation),
    "GetWaitersTables" => GetWaitersTables(api, translation),
    "SaveWaitersTables" => SaveWaitersTables(api, translation),
    "DeleteWaitersTables" => DeleteWaitersTables(api, translation),
    "GetWaiters" => GetWaiters(api, translation),
    "GetWaiterTables" => GetWaiterTables(api, translation), 
    "GetTableCommands" => GetTableCommands(api, translation),
    "SaveTableOrder" => SaveTableOrder(api, translation),
    "DeleteTableOrder" => DeleteTableOrder(api, translation),    
    "GetClassifications" => GetClassifications(api, translation),
    "GetSubClassifications" => GetSubClassifications(api, translation),
    "GetMenuClassifications" => GetMenuClassifications(api, translation),
    "ConfirmOrderForPrinting" => ConfirmOrderForPrinting(api, translation),
    "GetMenuOptions" => GetMenuOptions(api, translation),   
    "GetIdOrder" => GetIdOrder(api, translation),     
    "SaveOrderPreference" => SaveOrderPreference(api, translation),
    _ => $"Error: No service found {api.OperationType}",
};

string GetTablesFromWaiter(AngelApiOperation api, Translations translation)
{
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation);
    if (data.Id is null) return "Error: " + translation.Get("Id not found", api.UserLanguage);

    result = db.Prompt("SELECT * FROM waiters_tables WHERE id = '" + data.Id.ToString().Trim() + "'");

    if (result.StartsWith("Error:")) return result;

    DataTable t = JsonConvert.DeserializeObject<DataTable>(result);

    Waiters_tables w = new()
    {
        Id = t.Rows[0]["id"].ToString(),
        Waiter = t.Rows[0]["Waiter"].ToString(),
        Name = t.Rows[0]["Name"].ToString(),
        Tables_assigned = t.Rows[0]["Tables_assigned"].ToString(),
    };

    return JsonConvert.SerializeObject(w);
}

string GetWaitersTables(AngelApiOperation api, Translations translation)
{
    string result = IsUserValid(api, translation, "HEAD_OF_WAISTERS");
    if( result.StartsWith("Error:") ) return result;        
    return db.Prompt("SELECT * FROM Waiters_tables ORDER BY Waiter, Name");
}


string SaveWaitersTables(AngelApiOperation api, Translations translation)
{
    string result = IsUserValid(api, translation, "HEAD_OF_WAISTERS");

    dynamic data = api.DataMessage;

    if ( data.Id is null ) 
    {
        data.Id = Guid.NewGuid().ToString();
    };

    if( string.IsNullOrEmpty(data.Id.ToString()) ) 
    {
        data.Id = Guid.NewGuid().ToString();
    };

    if (data.Waiter is null) return "Error: " + translation.Get("Waiter not found", api.UserLanguage);
    if (data.Tables_assigned is null) return "Error: " + translation.Get("Tables_assigned not found", api.UserLanguage);

    Waiters_tables waiter_details = new()
    {
        Id = data.Id.ToString().Trim(),
        Waiter = data.Waiter.ToString().Trim(),
        Name = data.Name.ToString().Trim(),
        Tables_assigned = data.Tables_assigned.ToString().Trim()
    };

    result = db.UpsertInto("Waiters_tables", waiter_details);

    return result;

}

string GetWaiterTables(AngelApiOperation api, Translations translation)
{
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation, "WAITER");

    if (result.StartsWith("Error:")) return result;

    if (data.Waiter is null) return "Error: " + translation.Get("Waiter parameter not found", api.UserLanguage);

    result = db.Prompt("SELECT Tables_assigned FROM waiters_tables WHERE Waiter = '" + data.Waiter.ToString().Trim() + "'");
    if (result.StartsWith("Error:")) return result;
    if( result == "[]" ) return "Error: " + translation.Get("No tables assigned to this waiter", api.UserLanguage);

    DataTable t = JsonConvert.DeserializeObject<DataTable>(result);
    List<string> tables_names = new();
    List<Table> waiter_tables = new();

    foreach (DataRow row in t.Rows)
    {
        List<string> tables_assigned = ConvertStringToList( row["Tables_assigned"].ToString() );

        foreach (string table in tables_assigned)
        {
            if(!tables_names.Contains(table)) 
            {
                tables_names.Add(table);
            }
        } 
    } 

    db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);
    
    insertTables(tables_names);
    result = db.Prompt($"SQL SERVER QUERY SELECT mesa, estado FROM r_mesas WHERE mesa IN ({string.Join(", ", tables_names.Select(elemento => $"'{elemento}'"))}) CONNECTION ALIAS local", true);

    t = JsonConvert.DeserializeObject<DataTable>(result);

    foreach (string item in tables_names)
    {
        DataRow[] rows = t.Select($"mesa = '{item}'");    

        if( rows.Length > 0 ) 
        {
            Table t1 = new()
            {
                TableName = rows[0]["mesa"].ToString().Trim(),
                Status = rows[0]["estado"].ToString().Trim()
            };

            waiter_tables.Add(t1);
        }     
    }

    return JsonConvert.SerializeObject(waiter_tables);
}

void insertTables(List<string> table)
{
    string result;
    result = db.Prompt($"SQL SERVER QUERY SELECT mesa, estado FROM r_mesas WHERE mesa IN ({string.Join(", ", table.Select(elemento => $"'{elemento}'"))}) CONNECTION ALIAS local", true);

    DataTable t = JsonConvert.DeserializeObject<DataTable>(result);

    foreach (string item in table)
    {
        DataRow[] rows = t.Select($"mesa = '{item}'");

        if( rows.Length == 0 )
        {
            result = db.Prompt($"SQL SERVER QUERY SELECT mesa FROM r_mesas WHERE mesa = '" + item + "' CONNECTION ALIAS local", true);

            var o = new 
            {
                mesa = item,
                estado = "LISTA",
                comensales = 0,        
                mesero = "",
                propina_porcentaje = "0.00",
                propina_importe = "0.00",      
                unionmesa = "",
                impresora = "",
                operadaPorComandera = 0,
                activa = 0
            };


            if (result == "[]")
            {
                result = db.Prompt($"SQL SERVER INSERT INTO r_mesas CONNECTION ALIAS local VALUES " + db.GetJson(o, false), true);
            }        
                
        }
    }
}

private class Table 
{
    public string TableName { get; set; } = "";
    public string Status { get; set; } = "";
}

string DeleteWaitersTables(AngelApiOperation api, Translations translation)
{

    string result = IsUserValid(api, translation, "HEAD_OF_WAISTERS");

    if( result.StartsWith("Error:") ) return result;

    dynamic data = api.DataMessage;

    if (data.Id is null) return "Error: " + translation.Get("Waiter not found (Id)", api.UserLanguage);

    result = db.Prompt($"DELETE FROM Waiters_tables PARTITION KEY main WHERE id = '{data.Id}'");
    return result;

}


string GetWaiters(AngelApiOperation api, Translations translation)
{
    string result = IsUserValid(api, translation, "HEAD_OF_WAISTERS");

    if(result.StartsWith("Error:"))
    {
        return result;
    }

    api.DataMessage = new { Group = "HEAD_OF_WAISTERS",
                            Where = "UserGroups LIKE '%WAITERS%'"
                          };

    result = SendToAngelPOST("tokens/admintokens", api.User, api.Token, "GetUsersBy", api.UserLanguage, api.DataMessage);

    if(result.StartsWith("Error:"))
    {
        return $"Error: {result}";
    }

    return result;
}


string GetTableCommands(AngelApiOperation api, Translations translation) 
{
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation, "WAITER");

    if (result.StartsWith("Error:")) return result;

    if( data.Table is null ) return "Error: " + translation.Get("Table parameter not found", api.UserLanguage );

    db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);
    result = db.Prompt($"SQL SERVER QUERY SELECT id, mesa, comensal, r_comandas.articulo, observaciones, precio, confirmado, descrip, imagen FROM r_comandas INNER JOIN prods ON r_comandas.articulo = prods.articulo WHERE mesa = '{data.Table}' AND eliminado = 0 ORDER BY comensal CONNECTION ALIAS local", true);

    if (result.StartsWith("Error:")) return result;

    DataTable t = JsonConvert.DeserializeObject<DataTable>(result);

    List<Order> orders = new();

    foreach (DataRow row in t.Rows)
    {

        string image = "";

        if(File.Exists(row["imagen"].ToString().Trim()))  
        {
            string file_name = Path.GetFileName(row["imagen"].ToString().Trim());

            if(!File.Exists( server_db.Prompt("VAR db_wwwroot") + "/RestaurantCommander/images/" + file_name))
            {
                File.Copy(row["imagen"].ToString().Trim(), server_db.Prompt("VAR db_wwwroot") + "/RestaurantCommander/images/" + file_name);
            }

            image = file_name;

        }

        Order o = new()
        {
            Id = row["id"].ToString().Trim(),
            Table = row["mesa"].ToString().Trim(),
            Sku = row["articulo"].ToString().Trim(),
            Price = row["precio"].ToString().Trim(),
            Confirmed = row["confirmado"].ToString().Trim(),
            Description = row["descrip"].ToString().Trim(),
            Person = row["comensal"].ToString().Trim(),
            Observations = row["observaciones"].ToString().Trim(),
            Image = image
        };

        orders.Add(o);
    }

    return JsonConvert.SerializeObject(orders);

}

//Agregados

string SaveOrderPreference( AngelApiOperation api, Translations translation ) 
{
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation, "WAITER");
    if (result.StartsWith("Error:")) return result;    
    if (data.id_comanda is null) return "Error: " + translation.Get("Order parameter not found", api.UserLanguage);
    if (data.id_menu is null) return "Error: " + translation.Get("Menu parameter not found", api.UserLanguage);
    if (data.seleccion is null) return "Error: " + translation.Get("Selection parameter not found", api.UserLanguage);
    if (data.notas is null) return "Error: " + translation.Get("Observations parameter not found", api.UserLanguage);

    db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);
    var o = new 
    {
        id_comanda = data.id_comanda,
        idComanda2 = data.id_comanda,
        id_menu = data.id_menu,
        componente = "",
        opcion1 = "",
        opcion2 = "",
        opcion3 = "",
        seleccion = data.seleccion,
        notas = data.notas,                                                                
        actualizado = 1
    };

    var updateCommand = new 
    {
        actualizado = 0
    };     

    db.Prompt($"SQL SERVER UPDATE r_comandas WHERE id = '" + data.id_comanda + "' CONNECTION ALIAS local VALUES " + db.GetJson(updateCommand, false), true);  
    result = db.Prompt($"SQL SERVER INSERT INTO r_seleccioncomensal CONNECTION ALIAS local VALUES " + db.GetJson(o, false), true);

    return result;

}

string GetIdOrder( AngelApiOperation api, Translations translation ) 
{
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation, "WAITER");
    if (result.StartsWith("Error:")) return result;   

    try
    {
        db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);   

        result = db.Prompt($"SQL SERVER QUERY SELECT MAX(id) As 'OrderID' FROM r_comandas CONNECTION ALIAS local", true);
        if (result == "[]")
        {
            return "Ok.";
        }

        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: Error getting ID. " + ex.Message);
        return "Error: Error getting ID." ;
    }        
}

string ConfirmOrderForPrinting( AngelApiOperation api, Translations translation ) 
{
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation, "WAITER");
    if (result.StartsWith("Error:")) return result;     

    if( data.Table is null ) return "Error: " + translation.Get("Table parameter not found", api.UserLanguage );

    try
    {
        db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);   

        var o = new 
        {
            confComandera = 1
        };       
             
        result = db.Prompt($"SQL SERVER UPDATE r_comandas WHERE mesa = '{data.Table}' AND confComandera = '0' CONNECTION ALIAS local VALUES " + db.GetJson(o, false), true);  
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: Failed to confirm table order." + ex.Message);
        return "Error: Failed to confirm table order." ;
    }
}

string DeleteTableOrder ( AngelApiOperation api, Translations translation ) 
{
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation, "WAITER");
    if (result.StartsWith("Error:")) return result;    

    try
    {
        db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);    
        db.Prompt($"SQL SERVER QUERY DELETE FROM r_seleccioncomensal WHERE id_comanda = " + data.Id + " CONNECTION ALIAS local", true);        
        result = db.Prompt($"SQL SERVER QUERY DELETE FROM r_comandas WHERE id = " + data.Id + " CONNECTION ALIAS local", true);
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: Failed to delete table order." + ex.Message);
        return "Error: Failed to delete table order." ;
    }
}

//Agregados

string SaveTableOrder( AngelApiOperation api, Translations translation ) 
{
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation, "WAITER");
    if (result.StartsWith("Error:")) return result;    
    if (data.Table is null) return "Error: " + translation.Get("Table parameter not found", api.UserLanguage);
    if (data.Sku is null) return "Error: " + translation.Get("Sku parameter not found", api.UserLanguage);
    if (data.Person is null) return "Error: " + translation.Get("Person parameter not found", api.UserLanguage);
    if (data.Observations is null) return "Error: " + translation.Get("Observations parameter not found", api.UserLanguage);

    db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);
   
    result = db.Prompt($"SQL SERVER QUERY SELECT id FROM r_comandas WHERE confirmado = 0 AND confComandera = 1 AND id = '" + data.Id + "' CONNECTION ALIAS local", true);
    if (result != "[]") 
    {
        return "Error: " + "The order has already been confirmed, it cannot be modified";
    }    
 
    result = db.Prompt($"SQL SERVER QUERY SELECT articulo, precio1 * (1 + (impuestos.valor / 100)) AS 'Precio', prods.descrip FROM prods INNER JOIN impuestos ON prods.impuesto = impuestos.impuesto WHERE articulo = '{data.Sku}' CONNECTION ALIAS local", true);

    if (result.StartsWith("Error:")) return result;
    DataTable t = JsonConvert.DeserializeObject<DataTable>(result);

    if( result == "[]" ) return "Error: " + translation.Get("Sku not found", api.UserLanguage);

    if (data.Id is null)
    {
        result = "[]";
    } else 
    {
        result = db.Prompt($"SQL SERVER QUERY SELECT id FROM r_comandas WHERE id = '" + data.Id + "' CONNECTION ALIAS local", true);
        if (result.StartsWith("Error:")) return result;
    };

    var o = new 
    {
        //id =  int.Parse( data.Id.ToString().Trim() ),
        mesa = data.Table,
        articulo = data.Sku,
        precio = decimal.Parse( t.Rows[0]["Precio"].ToString().Trim() ),        
        comensal = data.Person,
        observaciones = data.Observations,
        confirmado = 0,      
        encaja = 0,
        actualizado = 1
    };

    var changeStatus = new 
    {
        estado = "EN USO"
    };    

    var updateCommand = new 
    {
        actualizado = 0
    };        

    if (result == "[]")
    {
        result = db.Prompt($"SQL SERVER INSERT INTO r_comandas CONNECTION ALIAS local VALUES " + db.GetJson(o, false), true);
        db.Prompt($"SQL SERVER UPDATE r_mesas WHERE mesa = '" + data.Table + "' AND estado = 'LISTA' CONNECTION ALIAS local VALUES " + db.GetJson(changeStatus, false), true);        
        db.Prompt($"SQL SERVER UPDATE r_mesas WHERE mesa = '" + data.Table + "' AND estado = 'SUCIA' CONNECTION ALIAS local VALUES " + db.GetJson(changeStatus, false), true);               
    }
    else
    {
        result = db.Prompt($"SQL SERVER UPDATE r_comandas WHERE id = " + data.Id + " CONNECTION ALIAS local VALUES " + db.GetJson(o, false), true);
        db.Prompt($"SQL SERVER UPDATE r_seleccioncomensal WHERE idcomanda2 = '" + data.Id + "' OR id_comanda = '" + data.Id + "' CONNECTION ALIAS local VALUES " + db.GetJson(updateCommand, false), true);           
    }

    return result;
}


class Order 
{
    public string Id { get; set; }
    public string Table { get; set; }
    public string Sku { get; set; }
    public string Price { get; set; }
    public string Confirmed { get; set; }
    public string Description { get; set; }
    public string Observations { get; set; }
    public string Person { get; set; }
    public string Image { get; set; }
}


string GetClassifications(AngelApiOperation api, Translations translation) 
{
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation, "WAITER");
    if (result.StartsWith("Error:")) return result;   

    db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);
    result = db.Prompt($"SQL SERVER QUERY SELECT * FROM r_clasificacion WHERE incluirencarta = 1 CONNECTION ALIAS local", true);

    return result;

}

string GetSubClassifications(AngelApiOperation api, Translations translation) 
{
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation, "WAITER");
    if (result.StartsWith("Error:")) return result;   


    db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);
    result = db.Prompt($"SQL SERVER QUERY SELECT * FROM r_subclasificacion WHERE clasificacion = '" + data.Clasification + "' CONNECTION ALIAS local", true);

    // Console.WriteLine("Sub:" + result);
    return result;

}

string GetMenuClassifications(AngelApiOperation api, Translations translation) 
{    
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation, "WAITER");
    if (result.StartsWith("Error:")) return result;   

    db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);
    result = db.Prompt($"SQL SERVER QUERY SELECT * FROM r_menu WHERE subclasificacion = '" + data.SubClasification + "' CONNECTION ALIAS local", true);

    // Console.WriteLine("Menu:" + result);
    return result;

}

string GetMenuOptions(AngelApiOperation api, Translations translation) 
{    
    dynamic data = api.DataMessage;
    string result = IsUserValid(api, translation, "WAITER");
    if (result.StartsWith("Error:")) return result;   

    db.Prompt($@"SQL SERVER CONNECT {ConnectionString} ALIAS local", true);
    result = db.Prompt($"SQL SERVER QUERY SELECT * FROM r_menu WHERE articulo = '" + data.nPlatillo + "' CONNECTION ALIAS local", true);

    DataTable t = JsonConvert.DeserializeObject<DataTable>(result);

    string valorColumna1 = "";
    foreach (DataRow row in t.Rows)
    {
        valorColumna1 = row["id"].ToString();
        //Console.WriteLine($"Valor de la columna 1: {valorColumna1}");
    }

    result = db.Prompt($"SQL SERVER QUERY SELECT * FROM r_componentes WHERE id_menu = '" + valorColumna1 + "' AND (articulo NOT LIKE '%Componente%' OR opcion1 NOT LIKE '%Opción%'  OR opcion2 NOT LIKE '%Opción%' OR opcion3 NOT LIKE '%Opción%') CONNECTION ALIAS local", true);    

    if (result == "[]")
    {
        return "Ok.";
    }
    else
    {
        //Console.WriteLine("Opciones:" + result);
        return result;
    }   


}

string IsUserValid(AngelApiOperation api, Translations translation, string group = "WAITER")
{
    string result = GetGroupsUsingTocken(api.Token, api.User, api.UserLanguage);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    dynamic user_data = JsonConvert.DeserializeObject<dynamic>(result);

    if (user_data.groups == null)
    {
        return "Error: " + translation.Get("No groups found", api.UserLanguage);
    }

    if (!user_data.groups.ToString().Contains(group))
    {
        return "Error: " + translation.Get("User does not have permission to edit", api.UserLanguage);
    }
    
    return "Ok.";

}


private string GetGroupsUsingTocken(string token, string user, string language)
{

    var d = new
    {
        TokenToObtainPermission = token
    };

    string result = SendToAngelPOST("tokens/admintokens", user, token, "GetGroupsUsingTocken", language, d);

    if (result.StartsWith("Error:"))
    {
        return $"Error: {result}";
    }

    return result;

}


private string SendToAngelPOST(string api_name, string user, string token, string OPerationType, string Language, dynamic object_data)
{

    string db_account = user.Split("@")[1];

    var d = new
    {
        api = api_name,
        account = db_account,
        language = "C#",
        message = new
        {
            OperationType = OPerationType,
            Token = token,
            UserLanguage = Language,
            DataMessage = object_data
        }
    };

    string result = db.Prompt($"POST {server_db.Prompt("VAR server_tokens_url")} MESSAGE {JsonConvert.SerializeObject(d, Formatting.Indented)}", true);
    AngelDB.AngelResponce responce = JsonConvert.DeserializeObject<AngelDB.AngelResponce>(result);
    return responce.result;

}

string GetVariable(string name, string default_value)
{
    if (Environment.GetEnvironmentVariable(name) == null) return default_value;
    return Environment.GetEnvironmentVariable(name);
}


class Waiters_tables
{
    public string Id { get; set; }
    public string Waiter { get; set; } = "";
    public string Name { get; set; } = "";
    public string Tables_assigned { get; set; } = "";
}

private string CreateTables(AngelDB.DB db)
{
    string result;

    Waiters_tables w = new();
    result = db.CreateTable(w);
    return result;
}


public static List<string> ConvertStringToList(string input)
{
    List<string> result = new();
    string[] elements = input.Split(',');

    foreach (string element in elements)
    {
        if (element.Contains("-"))
        {
            var range = element.Split('-');
            int start = int.Parse(range[0]);
            int end = int.Parse(range[1]);

            for (int i = start; i <= end; i++)
            {
                result.Add(i.ToString());
            }
        }
        else
        {
            result.Add(element.Trim());
        }
    }

    return result;
}


