//AngelSQLServer
using AngelDB;
using AngelSQLServer;
using Azure.Core;
using DocumentFormat.OpenXml.Vml;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Win32;
using Newtonsoft.Json;
using Python.Runtime;
using Renci.SshNet;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;

//if is a Windows service, set the current directory to the same as the executable
if (WindowsServiceHelpers.IsWindowsService())
{
    Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
}

//Console.WriteLine(Environment.CurrentDirectory);
string commandLine = string.Join(" ", args);

string config_file = Environment.CurrentDirectory + "/dev/config/AngelSQL.csx";
string app_directory = Environment.CurrentDirectory + "/dev";

if (!string.IsNullOrEmpty(commandLine))
{

    DbLanguage language = new DbLanguage();
    language.SetCommands(AngelSQL.ServerCommands.DbCommands());
    Dictionary<string, string> d = new Dictionary<string, string>();

    d = language.Interpreter(commandLine);

    switch (d.First().Key) 
    {
        case "app_directory":
            app_directory = d["app_directory"];
            config_file = app_directory + "/config/AngelSQL.csx";
            break;
        default:
            LogFile.Log($"Error: Invalid command line argument {d.First().Key}");
            Environment.Exit(0);
            return;
    }

}

string api_file = app_directory + "/config/AngelAPI.csx";
string new_server_account = app_directory + "/config/ServerAccounts.csx";

// Using dot as decimal separator
NumberFormatInfo nfi = new NumberFormatInfo();
nfi.NumberDecimalSeparator = ".";
CultureInfo.CurrentCulture = new CultureInfo("en-US", false);

AngelDB.DB server_db = new AngelDB.DB();
string result = server_db.Prompt($"SCRIPT FILE {config_file}");

Dictionary<string, string> parameters = new Dictionary<string, string>
{
    { "certificate", "" },
    { "password", "" },
    { "urls", "" },
    { "cors", "http://localhost:11000" },
    { "master_user", "db" },
    { "master_password", "db" },
    { "data_directory", "" },
    { "account", "account1" },
    { "account_user", "angelsql" },
    { "account_password", "angelsql123" },
    { "database", "database1" },
    { "request_timeout", "4" },
    { "wwwroot", "" },
    { "scripts_directory", "" },
    { "smtp", "" },
    { "smtp_port", "" },
    { "email_address", "" },
    { "email_password", "" },
    { "chat_script", "" }
};


if (!result.StartsWith("Error:"))
{
    parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
}
else
{
    LogFile.Log(result);
}

parameters["wwwroot"] = app_directory + "/wwwroot";
parameters["scripts_directory"] = app_directory + "/scripts";

if (parameters.ContainsKey("python_path"))
{
    Environment.SetEnvironmentVariable("PYTHON_PATH", parameters["python_path"]);
}

if (parameters.ContainsKey("accounts_directory"))
{
    if (string.IsNullOrEmpty(parameters["accounts_directory"]))
    {
        parameters["accounts_directory"] = Environment.CurrentDirectory + "/data/accounts";
    }
}
else
{
    parameters.Add("accounts_directory", Environment.CurrentDirectory + "/data/accounts");
}

Environment.SetEnvironmentVariable("ANGELSQL_PARAMETERS", JsonConvert.SerializeObject(parameters, Newtonsoft.Json.Formatting.Indented));

//If we need to save the server activity
bool save_activity = false;

if (parameters.ContainsKey("save_activity"))
{
    if (parameters["save_activity"].ToString().Trim().ToLower() == "true")
    {
        save_activity = true;
    }
}

// Create a bulder for the web app
//if is a Windows service, set the current directory to the same as the executable
var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) : default,
    WebRootPath = parameters["wwwroot"],
});

// End Create a builder for the web app
// Server DB
builder.Services.AddSingleton<AngelDB.DB>(server_db);
// Object to save the connections
builder.Services.AddSingleton<ConnectionMappingService>();


if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Host.UseWindowsService();
}

// Create the master database
server_db.Prompt($"DB USER {parameters["master_user"]} PASSWORD {parameters["master_password"]} DATA DIRECTORY {parameters["data_directory"]}", true);
server_db.Prompt($"CREATE ACCOUNT {parameters["account"]} SUPERUSER {parameters["account_user"]} PASSWORD {parameters["account_password"]}", true);
server_db.Prompt($"USE ACCOUNT {parameters["account"]}", true);
server_db.Prompt($"CREATE DATABASE {parameters["database"]}", true);
server_db.Prompt($"USE DATABASE {parameters["database"]}", true);

// Create the accounts table
server_db.Prompt($"CREATE TABLE accounts FIELD LIST db_user, name, email, connection_string, db_password, database, data_directory, account, super_user, super_user_password, active, created", true);
// Create the hub users table
server_db.Prompt($"CREATE TABLE hub_users FIELD LIST account, name, email, phone, password, role, active, last_access, created", true);
// Create the table pins
server_db.Prompt($"CREATE TABLE pins FIELD LIST authorizer TEXT, authorizer_name TEXT, branch_store TEXT, pin_number TEXT, message TEXT, authorizer_message TEXT, pintype TEXT, date TEXT, expirytime TEXT, minutes INTEGER, permissions TEXT, confirmed_date TEXT, user TEXT, app_user TEXT, app_user_name TEXT, status TEXT", true);
// Register the AngelSQL commands
server_db.Prompt($"CREATE TABLE commands FIELD LIST ip, command, log", true);
// White List
server_db.Prompt($"CREATE TABLE whitelist FIELD LIST comment", true);
// Black List
server_db.Prompt($"CREATE TABLE blacklist FIELD LIST comment", true);

Dictionary<string, string> servers = JsonConvert.DeserializeObject<Dictionary<string, string>>(Environment.GetEnvironmentVariable("ANGELSQL_SERVERS"));

