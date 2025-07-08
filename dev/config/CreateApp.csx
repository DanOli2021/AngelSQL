// GLOBALS
// These lines of code go in each script
#load "Globals.csx"
// END GLOBALS

using System;
using System.Collections.Generic;
using System.IO;

App app = db.jSonDeserialize<App>(message);

if (Directory.Exists(app.Files_directory))
{
    Console.WriteLine("Directory already exists: " + app.Files_directory);
}
else
{
    Directory.CreateDirectory(app.Files_directory);
    Console.WriteLine("Created directory: " + app.Files_directory);
}

string appPath = Path.Combine(app.Files_directory, app.App_name);

if (Directory.Exists(appPath))
{
    Console.WriteLine("App directory already exists: " + appPath);
}
else
{
    Directory.CreateDirectory(appPath);
    Console.WriteLine("Created app directory: " + appPath);
}

FileUtils.CopyDirectory($"{app.Main_directory}/config", Path.Combine(appPath, "config"), false, "AngelSQL.csx");

string configFile = Path.Combine(appPath, "config/AngelSQL.csx");

if (File.Exists(configFile))
{
    Console.WriteLine("Config file already exists: " + configFile);
}
else
{
    string config_result = ScriptGenerator.GenerateAngelSQLScriptFile(app);
    File.WriteAllText(configFile, config_result);
    Console.WriteLine("Created config file: " + configFile);
}

string scripts_directory = Path.Combine(appPath, "scripts");
FileUtils.CopyDirectory($"{app.Main_directory}/scripts", scripts_directory, false);

string wwwroot = Path.Combine(appPath, "wwwroot");
FileUtils.CopyDirectory($"{app.Main_directory}/wwwroot", wwwroot, false);

Console.WriteLine();
Console.WriteLine("RUN your APP from command line: C:/AngelSQLNet/AngelSQL/AngelSQLServer.exe APP DIRECTORY " + appPath);
Console.WriteLine();

return "Ok. App created successfully: " + app.App_name;

public class App
{
    public string App_name { get; set; }
    public string Files_directory { get; set; }
    public string Main_directory { get; set; }
}


public static class FileUtils
{
    public static void CopyDirectory(string sourceDir, string targetDir, bool overwrite = true, string excludeFile = "")
    {
        // Crea el directorio de destino si no existe
        Directory.CreateDirectory(targetDir);

        // Copia todos los archivos
        foreach (var file in Directory.GetFiles(sourceDir))
        {

            if (excludeFile.Contains(Path.GetFileName(file))) 
            {
                Console.WriteLine($"Skipping excluded file: {file}");
                continue; // Si el archivo está en la lista de exclusión, salta al siguiente archivo
            }

            string targetFilePath = Path.Combine(targetDir, Path.GetFileName(file));
            if (File.Exists(targetFilePath))
            {
                if (!overwrite)
                {
                    Console.WriteLine($"File already exists and overwrite is false: {targetFilePath}");
                    continue; // Si no se debe sobrescribir, salta al siguiente archivo
                }
                Console.WriteLine($"Overwriting file: {targetFilePath}");
            }
            File.Copy(file, targetFilePath, overwrite);
            Console.WriteLine($"Copied file: {file} to {targetFilePath}");
        }

        // Recurre en cada subdirectorio
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            string targetSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, targetSubDir, overwrite);
        }
    }
}


public class ScriptGenerator
{
    public static string GenerateAngelSQLScriptFile(App app)
    {
        int freePort = AngelDB.PortScanner.GetFreePort(); // 👈 Obtenemos el puerto libre

        string result = $@"// GLOBALS
// These lines of code go in each script
#load ""Globals.csx""
// END GLOBALS

#r ""Newtonsoft.Json.dll""

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

public class Certificates
{{
    public string Domain {{ get; set; }}
    public string Certificate {{ get; set; }}
    public string Password {{ get; set; }}
}}

List<Certificates> certificates = [];

certificates.Add(new Certificates
{{
    Domain = GetVariable(""ANGELSQL_DOMAIN1"", """"),
    Certificate = GetVariable(""ANGELSQL_CERTIFICATE1"", ""Certificate/localhost.pfx""),
    Password = GetVariable(""ANGELSQL_CERTIFICATE1_PASSWORD"", ""changeme"")
}});

certificates.Add(new Certificates
{{
    Domain = GetVariable(""ANGELSQL_DOMAIN2"", """"),
    Certificate = GetVariable(""ANGELSQL_CERTIFICATE2"", """"),
    Password = GetVariable(""ANGELSQL_CERTIFICATE2_PASSWORD"", """")
}});

string main_url = GetVariable(""ANGELSQL_MAINSERVER"", ""http://localhost:{freePort}"");

Dictionary<string, string> parameters = new()
{{
    {{ ""certificate"", """" }},
    //{{ ""certificates_list"", JsonConvert.SerializeObject(certificates) }},
    {{ ""password"", """" }},
    {{ ""urls"", main_url }},
    {{ ""cors"", main_url }},
    {{ ""master_user"", ""db"" }},
    {{ ""master_password"", ""db"" }},
    {{ ""data_directory"", ""{ app.Files_directory }/Data"" }},
    {{ ""request_timeout"", ""4"" }},
    {{ ""accounts_directory"", ""{app.Files_directory}/Data"" }},
    {{ ""smtp"", """" }},
    {{ ""smtp_port"", """" }},
    {{ ""email_address"", """" }},
    {{ ""email_password"", """" }},
    {{ ""python_path"", ""C:/Python313/python.exe"" }},
    {{ ""use_proxy"", ""yes"" }},
    {{ ""proxy_account"", """" }},
    {{ ""proxy_password"", """" }},
    {{ ""public_account"", """" }},
    {{ ""gpt_key"", """" }},
    {{ ""save_activity"", ""false"" }},
    {{ ""use_black_list"", ""false"" }},
    {{ ""use_white_list"", ""false"" }},
    {{ ""service_delay"", ""30000"" }},
}};

string local_url = main_url.Split("","")[0];;

Dictionary<string, string> servers = new Dictionary<string, string>
{{
    {{ ""tokens_url"", $""{{local_url}}/AngelPOST"" }},
    {{ ""skus_url"", $""{{local_url}}/AngelPOST"" }},
    {{ ""sales_url"", $""{{local_url}}/AngelPOST"" }},
    {{ ""configuration_url"", $""{{local_url}}/AngelPOST"" }},
    {{ ""auth_url"", $""{{local_url}}/AngelPOST"" }}
}};

Environment.SetEnvironmentVariable(""ANGELSQL_SERVERS"", JsonConvert.SerializeObject(servers, Formatting.Indented));

return JsonConvert.SerializeObject(parameters);

string GetVariable(string name, string default_value)
{{
    if (Environment.GetEnvironmentVariable(name) == null || Environment.GetEnvironmentVariable(name) == """") return default_value;
    Console.WriteLine($""Variable {{name}} found"");
    return Environment.GetEnvironmentVariable(name);
}}";

        return result;
    }
}
