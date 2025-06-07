#r "C:\AngelSQLNet\AngelSQL\db.dll"
#r "C:\AngelSQLNet\AngelSQL\Newtonsoft.Json.dll"

AngelDB.DB db = new();
AngelDB.DB server_db = new();
AngelDB.TaskState Taskstate = new();

string data = "";
string message = "";