foreach (string key in parameters.Keys)
{
    result = server_db.Prompt("VAR db_" + key + " = " + parameters[key]);

    if (result.StartsWith("Error:"))
    {
        LogFile.Log("Error: Setting Parameters vars: " + result.Replace("Error:", ""));
    }
}


foreach (string key in servers.Keys)
{
    result = server_db.Prompt("VAR server_" + key + " = " + servers[key]);

    if (result.StartsWith("Error: Setting server vars: "))
    {
        LogFile.Log("Error: Setting Servers vars: " + result.Replace("Error:", ""));
    }
}


// Initial script
string start_file = "";

if (File.Exists(app_directory + "/config/start.csx"))
{
    if (!parameters.ContainsKey("start_script"))
    {
        start_file = "config/start.csx";
    }
    else
    {
        start_file = parameters["start_script"];
    }

    if (File.Exists(start_file))
    {
        result = server_db.Prompt($"SCRIPT FILE {start_file}", true);

        if (result.StartsWith("Error:"))
        {
            LogFile.Log(result);
        }

    }
}


// Add cors support
if (parameters["cors"] == "true")
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
                       builder =>
                       {
                           builder.AllowAnyOrigin()
                                  .AllowAnyMethod()
                                  .AllowAnyHeader();
                       });
    });
}

// Establece tu tiempo límite deseado aquí (en milisegundos)
if (!parameters.ContainsKey("request_timeout"))
{
    parameters.Add("request_timeout", "10");
}

int request_timeout = 10;
int.TryParse(parameters["request_timeout"], out request_timeout);


builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(request_timeout);

    if (parameters.ContainsKey("certificates_list") && !string.IsNullOrEmpty(parameters["certificates_list"]))
    {
        List<Certificates> certificates = JsonConvert.DeserializeObject<List<Certificates>>(parameters["certificates_list"]);

        if (certificates.Count > 0)
        {
            options.ListenAnyIP(443, listenOptions =>
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                {
                    ServerCertificateSelector = (context, domain) =>
                    {
                        // Selecciona el certificado en función del dominio solicitado
                        var certInfo = certificates.FirstOrDefault(c => c.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));

                        if (certInfo != null)
                        {
                            try
                            {
                                if (!File.Exists(certInfo.Certificate))
                                {
                                    return null;
                                }

                                return new X509Certificate2(certInfo.Certificate, certInfo.Password);
                            }
                            catch (Exception e)
                            {
                                LogFile.Log($"Error loading certificate for domain {certInfo.Domain}: {e.Message}");
                                return null;
                            }
                        }
                        return null;
                    }
                });
            });
        }

        return;
    }

    if (!string.IsNullOrEmpty(parameters["urls"]))
    {
        string[] urls = parameters["urls"].Split(',');
        foreach (string url in urls)
        {
            try
            {
                var uri = new Uri(url);

                bool certificate = false;

                if (!File.Exists(parameters["certificate"]))
                {
                    LogFile.Log($"Error: certificate file {parameters["certificate"]} does not exists");
                }
                else
                {
                    LogFile.Log($"Certificate file {parameters["certificate"]} used");
                    certificate = true;
                }

                if (uri.Host.ToLower().Trim() == "localhost")
                {
                    if (certificate && url.ToLower().Trim().StartsWith("https"))
                    {
                        options.ListenLocalhost(uri.Port, listenOptions =>
                        {
                            listenOptions.UseHttps(parameters["certificate"], parameters["password"]);
                        });
                    }
                    else
                    {
                        options.ListenLocalhost(uri.Port);
                    }
                }
                else
                {
                    if (certificate && url.ToLower().Trim().StartsWith("https"))
                    {
                        try
                        {
                            options.Listen(System.Net.IPAddress.Parse(uri.Host), uri.Port, listenOptions =>
                            {
                                listenOptions.UseHttps(parameters["certificate"], parameters["password"]);
                            });
                        }
                        catch (Exception e)
                        {
                            LogFile.Log($"Error: {e}");
                        }
                    }
                    else
                    {
                        options.Listen(IPAddress.Parse(uri.Host), uri.Port);
                    }
                }

            }
            catch (Exception e)
            {
                LogFile.Log($"Error: {e}");
            }

        }
    }


    // Add pfx certificate

});


// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

var app = builder.Build();


// Add SigNalR services to the container.
app.MapHub<AngelSQLServerHub>("/AngelSQLServerHub");

//Add cors support
app.UseCors(options => options.SetIsOriginAllowed(x => _ = true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());

//Using html static pages
var defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("index.html");

app.UseDefaultFiles(defaultFilesOptions);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(parameters["wwwroot"]),
    RequestPath = ""
});

ConcurrentDictionary<string, AngelSQL.DBConnections> connections = new ConcurrentDictionary<string, AngelSQL.DBConnections>();

string Identification(AngelSQL.Query query)
{
    try
    {
        AngelDB.DB db_local = new AngelDB.DB();
        db_local.NewDatabases = false;

        if (string.IsNullOrEmpty(query.data_directory))
        {
            query.data_directory = parameters["data_directory"];
        }

        string result = db_local.Prompt($"DB USER {query.User} PASSWORD {query.password} ACCOUNT {query.account} DATABASE {query.database} DATA DIRECTORY {query.data_directory}");

        AngelSQL.Responce responce = new AngelSQL.Responce();

        if (result.StartsWith("Error:"))
        {
            responce.token = "";
            responce.result = result;
            responce.type = "ERROR";
            return JsonConvert.SerializeObject(responce);
        }

        AngelSQL.DBConnections connection = new AngelSQL.DBConnections();
        connection.CreationDate = DateTime.Now;
        connection.expiration_days = 30;
        connection.date_of_last_access = DateTime.Now;
        connection.User = query.User;
        connection.db = db_local;

        string token = Guid.NewGuid().ToString();

        connections.TryAdd(token, connection);

        responce.token = token;
        responce.result = result;
        responce.type = "SUCCESS";
        return JsonConvert.SerializeObject(responce);

    }
    catch (Exception e)
    {
        return $"Error: Identification() {e}";
    }

}



