using System;
using System.Collections.Generic;

public class Chat
{
    public string Id { get; set; }
    public string Kiosko_user_id_sender { get; set; }
    public string Kiosko_user_id_receiver { get; set; }
    public string Message_text { get; set; }
    public string Image_data { get; set; }
}

public class ContactChat
{
    public string Id { get; set; }
    public string Kiosko_user_id { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Message_text { get; set; }
    public string Source { get; set; }
    public string Status { get; set; } = "Waiting"; 
}

public class ContactMessage
{
    public string Id { get; set; }
    public string Chat_id { get; set; }
    public string Kiosko_user_id { get; set; }
    public string Message_text { get; set; }
    public string Image_data { get; set; }
    public string Timestamp { get; set; } // Timestamp in UTC format
}

public class ChatGroup
{
    public string Id { get; set; }
    public string Description { get; set; }
    public string Kiosko_user_admin { get; set; }
    public List<string> Kiosko_user_ids { get; set; } = [];
}

public class ChatGroupMessage
{
    public string Id { get; set; }
    public string ChatGroup_Id { get; set; }
    public string Kiosko_user_Id { get; set; }
    public string MessageText { get; set; }
    public string ImageData { get; set; }
}