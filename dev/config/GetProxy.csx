// GLOBALS
// These lines of code go in each script
#load "Globals.csx"
// END GLOBALS

#r "Newtonsoft.Json.dll"

using System;
using Newtonsoft.Json;
using AngelDB;

string angel_proxy_account = server_db.Prompt("VAR db_proxy_account");
string angel_proxy_password = server_db.Prompt("VAR db_proxy_password");

if (string.IsNullOrEmpty(angel_proxy_account))
{
    return "Error: No proxy Account";
}

AngelSQLComm angelSQLComm = new AngelSQLComm("", "", "", "https://angelsql.net", "en");

string result;

for (int i = 0; i < 3; i++)
{
    try
    {
        result = angelSQLComm.Send("angelsql/register", "GetProxy", new { Account = angel_proxy_account, Password = angel_proxy_password });
        break;
    }
    catch (Exception e)
    {
        result = "Error: " + e.Message;

        if (e.InnerException != null)
        {
            result += " - " + e.InnerException.Message;
        }
        if (e.StackTrace != null)
        {
            result += "\n" + e.StackTrace;
        }
    }
}



return result;