string QueryResponce(AngelSQL.Query query)
{
    try
    {
        AngelSQL.Responce responce = new AngelSQL.Responce();

        if (!connections.ContainsKey(query.token))
        {
            responce.token = "";
            responce.result = $"Error: You need to identify yourself first. {query.token}";
            responce.type = "ERROR";
            return JsonConvert.SerializeObject(responce);
        }

        responce.token = query.token;
        connections[query.token].date_of_last_access = DateTime.Now;

        responce.result = connections[query.token].db.Prompt(query.command);

        if (responce.result.StartsWith("Error:"))
        {
            responce.type = "ERROR";
        }
        else
        {
            responce.type = "SUCCESS";
        }

        return JsonConvert.SerializeObject(responce);

    }
    catch (Exception e)
    {
        return $"Error: QueryResponce() {e}";
    }
}

app.MapGet("/AngelAPI", (string data, HttpContext context) =>
{
    string result = ProcessGet(data, context);

    if (result.StartsWith("<html>"))
    {
        // Retorna el resultado con el Content-Type de HTML
        return Results.Content(result.Substring(6), "text/html");
    }

    // Si no empieza con <html>, retorna el resultado como texto plano (o ajusta según tus necesidades)
    return Results.Text(result);
});

app.MapGet("/Get", (string data, HttpContext context) =>
{
    string result = ProcessGet(data, context);

    if (result.StartsWith("<html>"))
    {
        // Retorna el resultado con el Content-Type de HTML
        return Results.Content(result.Substring(6), "text/html");
    }

    // Si no empieza con <html>, retorna el resultado como texto plano (o ajusta según tus necesidades)
    return Results.Text(result);
});


// End of mapping the AngelSQL API




string ProcessGet(string data, HttpContext context)
{
    try
    {

        if (save_activity)
        {
            var log = new
            {
                ip = context.Connection.RemoteIpAddress.ToString(),
                command = "AngelAPI",
                log = data
            };

            server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
        }

        AngelDB.DB angel_api_db = new AngelDB.DB();
        angel_api_db.Prompt($"DB USER {parameters["master_user"]} PASSWORD {parameters["master_password"]} DATA DIRECTORY {parameters["data_directory"]}", true);
        angel_api_db.Prompt($"CREATE ACCOUNT {parameters["account"]} SUPERUSER {parameters["account_user"]} PASSWORD {parameters["account_password"]}", true);
        angel_api_db.Prompt($"USE ACCOUNT {parameters["account"]}", true);
        angel_api_db.Prompt($"CREATE DATABASE {parameters["database"]}", true);
        angel_api_db.Prompt($"USE DATABASE {parameters["database"]}", true);

        //AngelSQL.ApiClass api = JsonConvert.DeserializeObject<AngelSQL.ApiClass>(data);
        string ApiCommand;

        if (parameters.ContainsKey("angel_api"))
        {
            ApiCommand = parameters["angel_api"];
        }
        else
        {
            ApiCommand = api_file;
        }

        ApiCommand = ApiCommand.Trim();

        if (ApiCommand.EndsWith(".csx"))
        {
            //result = main_db.Prompt($"SCRIPT FILE {scripts_directory}/{api.api}.csx MESSAGE " + JsonConvert.SerializeObject(api.message), true, main_db);
            result = server_db.Prompt($"SCRIPT FILE {ApiCommand} MESSAGE " + context.Request.QueryString, false, angel_api_db);

            if (save_activity)
            {
                var log = new
                {
                    ip = context.Connection.RemoteIpAddress.ToString(),
                    command = "AngelAPI",
                    log = result
                };

                server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
            }

            return result;

        }

        if (ApiCommand.EndsWith(".py"))
        {
            result = server_db.Prompt($"PYTHON FILE {parameters["scripts_directory"]}/{ApiCommand} MESSAGE " + data, false, angel_api_db);

            if (save_activity)
            {
                var log = new
                {
                    ip = context.Connection.RemoteIpAddress.ToString(),
                    command = "AngelAPI",
                    log = result
                };

                server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
            }

            return result;
        }

        result = "Error: Invalid API file.";

        if (save_activity)
        {
            var log = new
            {
                ip = context.Connection.RemoteIpAddress.ToString(),
                command = "AngelAPI",
                log = result
            };

            server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
        }

        return result;

    }
    catch (Exception e)
    {
        return "Error: " + e.Message;
    }

}


Dictionary<string, AngelDB.DB> post_databases = new Dictionary<string, AngelDB.DB>();
string session_guid = Guid.NewGuid().ToString();

// End of mapping AngelPOST communications protocol



app.MapPost("/AngelPOST", async delegate (HttpContext context)
{
    using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
    {
        string jsonstring = await reader.ReadToEndAsync();
        return ProcessAngelData(jsonstring, context.Connection.RemoteIpAddress.ToString());
    }
});

ConcurrentDictionary<string, AngelDB.TaskState> taskResults = new();

