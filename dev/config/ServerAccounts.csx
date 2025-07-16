// GLOBALS
// These lines of code go in each script
#load "Globals.csx"
// END GLOBALS

using System;
using System.Data;
using Newtonsoft.Json;
using System.Collections.Generic;

Dictionary<string, string> commands = server_db.jSonDeserialize<Dictionary<string, string>>(message);

string result = server_db.Prompt($"SELECT * FROM accounts WHERE account = '{commands["create_server_account"]}'");

if (result != "[]")
{
    return "Error: The account already exist: " + commands["create_server_account"];
}

//Console.WriteLine("Creating account: " + commands["create_server_account"]);

var pin = new
{
    id = System.Guid.NewGuid().ToString(),
    authorizer = "SYSTEM",
    authorizer_name = "SYSTEM",
    branch_store = "SYSTEM",
    pin_number = "0000",
    message = (string)null,
    authorizer_message = "Created by Default",
    pintype = "touser",
    date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
    expirytime = DateTime.Now.ToUniversalTime().AddDays(2).ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
    minutes = 120,
    permissions = "Create account",
    confirmed_date = (string)null,
    user = commands["user"],
    app_user = (string)null,
    app_user_name = (string)null,
    status = "pending"
};

server_db.UpsertInto("pins", pin);

var api_message = new
{
    OperationType = "CreateAccount",
    Token = "",
    DataMessage = new
    {
        Pin = "0000",
        AccountName = commands["create_server_account"],
        Name = commands["create_server_account"],
        Phone = "1234567890",
        User = commands["user"],
        Password = commands["password"],
        DefaultToken = System.Guid.NewGuid().ToString(),
        DataDirectory = commands["data_directory"]
    },
    UserLanguage = "en"
};

if (!AngelDBTools.StringFunctions.IsStringValidPassword(commands["password"]))
{
    return $"Syntax error, the following KeyWord PASSWORD, has invalid characters, minimum 8 characters: {commands["password"]}";
}
else 
{
    //Console.WriteLine("Password fine");
}

result = server_db.Prompt("SCRIPT FILE dev/scripts/tokens/createaccount.csx MESSAGE " + server_db.GetJson(api_message));

return result;

