#r "/AngelSQLNet/AngelSQL/db.dll"
#r "/AngelSQLNet/AngelSQL/Newtonsoft.Json.dll"

AngelDB.DB db = new();
AngelDB.DB server_db = new();
AngelDB.TaskState Taskstate = new();

string data = "";
string message = "";