app.MapPost("/AsyncPOST", async delegate (HttpContext context)
{
    using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
    {
        string jsonstring = await reader.ReadToEndAsync();
        AngelApiOperation api = JsonConvert.DeserializeObject<AngelApiOperation>(jsonstring);

        if (api.TaskGuid == null || api.TaskGuid.Trim() == "")
            return "Error: TaskGuid not provided.";

        if (api.GetResult)
        {
            if (taskResults.TryGetValue(api.TaskGuid, out var taskState))
            {
                if (taskState.Finished)
                {
                    string result = taskState.Result;
                    taskResults.TryRemove(api.TaskGuid, out _);
                    return result;
                }

                AngelSQL.Responce responce_delayed = new AngelSQL.Responce
                {
                    type = "DELAYED",
                    result = taskState.Message
                };
                return server_db.GetJson(responce_delayed);
            }

            // No se encontró la tarea
            return server_db.GetJson(new AngelSQL.Responce
            {
                type = "DELAYED",
                result = "Ok."
            });
        }

        // Registro inicial de la tarea
        taskResults.TryAdd(api.TaskGuid, new TaskState { Finished = false, Created = DateTime.UtcNow });

        // Capturamos la IP ANTES del Task
        string remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _ = Task.Run(() =>
        {
            try
            {
                string result = ProcessAngelData(jsonstring, remoteIp, api, taskResults[api.TaskGuid]);
                taskResults[api.TaskGuid].Finished = true;
                taskResults[api.TaskGuid].Result = result;
                taskResults[api.TaskGuid].Message = "Ok.";
            }
            catch (Exception e)
            {
                taskResults[api.TaskGuid].Finished = true;
                taskResults[api.TaskGuid].Result = "Error: " + e.ToString();
                taskResults[api.TaskGuid].Message = "Error running task.";
            }
        });

        return server_db.GetJson(new AngelSQL.Responce
        {
            type = "SUCCESS",
            result = "Ok."
        });
    }
});



app.MapPost("/POST", async delegate (HttpContext context)
{
    using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
    {
        string jsonstring = await reader.ReadToEndAsync();
        return ProcessAngelData(jsonstring, context.Connection.RemoteIpAddress.ToString());
    }
});





app.MapPost("/AngelUpload", async (HttpContext httpContext) =>
{

    try
    {

        httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 100_000_000;
        var form = await httpContext.Request.ReadFormAsync();

        var file = form.Files.FirstOrDefault();

        // Obtener el primer valor del campo json del formulario
        string jSonString = form["jSonString"].FirstOrDefault();

        if (string.IsNullOrEmpty(jSonString))
        {
            return "Error: No JSON string";
        }

        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();

        AngelApiOperation api = JsonConvert.DeserializeObject<AngelApiOperation>(jSonString);
        api.message.File = file.FileName;
        api.message.FileSize = file.Length;
        api.message.FileType = file.ContentType;

        string result = ProcessAngelData(JsonConvert.SerializeObject(api), clientIp);

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        AngelResponce angelResponce = JsonConvert.DeserializeObject<AngelResponce>(result);

        if (angelResponce.result.StartsWith("Error:"))
        {
            return angelResponce.result;
        }

        FileUploadInfo file_info = JsonConvert.DeserializeObject<FileUploadInfo>(angelResponce.result);

        if (!file_info.ProceedToUpload)
        {
            return file_info.ErrorMessage;
        }

        var file_name = parameters["wwwroot"] + "/" + file_info.FileDirectory + "/" + file_info.FileName;

        if (Directory.Exists(parameters["wwwroot"] + "/" + file_info.FileDirectory) == false)
        {
            Directory.CreateDirectory(parameters["wwwroot"] + "/" + file_info.FileDirectory);
        }

        file_info.Url = file_info.FileDirectory + "/" + file_info.FileName;

        using var stream = File.OpenWrite(file_name);
        await file.CopyToAsync(stream);
        return server_db.GetJson(file_info);

    }
    catch (Exception e)
    {
        return $"Error: {e}";
    }

});



