// GLOBALS
// These lines of code go in each script
#load "Globals.csx"
// END GLOBALS

using System;
using Newtonsoft.Json;
using System.Collections.Generic;

string context_data = message.Substring(1);

string scripts_directory = server_db.Prompt("VAR db_scripts_directory");
return server_db.Prompt($"SCRIPT FILE {scripts_directory}/Get/Get.csx MESSAGE " + context_data, true, db);

