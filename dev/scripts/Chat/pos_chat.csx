// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

// Process to send messages to user
// Daniel() Oliver Rojas
// 2024-08-25

#load "translations.csx"
#load "..\AngelComm\AngelComm.csx"
#load "..\POSApi\Chat.csx"

#r "System.Runtime"
#r "System.Private.CoreLib"
#r "netstandard"

// This script works as an API so that different applications
// can affect sales, purchases, inventory entries and exits,
// physical inventories, accounts receivable and payable.

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Linq;
using System.IO;

private AngelApiOperation api = JsonConvert.DeserializeObject<AngelApiOperation>(message);
api.db = db;
api.server_db = server_db;

//Server parameters
private Dictionary<string, string> parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Environment.GetEnvironmentVariable("ANGELSQL_PARAMETERS"));
private Translations translation = new();
translation.SpanishValues();

// This is the main function that will be called by the API
return api.OperationType switch
{
    "CloseChat" => CloseChat(),
    "GetChats" => GetChats(),
    "GetChat" => GetChat(),
    "GetContactChatReply" => GetContactChatReply(),
    "GetKioskoChatReply" => GetKioskoChatReply(),
    "InsertContactChat" => InsertContactChat(api, translation),
    "InsertKioskoUserChat" => InsertKioskoUserChat(),
    _ => $"Error: No service found {api.OperationType}",
};


string CloseChat()
{

    string result = IsTokenValid(api, "ANY");
    if (result.StartsWith("Error:"))
    {
        return result;
    }
    if (api.DataMessage == null)
    {
        return "Error: Missing required DataMessage in CloseChat (12)";
    }
    if (api.DataMessage.Chat_id == null)
    {
        return "Error: Missing required Chat_id in DataMessage (13)";
    }
    string chat_id = api.DataMessage.Chat_id.ToString();

    string Query = $"SELECT * FROM ContactChat WHERE Id = '{chat_id}'";

    result = db.Prompt(Query);

    if (result.StartsWith("Error:"))
    {
        return result;
    }
    if (result == "[]")
    {
        return "Error: Chat not found";
    }

    result = db.Prompt($"GET PARTITIONS FROM TABLE contactchat");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    DataTable partitions = JsonConvert.DeserializeObject<DataTable>(result);

    foreach (DataRow partition in partitions.Rows)
    {
        string partitionKey = partition["partition"].ToString();
        Query = $"UPDATE ContactChat PARTITION KEY {partitionKey} SET Status = 'Closed' WHERE Id = '{chat_id}'";
        result = db.Prompt(Query);

        if (result.StartsWith("Error:"))
        {
            return result;
        }
    }


    // Return success message
    return "Ok. Chat closed successfully.";

}

string GetContactChatReply()
{

    if (api.DataMessage == null)
    {
        return "Error: Missing required DataMessage in DataMessage (14)";
    }

    if (api.DataMessage.Chat_id == null)
    {
        return "Error: Missing required Chat_id in DataMessage (15)";
    }

    string chat_id = api.DataMessage.Chat_id.ToString();

    string Query = $"SELECT * FROM ContactChat WHERE Id = '{chat_id}'";

    string result = db.Prompt(Query);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: Chat not found";
    }

    DataTable chatData = JsonConvert.DeserializeObject<DataTable>(result);

    if (api.DataMessage.Timestamp == null)
    {
        Query = $"SELECT * FROM ContactMessage WHERE Chat_id = '{chat_id}' ORDER BY timestamp";
    }
    else
    {
        Query = $"SELECT * FROM ContactMessage WHERE Chat_id = '{chat_id}' AND timestamp > '{api.DataMessage.Timestamp}' ORDER BY timestamp";
    }

    result = db.Prompt(Query);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return result;
    }

    DataTable dataTable = JsonConvert.DeserializeObject<DataTable>(result);
    List<ContactMessage> messages = new();

    foreach (DataRow row in dataTable.Rows)
    {
        ContactMessage message = new()
        {
            Id = row["Id"].ToString(),
            Chat_id = row["Chat_id"].ToString(),
            Kiosko_user_id = row["Kiosko_user_id"].ToString(),
            Message_text = row["Message_text"].ToString(),
            Image_data = row["Image_data"].ToString(),
            Timestamp = row["timestamp"].ToString()
        };

        messages.Add(message);
    }

    // Return the message in JSON format
    return db.GetJson(messages);
}