string ProcessAngelData(string jsonstring, string RemoteIp, AngelApiOperation api = null, AngelDB.TaskState taskstate = null)
{

    try
    {

        if (api is null)
        {
            api = JsonConvert.DeserializeObject<AngelApiOperation>(jsonstring);
        }

        if (save_activity)
        {
            var log = new
            {
                ip = RemoteIp,
                command = "POST",
                log = jsonstring
            };

            server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
        }

        AngelDB.DB db;

        if (api.account is null)
        {
            api.account = "default: " + session_guid;
        }

        if (string.IsNullOrEmpty(api.account.ToString()))
        {
            api.account = "default: " + session_guid;
        }

        if (!post_databases.ContainsKey(api.account.ToString()))
        {
            db = new AngelDB.DB();

            if (api.account.ToString().StartsWith("default: "))
            {
                if (!parameters.ContainsKey("connection_string"))
                {
                    db.Prompt($"DB USER {parameters["master_user"]} PASSWORD {parameters["master_password"]} DATA DIRECTORY {parameters["data_directory"]}", true);
                    db.Prompt($"CREATE ACCOUNT {parameters["account"]} SUPERUSER {parameters["account_user"]} PASSWORD {parameters["account_password"]}", true);
                    db.Prompt($"USE ACCOUNT {parameters["account"]}", true);
                    db.Prompt($"CREATE DATABASE {parameters["database"]}", true);
                    db.Prompt($"USE DATABASE {parameters["database"]}", true);
                }
                else
                {
                    db.Prompt($"{parameters["connection_string"]}", true);

                    if (parameters["connection_string"].StartsWith("ANGEL"))
                    {
                        db.Prompt($"ALWAYS USE ANGELSQL", true);
                    }

                }

                api.db_user = parameters["master_user"];
                api.db_password = parameters["master_password"];
                db.Prompt($"VAR db_user = '{parameters["master_user"]}'", true);
                db.Prompt($"VAR db_password = '{parameters["master_password"]}'", true);
                db.Prompt($"VAR db_account = ''", true);

            }
            else
            {
                string result = server_db.Prompt($"SELECT * FROM accounts WHERE id = '{api.account.ToString().Trim().ToLower()}'", true);

                if (result.StartsWith("Error:"))
                {

                    if (save_activity)
                    {
                        var log = new
                        {
                            ip = RemoteIp,
                            command = "POST",
                            log = result
                        };

                        server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
                    }

                    return result;
                }

                if (result == "[]")
                {

                    if (save_activity)
                    {
                        var log = new
                        {
                            ip = RemoteIp,
                            command = "POST",
                            log = result
                        };

                        server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
                    }

                    return $"Error: Account {api.account} not found.";
                }

                System.Data.DataTable dt = JsonConvert.DeserializeObject<System.Data.DataTable>(result);

                if (dt.Rows[0]["active"].ToString() == "false")
                {
                    if (save_activity)
                    {
                        var log = new
                        {
                            ip = RemoteIp,
                            command = "POST",
                            log = result
                        };

                        server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
                    }

                    return $"The account is not active: {api.account}";
                }

                string connection_string = dt.Rows[0]["connection_string"].ToString();
                connection_string = connection_string.Replace("main_directory", Environment.CurrentDirectory);
                string db_user = dt.Rows[0]["db_user"].ToString();
                string db_password = dt.Rows[0]["db_password"].ToString();
                string db_database = dt.Rows[0]["database"].ToString();
                string data_directory = dt.Rows[0]["data_directory"].ToString();
                string account = dt.Rows[0]["account"].ToString();
                string super_user = dt.Rows[0]["super_user"].ToString();
                string super_user_password = dt.Rows[0]["super_user_password"].ToString();

                if (!string.IsNullOrEmpty(connection_string))
                {
                    result = db.Prompt(connection_string);

                    if (connection_string.StartsWith("ANGEL"))
                    {
                        result = db.Prompt("ALWAYS USE ANGELSQL");
                    }

                    if (result.StartsWith("Error: AngelPOST"))
                    {
                        if (save_activity)
                        {
                            var log = new
                            {
                                ip = RemoteIp,
                                command = "POST",
                                log = result
                            };

                            server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
                        }

                        return result;
                    }

                    db.Prompt($"CREATE ACCOUNT {account} SUPERUSER {super_user} PASSWORD {super_user_password}", true);
                    db.Prompt($"USE ACCOUNT {account}", true);
                    db.Prompt($"CREATE DATABASE {db_database}", true);
                    db.Prompt($"USE DATABASE {db_database}", true);

                }
                else
                {
                    result = db.Prompt($"DB USER db PASSWORD db DATA DIRECTORY {data_directory}");

                    if (result.StartsWith("Error:"))
                    {
                        result = db.Prompt($"DB USER {db_user} PASSWORD {db_password} DATA DIRECTORY {data_directory}");
                    }
                    else
                    {
                        result = db.Prompt($"CHANGE MASTER TO USER {db_user} PASSWORD {db_password}");
                    }

                    if (result.StartsWith("Error:"))
                    {

                        if (save_activity)
                        {
                            var log = new
                            {
                                ip = RemoteIp,
                                command = "POST",
                                log = result
                            };

                            server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
                        }


                        return result;
                    }

                    db.Prompt($"CREATE ACCOUNT {account} SUPERUSER {super_user} PASSWORD {super_user_password}", true);
                    db.Prompt($"USE ACCOUNT {account}", true);
                    db.Prompt($"CREATE DATABASE {db_database}", true);
                    db.Prompt($"USE DATABASE {db_database}", true);

                }

                db.Prompt($"VAR db_user = '{db_user}'", true);
                db.Prompt($"VAR db_password = '{db_password}'", true);
                db.Prompt($"VAR db_account = '{api.account}'", true);

            }

            post_databases.TryAdd(api.account.ToString(), db);

        }
        else
        {
            db = post_databases[api.account.ToString()];
        }

        if (api.language is null)
        {
            api.language = "C#";
        }

        if (string.IsNullOrEmpty(api.language.ToString()))
        {
            result = db.Prompt($"SCRIPT FILE {parameters["scripts_directory"]}/{api.api}.csx MESSAGE " + JsonConvert.SerializeObject(api.message), true, db);
        }
        else
        {
            switch (api.language.ToString())
            {
                case "C#":
                    result = server_db.Prompt($"SCRIPT FILE {parameters["scripts_directory"]}/{api.api}.csx MESSAGE " + JsonConvert.SerializeObject(api.message), true, db, null, taskstate);
                    break;
                case "CSHARP":
                    result = server_db.Prompt($"SCRIPT FILE {parameters["scripts_directory"]}/{api.api}.csx MESSAGE " + JsonConvert.SerializeObject(api.message), true, db, null, taskstate);
                    break;
                case "null":
                    result = server_db.Prompt($"SCRIPT FILE {parameters["scripts_directory"]}/{api.api}.csx MESSAGE " + JsonConvert.SerializeObject(api.message), true, db, null, taskstate);
                    break;
                case "PYTHON":
                    result = server_db.Prompt($"PYTHON FILE {parameters["scripts_directory"]}/{api.api}.py MESSAGE " + JsonConvert.SerializeObject(api.message), true, db, null, taskstate);
                    break;
                default:
                    break;
            }
        }

        AngelSQL.Responce responce = new AngelSQL.Responce
        {
            type = "SUCCESS",
            result = result
        };

        if (save_activity)
        {
            var log = new
            {
                ip = RemoteIp,
                command = "POST",
                log = result
            };

            server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
        }

        return JsonConvert.SerializeObject(responce);

    }
    catch (global::System.Exception e)
    {

        if (save_activity)
        {
            var log = new
            {
                ip = RemoteIp,
                command = "POST",
                log = $"Error: {e}"
            };

            server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
        }

        AngelSQL.Responce responce = new AngelSQL.Responce();
        responce.type = "ERROR";
        responce.result = $"Error: {e}";
        return JsonConvert.SerializeObject(responce);
    }

}


