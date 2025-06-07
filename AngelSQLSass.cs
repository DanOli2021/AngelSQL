namespace AngelSQLServer
{
    public class AngelSQLsass
    {
        public string Id { get; set; }
        public string Account { get; set; } = "";
        public string Account_id { get; set; } = "";
        public string Password { get; set; } = "";
        public int Port { get; set; } = 0;
        public string Domain { get; set; } = "";
        public string Ssh_user { get; set; } = "";
        public int Ssh_port { get; set; } = 0;
        public string Ssh_password { get; set; } = "";
        public string Host { get; set; } = "";
        public int Host_Port { get; set; } = 0;
        public string Main_domain { get; set; } = "";
    }
}