string GetKioskoChatReply()
{

    string result = IsTokenValid(api, "ANY");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (api.DataMessage == null)
    {
        return "Error: Missing required DataMessage (16)";
    }

    if (api.DataMessage.Chat_id == null)
    {
        return "Error: Missing required Chat_id in DataMessage (17)";
    }

    string chat_id = api.DataMessage.Chat_id.ToString();

    string Query = $"SELECT * FROM ContactChat WHERE Id = '{chat_id}'";

    result = db.Prompt(Query);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: Chat not found";
    }

    if (api.DataMessage.Timestamp == null)
    {
        Query = $"SELECT * FROM ContactMessage WHERE Chat_id = '{chat_id}' AND Kiosko_user_id != '{api.User}' ORDER BY timestamp";
    }
    else
    {
        Query = $"SELECT * FROM ContactMessage WHERE Chat_id = '{chat_id}' AND Kiosko_user_id != '{api.User}' AND timestamp > '{api.DataMessage.Timestamp}' ORDER BY timestamp";
    }

    result = db.Prompt(Query);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return result;
    }

    DataTable chatData = JsonConvert.DeserializeObject<DataTable>(result);

    List<ContactMessage> messages = [];
    bool Waiting_status = false;

    foreach (DataRow row in chatData.Rows)
    {

        ContactMessage message = new()
        {
            Id = row["Id"].ToString(),
            Chat_id = row["Chat_id"].ToString(),
            Kiosko_user_id = row["Kiosko_user_id"].ToString(),
            Message_text = row["Message_text"].ToString(),
            Image_data = row["Image_data"].ToString(),
            Timestamp = row["timestamp"].ToString()
        };

        if (message.Kiosko_user_id == "")
        {
            Waiting_status = true;
        }
        else
        {
            Waiting_status = false;
        }

        messages.Add(message);

    }

    if (Waiting_status)
    {
        result = db.Prompt($"GET PARTITIONS FROM TABLE contactchat");

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        DataTable partitions = JsonConvert.DeserializeObject<DataTable>(result);

        foreach (DataRow partition in partitions.Rows)
        {
            string partitionKey = partition["partition"].ToString();
            result = db.Prompt($"UPDATE contactchat PARTITION KEY {partitionKey} SET Kiosko_user_id = '{api.DataMessage.Kiosko_user_id}', Status = 'Waiting' WHERE Id = '{api.DataMessage.Chat_id}'");

            if (result.StartsWith("Error:"))
            {
                return result;
            }

        }
    }

    // Return the message in JSON format
    return db.GetJson(messages);
}



string GetChats()
{

    string result = IsTokenValid(api, "ANY");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (api.DataMessage == null)
    {
        return "Error: Missing required DataMessage (1)";
    }

    if (api.DataMessage.OnlyPending == null)
    {
        return "Error: Missing required OnlyPending in DataMessage (2)";
    }

    string search_text = api.DataMessage.Search_text?.ToString()?.Trim() ?? "";

    if (search_text.Length > 0)
    {
        search_text = "%" + search_text.Replace("'", "''").Replace(" ", "%") + "%";
    }

    bool only_pending = api.DataMessage.OnlyPending;

    string formato = "yyyy-MM-dd";

    string start_date = api.DataMessage.Start_date.ToString(formato) + " 00:00:00.0000000";
    string end_date = api.DataMessage.End_date.ToString(formato) + " 23:59:59.9999999";

    string Query;

    end_date = ConvertirLocalAUtcAngelFormat(end_date);

    if (only_pending)
    {
        if (search_text.Length > 0)
        {
            Query = $"SELECT * FROM ContactChat WHERE timestamp >= '{start_date}' AND timestamp <= '{end_date}' AND Status <> 'Closed' AND (Phone LIKE '{search_text}' OR Email LIKE '{search_text}' OR Message_text LIKE '{search_text}') ORDER BY timestamp";
        }
        else
        {
            Query = $"SELECT * FROM ContactChat WHERE timestamp >= '{start_date}' AND timestamp <= '{end_date}' AND Status <> 'Closed' ORDER BY timestamp";
        }
    }
    else
    {
        if (search_text.Length > 0)
        {
            Query = $"SELECT * FROM ContactChat WHERE timestamp >= '{start_date}' AND timestamp <= '{end_date}' AND (Phone LIKE '{search_text}' OR Email LIKE '{search_text}' OR Message_text LIKE '{search_text}') ORDER BY timestamp";
        }
        else
        {
            // If no search text, return all chats within the date range
            Query = $"SELECT * FROM ContactChat WHERE timestamp >= '{start_date}' AND timestamp <= '{end_date}' ORDER BY timestamp";
        }
    }

    return db.Prompt(Query);

}