//Mapping AngelSQL communications protocol
app.MapPost("/AngelSQL", async delegate (HttpContext context)
{

    string result = "";

    using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
    {
        try
        {
            string jsonstring = await reader.ReadToEndAsync();
            AngelSQL.Query query = JsonConvert.DeserializeObject<AngelSQL.Query>(jsonstring);

            if (save_activity)
            {
                var log = new
                {
                    ip = context.Connection.RemoteIpAddress.ToString(),
                    command = "AngelSQL",
                    log = jsonstring
                };

                server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
            }

            switch (query.type.Trim().ToLower())
            {
                case "identification":

                    result = Identification(query);
                    break;

                case "query":

                    result = QueryResponce(query);
                    break;

                case "disconnect":

                    if (connections.ContainsKey(query.token))
                    {
                        connections.TryRemove(query.token, out _);
                    }

                    AngelSQL.Responce responce = new AngelSQL.Responce();
                    responce.token = "";
                    responce.result = "Disconnected";
                    responce.type = "SUCCESS";
                    result = JsonConvert.SerializeObject(responce);
                    break;

                case "server_command":

                    result = QueryResponce(query);
                    break;

                default:
                    result = "Unknown command";
                    break;
            }

            if (save_activity)
            {
                var log = new
                {
                    ip = context.Connection.RemoteIpAddress.ToString(),
                    command = "AngelSQL",
                    log = result
                };

                server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
            }

            return result;
        }
        catch (global::System.Exception e)
        {

            if (save_activity)
            {
                var log = new
                {
                    ip = context.Connection.RemoteIpAddress.ToString(),
                    command = "AngelSQL",
                    log = $"Error: {e}"
                };

                server_db.Prompt($"INSERT INTO commands PARTITION KEY {DateTime.Now:yyyy-MM-dd} VALUES " + JsonConvert.SerializeObject(log));
            }

            AngelSQL.Responce responce = new AngelSQL.Responce();
            responce.type = "ERROR";
            responce.result = $"Error: {e}";
            return JsonConvert.SerializeObject(responce);
        }
    }
});

//End of mapping AngelSQL communications protocol
// Adding urls to the server
if (!string.IsNullOrEmpty(parameters["urls"]))
{
    string[] urls = parameters["urls"].Split(",");

    foreach (string url in urls)
    {
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                app.Urls.Add(url);
            }
            catch (Exception e)
            {
                LogFile.Log($"Error: Add urls {e}");
            }
        }
    }
}



// Remove garbage
int garbage_cicle = 0;

_ = Task.Run(() =>
{
    try
    {
        if (!parameters.ContainsKey("connection_timeout"))
        {
            parameters.Add("connection_timeout", "12");
        }


        while (true)
        {

            GC.Collect();

            ++garbage_cicle;

            if (garbage_cicle == 120)
            {
                foreach (string item in connections.Keys)
                {
                    if ((DateTime.Now - connections[item].date_of_last_access).Hours > decimal.Parse(parameters["connection_timeout"]))
                    {
                        connections.TryRemove(item, out _);
                    }
                }

                garbage_cicle = 0;

            }

            Thread.Sleep(5000);
        }

    }
    catch (Exception e)
    {
        LogFile.Log("Error: " + e);
    }

});


_ = Task.Run(() =>
{

    try
    {
        if (!parameters.ContainsKey("service_delay"))
        {
            parameters.Add("service_delay", "300000");
        }

        while (true)
        {
            int delay = 300000;

            //Console.WriteLine($"SCRIPT FILE {parameters["service_command"]}");

            string local_result = server_db.Prompt($"SCRIPT FILE {app_directory + "/config/tasks.csx"}", false, server_db);

            if (local_result.StartsWith("Error:"))
            {
                LogFile.Log(local_result);
            }

            if (int.TryParse(parameters["service_delay"], out delay))
            {
                Thread.Sleep(delay);
            }
            else
            {
                Thread.Sleep(300000);
            }

        }
    }
    catch (Exception e)
    {
        LogFile.Log("Error: " + e);
    }

});

// End of running Tasks script


_ = Task.Run(() =>
{
    while (true)
    {
        try
        {
            var expiredKeys = taskResults
                .Where(kvp => DateTime.UtcNow - kvp.Value.Created > TimeSpan.FromHours(3))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                taskResults.TryRemove(key, out _);
            }

            // Verifica cada 10 minutos
            Thread.Sleep(TimeSpan.FromMinutes(10));
        }
        catch (Exception ex)
        {
            LogFile.Log("Error in taskResults cleaner: " + ex.Message);
        }
    }
});



// Our Header
void PutHeader()
{
    //Console.Clear();
    AngelDB.Monitor.ShowLine("===================================================================", ConsoleColor.Magenta);
    AngelDB.Monitor.ShowLine(" =>  DataBase software, powerful and simple at the same time", ConsoleColor.Magenta);
    AngelDB.Monitor.ShowLine(" =>  We explain it to you in 20 words or fewer:", ConsoleColor.Magenta);
    AngelDB.Monitor.ShowLine(" =>  AngelSQL", ConsoleColor.Green);
    AngelDB.Monitor.ShowLine("===================================================================", ConsoleColor.Magenta);

}


// Agrega el middleware personalizado para capturar solicitudes a '/swagger/index.html'.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/swagger"))
    {
        // Responde con el código de estado HTTP 404 (No encontrado)
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        // Opcional: Redirige a otra página, por ejemplo, la página principal de tu aplicación
        context.Response.Redirect("/");
    }
    else
    {
        await next();
    }
});


