// GLOBALS
// These lines of code go in each script
#load "Globals.csx"
// END GLOBALS

#r "Newtonsoft.Json.dll"

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;


public class Certificates
{
    public string Domain { get; set; }
    public string Certificate { get; set; }
    public string Password { get; set; }
}

List<Certificates> certificates = [];

certificates.Add(new Certificates
{
    Domain = GetVariable("ANGELSQL_DOMAIN1", ""),
    Certificate = GetVariable("ANGELSQL_CERTIFICATE1", "Certificate/localhost.pfx"),
    Password = GetVariable("ANGELSQL_CERTIFICATE1_PASSWORD", "changeme")
});

certificates.Add(new Certificates
{
    Domain = GetVariable("ANGELSQL_DOMAIN2", ""),
    Certificate = GetVariable("ANGELSQL_CERTIFICATE2", ""),
    Password = GetVariable("ANGELSQL_CERTIFICATE2_PASSWORD", "")
});

string main_url = GetVariable("ANGELSQL_MAINSERVER", "http://localhost:11000, https://localhost:12000");

Dictionary<string, string> parameters = new()
{
    { "certificate", GetVariable( "ANGELSQL_CERTIFICATE", "Certificate/localhost.pfx" ) },
    //{ "certificates_list", JsonConvert.SerializeObject(certificates) },
    { "password", GetVariable( "ANGELSQL_CERTIFICATE_PASSWORD", "changeme" ) },
    { "urls", GetVariable( "ANGELSQL_URLS", main_url ) },
    { "cors", GetVariable( "ANGELSQL_CORS", main_url ) },
    { "master_user", GetVariable( "ANGELSQL_MASTER_USER", "db" ) },
    { "master_password", GetVariable( "ANGELSQL_MASTER_PASSWORD", "db" ) },
    { "data_directory", GetVariable( "ANGELSQL_DATA_DIRECTORY", "" ) },
    { "database", GetVariable( "ANGELSQL_DATABASE", "database1" ) },
    { "request_timeout", GetVariable( "ANGELSQL_REQUEST_TIMEOUT", "4" ) },
    { "accounts_directory", GetVariable( "ANGELSQL_ACCOUNTS_DIRECTORY", "" ) },
    { "smtp", GetVariable( "ANGELSQL_SMPT", "" ) },
    { "smtp_port", GetVariable( "ANGELSQL_SMPT_PORT", "" ) },
    { "email_address", GetVariable( "ANGELSQL_EMAIL_ADDRESS", "" ) },
    { "email_password", GetVariable( "ANGELSQL_EMAIL_PASSWORD", "" ) },
    { "python_path", GetVariable( "ANGELSQL_PYTHON_PATH", "C:/Python313/python.exe" ) },
    { "use_proxy", "yes" },
    { "proxy_account", GetVariable( "ANGELSQL_PROXY_ACCOUNT", "" ) },
    { "proxy_password", GetVariable( "ANGELSQL_PROXY_PASSWORD", "" ) },
    { "public_account", GetVariable( "ANGELSQL_PUBLIC_ACCOUNT", "angelsql" ) },
    { "gpt_key", GetVariable( "ANGELSQL_GPT_KEY", "" ) },
    { "save_activity", "false" },
    { "use_black_list", "false" },
    { "use_white_list", "false" },
    { "service_delay", "30000" },
};

string local_url = main_url.Split(",")[0];

Dictionary<string, string> servers = new Dictionary<string, string>
{
    { "tokens_url", $"{local_url}/AngelPOST" },
    { "skus_url", $"{local_url}/AngelPOST" },
    { "sales_url", $"{local_url}/AngelPOST" },
    { "configuration_url", $"{local_url}/AngelPOST" },
    { "auth_url", $"{local_url}/AngelPOST" }
};

Environment.SetEnvironmentVariable("ANGELSQL_SERVERS", JsonConvert.SerializeObject(servers, Formatting.Indented));

return JsonConvert.SerializeObject(parameters);

string GetVariable(string name, string default_value)
{
    if (Environment.GetEnvironmentVariable(name) == null || Environment.GetEnvironmentVariable(name) == "") return default_value;
    Console.WriteLine($"Variabl+e {name} found");
    return Environment.GetEnvironmentVariable(name);
}