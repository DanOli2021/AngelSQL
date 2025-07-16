// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

// Script for system access management, it is based on the generation of Tokens, 
// for which first user groups must be created, which define their access levels, 
// the necessary users are created indicating the group they belong to and in the end, 
//an access token is generated indicating the user it is intended to assign 
//for use in all operations within the system
// Daniel() Oliver Rojas
// 2023-05-19

#load "tokens.csx"
#load "users.csx"
#load "usersgroup.csx"
#load "branch_stores.csx" 
#load "pins.csx"

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Data;

public class AngelApiOperation
{
    public string OperationType { get; set; }
    public string Token { get; set; }
    public dynamic DataMessage { get; set; }
}

AngelApiOperation api = JsonConvert.DeserializeObject<AngelApiOperation>(message);

//Server parameters
Dictionary<string, string> parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Environment.GetEnvironmentVariable("ANGELSQL_PARAMETERS"));

return api.OperationType switch
{
    "CreateAccount" => CreateNewAccount(server_db, api),
    _ => $"Error: No service found {api.OperationType}",
};


// CreateAccount
// This method is used to create a new user account in the system,
string CreateNewAccount(AngelDB.DB server_db, AngelApiOperation api)
{
    var d = api.DataMessage;

    if (d.Pin == null) return "Error: Pin is required";

    if (d.AccountName == null) return "Error: AccountName is required";

    if (d.Password == null) return "Error: Password is required";

    if (d.Name == null) return "Error: Name is required";

    if (d.User == null) return "Error: User is required";

    d.User = d.User.ToString().Trim().ToLower();

    d.Pin = d.Pin.ToString().Trim();

    string result = server_db.Prompt($"SELECT * FROM pins WHERE pin_number = '{d.Pin}' AND status = 'pending'");

    if (result == "[]")
    {
        return "Error: CreateNewAccount() Pin does not exist";
    }

    if (result.StartsWith("Error:"))
    {
        return "Error: CreateNewAccount() " + result.Replace("Error:", "");
    }

    DataTable p = server_db.GetDataTable(result);

    Pin my_pin = new()
    {
        Id = p.Rows[0]["id"].ToString(),
        Pin_number = p.Rows[0]["pin_number"].ToString(),
        User = p.Rows[0]["user"].ToString(),
        Expirytime = p.Rows[0]["expirytime"].ToString(),
        Date = p.Rows[0]["date"].ToString(),
        Status = "confirmed" // Change status to confirmed
    };

    server_db.UpsertInto("pins", my_pin);

    DateTime expirytime = DateTime.ParseExact(my_pin.Expirytime, "yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);

    if (DateTime.Now > expirytime)
    {
        return "Error: CreateAccount() Pin has expired";
    }

    if (p.Rows[0]["status"].ToString() != "pending")
    {
        return "Error: CreateAccount() status is not pending";
    }

    if (my_pin.Pin_number != d.Pin.ToString())
    {
        return "Error: CreateAccount() Pin does not match";
    }

    //result = server_db.Prompt($"SELECT * FROM accounts WHERE email = '{my_pin.user}'", true);

    //if (result != "[]")
    //{
    //    return "Error: CreateAccount() Email already exists in another account";
    //}

    result = server_db.Prompt($"SELECT * FROM accounts WHERE id = '{d.AccountName}'");

    if (result != "[]")
    {
        return "Error: CreateAccount() AccountName already exists take another";
    }

    if (!StringValidator.IsValid(d.AccountName.ToString()))
    {
        return "Error: CreateAccount() AccountName is not valid, only letters and numbers are allowed, minimun 5 characters, maximun 30 characters";
    }

    AngelDB.DB db = new();

    if (d.DataDirectory == null || d.DataDirectory == "" || d.DataDirectory == "null")
    {
        result = db.Prompt($"DB USER db PASSWORD db DATA DIRECTORY {parameters["accounts_directory"]}/{d.AccountName}");
    }
    else
    {
        result = db.Prompt($"DB USER db PASSWORD db DATA DIRECTORY {d.DataDirectory}");
    }

    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine($"Creating account {d.AccountName} with user {d.User} and password {d.Password} data directory {d.DataDirectory}");

    if (result.StartsWith("Error:"))
    {
        return result + " (1)";
    }

    result = db.Prompt($"CHANGE MASTER TO USER {d.User} PASSWORD {d.Password}");

    if (result.StartsWith("Error:"))
    {
        return result + " (2)";
    }

    result = db.Prompt($"CREATE ACCOUNT {d.AccountName} SUPERUSER user_{d.AccountName} PASSWORD {d.Password}");

    if (result.StartsWith("Error:"))
    {
        return result + " (3)";
    }

    result = db.Prompt($"USE ACCOUNT {d.AccountName}");

    if (result.StartsWith("Error:"))
    {
        return result + " (4)";
    }

    result = db.Prompt($"CREATE DATABASE {d.AccountName}_data1");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    result = db.Prompt($"USE DATABASE {d.AccountName}_data1");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    string main_directory = parameters["accounts_directory"].Replace(Environment.CurrentDirectory, "main_directory");

    var account = new
    {
        id = d.AccountName,
        db_user = d.User.ToString(),
        name = d.Name,
        email = my_pin.User,
        connection_string = $"DB USER {d.User} PASSWORD {d.Password} DATA DIRECTORY main_directory/{d.AccountName}",
        db_password = d.Password.ToString(),
        database = $"{d.AccountName}_data1",
        data_directory = $"main_directory/{d.AccountName}",
        account = d.AccountName.ToString(),
        super_user = $"user_{d.AccountName}",
        super_user_password = d.Password.ToString(),
        active = "true",
        created = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
    };

    result = server_db.Prompt($"UPSERT INTO accounts VALUES {JsonConvert.SerializeObject(account)}");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    Tokens tokens = new();
    result = db.CreateTable(tokens);
    if (result.StartsWith("Error:")) return "Error: Creating table tokens " + result.Replace("Error:", "");

    Users users = new();
    result = db.CreateTable(users);
    if (result.StartsWith("Error:")) return "Error: Creating table users " + result.Replace("Error:", "");

    UsersGroup usersgroup = new();
    result = db.CreateTable(usersgroup);
    if (result.StartsWith("Error:")) return "Error: Creating table UsersGroup " + result.Replace("Error:", "");

    Branch_stores branch_store = new();
    result = db.CreateTable(branch_store);
    if (result.StartsWith("Error:")) return "Error: Creating table branch_stores " + result.Replace("Error:", "");

    Pin pin = new();
    result = db.CreateTable(my_pin, "pins");
    if (result.StartsWith("Error:")) return "Error: Creating table pins " + result.Replace("Error:", "");

    List<UsersGroup> groups = [];

    UsersGroup g = new()
    {
        id = "AUTHORIZERS",
        Name = "AUTHORIZERS",
        Permissions = ""
    };
    groups.Add(g);

    g = new UsersGroup
    {
        id = "PINSCONSUMER",
        Name = "PINSCONSUMER",
        Permissions = ""
    };
    groups.Add(g);

    g = new UsersGroup
    {
        id = "SUPERVISORS",
        Name = "SUPERVISORS",
        Permissions = ""
    };
    groups.Add(g);

    g = new UsersGroup
    {
        id = "ADMINISTRATIVE",
        Name = "ADMINISTRATIVE",
        Permissions = ""
    };
    groups.Add(g);

    g = new UsersGroup
    {
        id = "CASHIER",
        Name = "CASHIER",
        Permissions = ""
    };
    groups.Add(g);

    g = new UsersGroup
    {
        id = "HEAD_OF_WAISTERS",
        Name = "HEAD_OF_WAISTERS",
        Permissions = ""
    };
    groups.Add(g);

    g = new UsersGroup
    {
        id = "WAITERS",
        Name = "WAITERS",
        Permissions = ""
    };
    groups.Add(g);

    g = new UsersGroup
    {
        id = "STAKEHOLDER",
        Name = "STAKEHOLDER",
        Permissions = ""
    };
    groups.Add(g);

    g = new UsersGroup
    {
        id = "POS_DATA_CONSUMER",
        Name = "POS_DATA_CONSUMER",
        Permissions = ""
    };
    groups.Add(g);

    g = new UsersGroup
    {
        id = "POS_DATA_UPSERT",
        Name = "POS_DATA_UPSERT",
        Permissions = ""
    };
    groups.Add(g);

    g = new UsersGroup
    {
        id = "DOCEDITOR",
        Name = "DOCEDITOR",
        Permissions = ""
    };
    groups.Add(g);

    g = new UsersGroup
    {
        id = "DOCADMIN",
        Name = "DOCADMIN",
        Permissions = ""
    };
    groups.Add(g);


    result = db.Prompt("UPSERT INTO usersgroup VALUES " + JsonConvert.SerializeObject(groups));
    if (result.StartsWith("Error:")) return "Error: insert group ADMINISTRATIVE " + result.Replace("Error:", "");

    Users u = new()
    {
        Id = d.User.ToString(),
        Name = d.Name.ToString(),
        Password = d.Password.ToString(),
        UserGroups = "AUTHORIZERS, SUPERVISORS, PINSCONSUMER, ADMINISTRATIVE, CASHIER, WAITERS, HEAD_OF_WAISTERS, STAKEHOLDER, POS_DATA_CONSUMER, POS_DATA_UPSERT, DOCEDITOR, DOCADMIN",
        Organization = "",
        Email = my_pin.User,
        Phone = "",
        Permissions_list = "Any",
        Master = "true"
    };

    result = db.Prompt("UPSERT INTO users VALUES " + JsonConvert.SerializeObject(u));
    if (result.StartsWith("Error:")) return "Error: insert user " + result.Replace("Error:", "");

    Users u1 = new()
    {
        Id = "cashier",
        Name = "cashier",
        Password = "CASHIER",
        UserGroups = "CASHIER",
        Organization = "",
        Email = my_pin.User,
        Phone = "",
        Permissions_list = "",
        Master = "false"
    };

    result = db.Prompt("UPSERT INTO users VALUES " + JsonConvert.SerializeObject(u1));
    if (result.StartsWith("Error:")) return "Error: insert user " + result.Replace("Error:", "");

    string token = System.Guid.NewGuid().ToString();

    Tokens t = new()
    {
        Id = token,
        User = d.User.ToString(),
        ExpiryTime = DateTime.Now.AddYears(20).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
        CreationTime = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff")
    };

    db.Prompt("UPSERT INTO tokens VALUES " + JsonConvert.SerializeObject(t), true);

    sb.AppendLine($"Account {d.AccountName} and user {d.User}@{d.AccountName} created successfully.");
    return "Ok." + sb.ToString();

}


public static class StringValidator
{
    private static readonly Regex ValidStringPattern = new("^[a-zA-Z0-9]{1,30}$");

    public static bool IsValid(string input)
    {

        if (input.Length > 30) return false;
        if (input.Length < 5) return false;

        return ValidStringPattern.IsMatch(input);
    }

}