//Add White And Black List
app.Use(async (context, next) =>
{
    var clientIp = context.Connection.RemoteIpAddress;

    //Check For White List
    string result;

    if (parameters.ContainsKey("use_white_list"))
    {
        if (parameters["use_white_list"] == "true")
        {
            result = server_db.Prompt($"SELECT id FROM whitelist WHERE id = '{clientIp}'");

            if (result.StartsWith("Error:"))
            {
                LogFile.Log(result);
                await next.Invoke();
                return;
            }

            if (result != "[]")
            {
                await next.Invoke();
                return;
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Ip not allowed");
                return;
            }
        }
    }

    if (parameters.ContainsKey("use_black_list"))
    {
        if (parameters["use_black_list"] == "true")
        {
            result = server_db.Prompt($"SELECT id FROM blacklist WHERE id = '{clientIp}'");

            if (result.StartsWith("Error:"))
            {
                LogFile.Log(result);
                await next.Invoke();
                return;
            }

            if (result != "[]")
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Ip not allowed");
                return;
            }
        }
    }

    await next.Invoke();

});




void ActivateProxy()
{
    if (parameters.ContainsKey("use_proxy"))
    {
        if (parameters["use_proxy"] == "yes")
        {
            try
            {

                result = server_db.Prompt($"SCRIPT FILE {app_directory}/config/GetProxy.csx");

                if (result.StartsWith("Error:"))
                {
                    LogFile.Log("Error: Use Proxy " + result);
                }
                else
                {
                    result = AngelDBTools.Crypto.DecryptString(result, "F+wWcGbezWl12345");

                    if (result.StartsWith("Error:"))
                    {
                        LogFile.Log("Error: Use Proxy " + result);
                        return;
                    }

                    AngelSQLsass angelSQLsass = JsonConvert.DeserializeObject<AngelSQLsass>(result);

                    var sshClient = new SshClient(angelSQLsass.Domain, angelSQLsass.Port, angelSQLsass.Ssh_user, angelSQLsass.Ssh_password);
                    sshClient.KeepAliveInterval = TimeSpan.FromSeconds(60);
                    sshClient.Connect();

                    // Configura el túnel SSH reverso para redirigir tráfico desde el servidor público al servidor local
                    var portForwarded = new ForwardedPortRemote("127.0.0.1", (uint)angelSQLsass.Ssh_port, angelSQLsass.Host, (uint)angelSQLsass.Host_Port);
                    sshClient.AddForwardedPort(portForwarded);
                    portForwarded.Start();

                    StringBuilder sb = new StringBuilder();

                    string jsProxyNetworkFilePath = System.IO.Path.Combine(app_directory + "/wwwroot", "Proxy_network.js");
                    string jsProxyNetworkContent = File.ReadAllText(jsProxyNetworkFilePath);

                    sb.AppendLine($"var clientKey = '-{angelSQLsass.Account}';");
                    sb.AppendLine();
                    sb.AppendLine("if ( isPrivateNetwork() )");
                    sb.AppendLine("{");
                    sb.AppendLine($"    var clientKey = '';");
                    sb.AppendLine("}");

                    string jsFilePath = System.IO.Path.Combine(app_directory + "/wwwroot", "proxy.js");

                    if (File.Exists(jsFilePath))
                    {
                        File.Delete(jsFilePath);
                    }

                    File.WriteAllText(jsFilePath, jsProxyNetworkContent + "\r\n" + sb.ToString());

                    server_db.Prompt("VAR db_public_url = '" + angelSQLsass.Main_domain + "'");

                    LogFile.Log("Reverse SSH tunnel active and redirecting traffic");
                    LogFile.Log("Public url: " + angelSQLsass.Main_domain);
                }

            }
            catch (Exception e)
            {
                LogFile.Log("Error: Use Proxy " + e);
            }
        }
        else
        {
            string jsFilePath = System.IO.Path.Combine(app_directory + "/wwwroot", "proxy.js");
            string jsContent = $"const clientKey = '';";
            File.WriteAllTextAsync(jsFilePath, jsContent);
        }
    }

}


if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    SystemEvents.PowerModeChanged += OnPowerChange;
}


void OnPowerChange(object s, PowerModeChangedEventArgs e)
{
    switch (e.Mode)
    {
        case PowerModes.Resume:
            //LogFile.Log("PowerModeChanged: Resume");

            ActivateProxy();

            break;
        case PowerModes.StatusChange:
            //LogFile.Log("PowerModeChanged: StatusChange");
            break;
        case PowerModes.Suspend:
            //LogFile.Log("PowerModeChanged: Suspend");
            break;
    }
}


//Save Account for remote orders
File.WriteAllText(parameters["wwwroot"] + "/js/account.js", $"var angelsql_account = '{parameters["public_account"]}'");

server_db.Prompt("VAR db_public_url = ''");

// Activamos el proxy
ActivateProxy();

