// GLOBALS
// These lines of code go in each script
#load "Globals.csx"
// END GLOBALS

#r "Newtonsoft.Json.dll"

using System;

string result = server_db.Prompt( "SELECT * FROM accounts" );

if (result == "[]") 
{

    Console.WriteLine("Creando cuenta por defecto...");

    var pin = new
    {
        id = Guid.NewGuid().ToString(),
        authorizer = "SYSTEM",
        authorizer_name = "SYSTEM",
        branch_store = "SYSTEM",
        pin_number = "0000",
        message = (string)null,
        authorizer_message = "Created by Default",
        pintype = "touser",
        date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
        expirytime = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
        minutes = 120,
        permissions = "Create account",
        confirmed_date = (string)null,
        user = "angelsql@angelsql.net",
        app_user = (string)null,
        app_user_name = (string)null,
        status = "pending"
    };

    server_db.UpsertInto("pins", pin);

    var message = new
    {
        OperationType = "CreateAccount",
        Token = "",
        DataMessage = new
        {
            Pin = "0000",
            AccountName = "angelsql",
            Name = "AngelSQL Master",
            Phone = "1234567890",
            User = "master",
            Password = "changeme",
            DefaultToken = "default_token"
        },
        UserLanguage = "es"
    };

    result = server_db.Prompt("SCRIPT FILE dev/scripts/tokens/createaccount.csx MESSAGE " + server_db.GetJson(message) );

    Console.WriteLine("-->" + result);

}