string GetChat()
{
    if (api.DataMessage == null)
    {
        return "Error: Missing required DataMessage (3)";
    }

    if (api.DataMessage.Chat_id == null)
    {
        return "Error: Missing required Chat_id in DataMessage (4)";
    }

    string chat_id = api.DataMessage.Chat_id.ToString();
    string Query = $"SELECT * FROM ContactChat WHERE Id = '{chat_id}'";

    string result = db.Prompt(Query);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: Chat not found";
    }

    DataTable chatData = JsonConvert.DeserializeObject<DataTable>(result);
    string query = "";

    if (api.DataMessage.Timestamp == null)
    {
        query = $"SELECT * FROM ContactMessage PARTITION KEY {chatData.Rows[0]["timestamp"].ToString()[..7]} WHERE Chat_id = '{chat_id}' ORDER BY timestamp";
    }
    else
    {
        query = $"SELECT * FROM ContactMessage PARTITION KEY {chatData.Rows[0]["timestamp"].ToString()[..7]} WHERE Chat_id = '{chat_id}' AND timestamp > '{api.DataMessage.Timestamp}' ORDER BY timestamp";
    }

    result = db.Prompt(query);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    DataTable messagesData = JsonConvert.DeserializeObject<DataTable>(result);

    var contactChat = new
    {
        Id = chatData.Rows[0]["Id"].ToString(),
        Kiosko_user_id = chatData.Rows[0]["Kiosko_user_id"].ToString(),
        Phone = chatData.Rows[0]["Phone"].ToString(),
        Email = chatData.Rows[0]["Email"].ToString(),
        Message_text = chatData.Rows[0]["Message_text"].ToString(),
        Status = chatData.Rows[0]["Status"].ToString(),
        Source = chatData.Rows[0]["Source"].ToString(),
        Timestamp = ConvertirUtcALocalAngelFormat(chatData.Rows[0]["timestamp"].ToString()),
        // Aqui necesito el ultimo timestamp de los mensajes
        LastMessageTimestamp = messagesData.AsEnumerable().Select(row => row["timestamp"].ToString()).FirstOrDefault(),
        Messages = messagesData.AsEnumerable().Select(row => new
        {
            Id = row["Id"].ToString(),
            Chat_id = row["Chat_id"].ToString(),
            Kiosko_user_id = row["Kiosko_user_id"].ToString(),
            Message_text = row["Message_text"].ToString(),
            Image_data = row["Image_data"].ToString(),
            Timestamp = row["timestamp"].ToString()
        }).ToList()
    };

    return db.GetJson(contactChat);

}

public static string ConvertirUtcALocalAngelFormat(string fechaUtcAngel)
{
    // Parseamos usando el formato exacto
    var formato = "yyyy-MM-dd HH:mm:ss.fffffff";
    if (!DateTime.TryParseExact(fechaUtcAngel, formato, CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var fechaUtc))
    {
        throw new FormatException("La fecha no tiene el formato AngelSQL válido.");
    }

    // Convertimos a hora local
    var fechaLocal = fechaUtc.ToLocalTime();

    // Devolvemos en el mismo formato AngelSQL
    return fechaLocal.ToString(formato);
}

public static string ConvertirLocalAUtcAngelFormat(string fechaLocalAngel)
{
    var formato = "yyyy-MM-dd HH:mm:ss.fffffff";

    if (!DateTime.TryParseExact(fechaLocalAngel, formato, CultureInfo.InvariantCulture,
        DateTimeStyles.None, out var fechaLocal))
    {
        throw new FormatException("La fecha no tiene el formato AngelSQL válido.");
    }

    // Aquí usamos DateTime.SpecifyKind para asegurar que lo tratamos como Local
    var fechaLocalCorrecta = DateTime.SpecifyKind(fechaLocal, DateTimeKind.Local);
    var fechaUtc = fechaLocalCorrecta.ToUniversalTime();

    //Console.WriteLine($"Converting local date {fechaLocalCorrecta} to UTC: {fechaUtc}");

    return fechaUtc.ToString(formato);
}