// Running the server, as a service or as a console application
if (WindowsServiceHelpers.IsWindowsService())
{
    app.Run();
}
else
{
    //Mapping the AngelPOS API (POST)
    AngelDB.DB prompt_db = new AngelDB.DB();
    prompt_db.Prompt($"DB USER {parameters["master_user"]} PASSWORD {parameters["master_password"]} DATA DIRECTORY {parameters["data_directory"]}", true);
    prompt_db.Prompt($"CREATE ACCOUNT {parameters["account"]} SUPERUSER {parameters["account_user"]} PASSWORD {parameters["account_password"]}", true);
    prompt_db.Prompt($"USE ACCOUNT {parameters["account"]}", true);
    prompt_db.Prompt($"CREATE DATABASE {parameters["database"]}", true);
    prompt_db.Prompt($"USE DATABASE {parameters["database"]}", true);

    app.RunAsync();
    PutHeader();

    DbLanguage language = new DbLanguage();
    language.SetCommands(AngelSQL.AngelSQLCommands.DbCommands());
    Dictionary<string, string> d = new Dictionary<string, string>();

    for (; ; )
    {
        // All operations are done here
        string line;
        string prompt = "";
        string prompt_result = "";

        if (server_db.always_use_AngelSQL == true)
        {
            prompt = prompt_db.angel_url + ">" + prompt_db.angel_user;
        }
        else
        {
            if (server_db.IsLogged)
            {
                prompt = new DirectoryInfo(prompt_db.BaseDirectory + "/").Name + ">" + prompt_db.account + ">" + prompt_db.database + ">" + prompt_db.user;
            }
        }

        line = AngelDB.Monitor.Prompt(prompt + " $> ");

        if (string.IsNullOrEmpty(line))
        {
            continue;
        }

        d = language.Interpreter(line);

        if (d != null)
        {
            switch (d.First().Key)
            {
                case "save_activity_on":
                    save_activity = true;
                    prompt_result = "Ok. Save Activity ON";
                    break;

                case "save_activity_off":
                    save_activity = false;
                    prompt_result = "Ok. Save Activity OFF";
                    break;

                case "white_list_on":

                    if (parameters.ContainsKey("use_white_list"))
                    {
                        parameters["use_white_list"] = "true";
                    }
                    else
                    {
                        parameters.Add("use_white_list", "true");
                    }

                    prompt_result = "Ok. White List ON";

                    break;

                case "white_list_off":

                    if (parameters.ContainsKey("use_white_list"))
                    {
                        parameters["use_white_list"] = "false";
                    }
                    else
                    {
                        parameters.Add("use_white_list", "false");
                    }

                    prompt_result = "Ok. White List OFF";
                    break;

                case "black_list_on":

                    if (parameters.ContainsKey("use_black_list"))
                    {
                        parameters["use_black_list"] = "true";
                    }
                    else
                    {
                        parameters.Add("use_black_list", "true");
                    }

                    prompt_result = "Ok. Black List ON";
                    break;

                case "black_list_off":

                    if (parameters.ContainsKey("use_black_list"))
                    {
                        parameters["use_black_list"] = "false";
                    }
                    else
                    {
                        parameters.Add("use_black_list", "false");
                    }

                    prompt_result = "Ok. Black List OFF";
                    break;

                case "add_to_white_list":

                    if (!IsValidIPAddress(d["add_to_white_list"]))
                    {
                        prompt_result = "Error: Invalid IP Address";
                        break;
                    }

                    var w = new
                    {
                        id = d["add_to_white_list"]
                    };

                    prompt_result = prompt_db.UpsertInto("whitelist", w);
                    break;

                case "remove_from_white_list":


                    prompt_result = prompt_db.Prompt("SELECT id FROM whitelist WHERE id = '" + d["remove_from_white_list"] + "'");

                    if (prompt_result == "[]")
                    {
                        prompt_result = "Error: IP Address not found";
                        break;
                    }

                    prompt_result = prompt_db.Prompt($"DELETE FROM whitelist PARTITION KEY main WHERE id = '{d["remove_from_white_list"]}'");
                    break;

                case "add_to_black_list":

                    if (!IsValidIPAddress(d["add_to_black_list"]))
                    {
                        prompt_result = "Error: Invalid IP Address";
                        break;
                    }

                    var b = new
                    {
                        id = d["add_to_black_list"]
                    };

                    prompt_result = prompt_db.UpsertInto("blacklist", b);
                    break;

                case "remove_from_black_list":

                    prompt_result = prompt_db.Prompt("SELECT id FROM blacklist WHERE id = '" + d["remove_from_black_list"] + "'");

                    if (prompt_result == "[]")
                    {
                        prompt_result = "Error: IP Address not found";
                        break;
                    }

                    prompt_result = prompt_db.Prompt($"DELETE FROM blacklist PARTITION KEY main WHERE id = '{d["remove_from_black_list"]}'");
                    break;

                case "create_server_account":

                    prompt_result = server_db.Prompt($"SCRIPT FILE {new_server_account} MESSAGE {JsonConvert.SerializeObject(d)}", true);
                    continue;

                default:

                    prompt_result = "";
                    break;
            }
        }

        if (prompt_result != "")
        {
            if (prompt_result.StartsWith("Error:"))
            {
                AngelDB.Monitor.ShowError(prompt_result);
            }
            else
            {
                AngelDB.Monitor.Show(prompt_result);
            }
            continue;
        }

        if (line.Trim().ToUpper() == "QUIT")
        {
            try
            {
                app.StopAsync().GetAwaiter();
                PythonEngine.Shutdown();
            }
            catch
            {
            }

            Environment.Exit(0);
            return;
        }

        if (line.Trim().ToUpper() == "PROXY")
        {
            try
            {

                if (parameters.ContainsKey("use_proxy"))
                {
                    Console.WriteLine("Activate Proxy...");
                    if (parameters["use_proxy"] == "yes")
                    {
                        ActivateProxy();
                    }
                    else
                    {
                        Console.WriteLine("Proxy is not configured");
                    }
                }

            }
            catch
            {
            }

            continue;
        }

        if (line.Trim().ToUpper() == "CLEAR")
        {
            Console.Clear();
            PutHeader();
            continue;
        }

        if (line.Trim().ToUpper() == "LISTEN ON")
        {
            try
            {
                foreach (string item in app.Urls)
                {
                    Console.WriteLine(item);
                }
            }
            catch (Exception e)
            {
                AngelDB.Monitor.ShowError($"Error: {e.ToString()}");
            }
            continue;
        }

        string prompt_result_db = "";

        if (line.Trim().ToUpper().StartsWith("SERVER DB"))
        {
            prompt_result_db = server_db.Prompt("BATCH " + line.Replace("SERVER DB", "") + " SHOW IN CONSOLE");
        }
        else
        {
            prompt_result_db = prompt_db.Prompt("BATCH " + line + " SHOW IN CONSOLE");
        }

        if (prompt_result_db.StartsWith("Error:"))
        {
            AngelDB.Monitor.ShowError(prompt_result_db);
        }
        else
        {
            AngelDB.Monitor.Show(prompt_result_db);
        }

    }

    static bool IsValidIPAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }

}



