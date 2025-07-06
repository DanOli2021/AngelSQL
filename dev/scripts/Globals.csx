#r "System.Runtime"
#r "System.Private.CoreLib"
#r "netstandard"
#r "C:\AngelSQLNet\AngelSQL\db.dll"
#r "C:\AngelSQLNet\AngelSQL\Newtonsoft.Json.dll"

// ...existing code...
AngelDB.DB db = new();
AngelDB.DB server_db = new();
AngelDB.TaskState Taskstate = new();

string data = "";
string message = "";