string InsertKioskoUserChat()
{

    string result = IsTokenValid(api, "ANY");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (api.DataMessage == null)
    {
        return "Error: Missing required DataMessage (5)";
    }

    if (api.DataMessage.Chat_id == null)
    {
        return "Error: Missing required Chat_id in DataMessage (6)";
    }

    result = db.Prompt($"SELECT * FROM ContactChat WHERE Id = '{api.DataMessage.Chat_id}'");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    if (result == "[]")
    {
        return "Error: Chat not found";
    }

    string message_id = Guid.NewGuid().ToString();

    if (api.DataMessage.Image_data.ToString().Contains("base64"))
    {
        string directory = server_db.Prompt($"VAR db_wwwroot", true) + $"/images/chats/{api.account}";

        if (Directory.Exists(directory) == false)
        {
            Directory.CreateDirectory(directory);
        }

        string path = AngelDB.Base64Helper.SaveBase64ToAutoNamedFile(api.DataMessage.Image_data.ToString(), directory, message_id);

        if (path == "Error:")
        {
            Console.WriteLine("Error: Unable to save image");
            return "Error: Unable to save image ";
        }

        api.DataMessage.Image_data = $"../images/chats/{api.account}/" + message_id + Path.GetExtension(path);

    }

    ContactMessage contactMessage = new()
    {
        Id = message_id,
        Chat_id = api.DataMessage.Chat_id,
        Kiosko_user_id = api.User,
        Message_text = api.DataMessage.Message_text,
        Image_data = api.DataMessage.Image_data
    };

    DataTable chatData = JsonConvert.DeserializeObject<DataTable>(result);

    result = db.InsertInto("ContactMessage", contactMessage, chatData.Rows[0]["timestamp"].ToString()[..7]);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    result = db.Prompt($"GET PARTITIONS FROM TABLE contactchat");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    DataTable partitions = JsonConvert.DeserializeObject<DataTable>(result);

    foreach (DataRow partition in partitions.Rows)
    {
        string partitionKey = partition["partition"].ToString();
        result = db.Prompt($"UPDATE contactchat PARTITION KEY {partitionKey} SET Kiosko_user_id = '{api.DataMessage.Kiosko_user_id}', Status = 'Answered' WHERE Id = '{api.DataMessage.Chat_id}'");

        if (result.StartsWith("Error:"))
        {
            return result;
        }
    }

    // Return the message in JSON format
    return "Ok. Message inserted successfully.";

}


string InsertContactChat(AngelApiOperation api, Translations translation)
{

    if (api.DataMessage == null)
    {
        return "Error: Missing required DataMessage (8)";
    }

    if (api.DataMessage.Phone == null)
    {
        return "Error: Missing required phone in DataMessage (9)";
    }

    if (api.DataMessage.Email == null)
    {
        return "Error: Missing required email in DataMessage (10)";
    }

    if (api.DataMessage.Message_text == null)
    {
        return "Error: Missing required message_text in DataMessage (11)";
    }

    if (api.DataMessage.Chat_id == null)
    {
        api.DataMessage.Chat_id = Guid.NewGuid().ToString();
    }

    // Update existing chat
    ContactChat contactChat = new()
    {
        Id = api.DataMessage.Chat_id,
        Kiosko_user_id = "",
        Phone = api.DataMessage.Phone.ToString(),
        Email = api.DataMessage.Email.ToString(),
        Message_text = api.DataMessage.Message_text.ToString(),
        Status = "Waiting",
        Source = api.DataMessage.Source.ToString()
    };

    string result = db.UpsertInto("ContactChat", contactChat, DateTime.UtcNow.ToString("yyyy-MM"));

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    result = db.Prompt($"SELECT * FROM ContactChat WHERE Id = '{api.DataMessage.Chat_id}'");

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    DataTable chatData = JsonConvert.DeserializeObject<DataTable>(result);

    string message_id = Guid.NewGuid().ToString();

    if (api.DataMessage.Image_data.ToString().Contains("base64"))
    {
        string directory = server_db.Prompt($"VAR db_wwwroot", true) + $"/images/chats/{api.account}";

        if (Directory.Exists(directory) == false)
        {
            Directory.CreateDirectory(directory);
        }

        string path = AngelDB.Base64Helper.SaveBase64ToAutoNamedFile(api.DataMessage.Image_data.ToString(), directory, message_id);

        if (path == "Error:")
        {
            Console.WriteLine("Error: Unable to save image");
            return "Error: Unable to save image ";
        }

        api.DataMessage.Image_data = $"../images/chats/{api.account}/" + message_id + Path.GetExtension(path);

    }

    ContactMessage contactMessage = new()
    {
        Id = message_id,
        Chat_id = api.DataMessage.Chat_id,
        Kiosko_user_id = "",
        Message_text = api.DataMessage.Message_text,
        Image_data = api.DataMessage.Image_data
    };

    result = db.InsertInto("ContactMessage", contactMessage, chatData.Rows[0]["timestamp"].ToString()[..7]);

    if (result.StartsWith("Error:"))
    {
        return result;
    }

    ContactMessage Message_text = new()
    {
        Id = api.DataMessage.Chat_id,
        Kiosko_user_id = "...",
        Message_text = "Search for a user to assist you.",
        Image_data = "",
        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff")
    };

    return db.GetJson(Message_text);

}

