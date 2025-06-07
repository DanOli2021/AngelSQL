// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

// Process to send messages to user
// Daniel() Oliver Rojas
// 2025-05-22

#load "..\AngelComm\AngelComm.csx"
#load "translations.csx"

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

private Translations translation = new();

private AngelApiOperation api = JsonConvert.DeserializeObject<AngelApiOperation>(message);
api.db = db;
api.server_db = server_db;

// This is the main function that will be called by the API
return api.OperationType switch
{
    "PublicUrl" => PublicUrl(),
    _ => $"Error: No service found 1 {api.OperationType}",
};

string PublicUrl() 
{
    return server_db.Prompt("VAR db_public_url");
}


