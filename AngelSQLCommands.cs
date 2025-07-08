namespace AngelSQLServer
{
    public class AngelSQLCommands
    {
        public static Dictionary<string, string> DbCommands()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>
            {
                { @"SAVE ACTIVITY ON", @"SAVE ACTIVITY ON#free" },
                { @"SAVE ACTIVITY OFF", @"SAVE ACTIVITY OFF#free" },
                { @"WHITE LIST ON", @"WHITE LIST ON#free" },
                { @"WHITE LIST OFF", @"WHITE LIST OFF#free" },
                { @"BLACK LIST ON", @"WHITE LIST ON#free" },
                { @"BLACK LIST OFF", @"WHITE LIST OFF#free" },
                { @"ADD TO BLACK LIST", @"ADD TO BLACK LIST#free;COMMENT#freeoptional" },
                { @"REMOVE FROM BLACK LIST", @"REMOVE FROM BLACK LIST#free" },
                { @"ADD TO WHITE LIST", @"ADD TO WHITE LIST#free;COMMENT#freeoptional" },
                { @"REMOVE FROM WHITE LIST", @"REMOVE FROM WHITE LIST#free" },
                { @"CREATE SERVER ACCOUNT", @"CREATE SERVER ACCOUNT#free;USER#free;PASSWORD#free;DATA DIRECTORY#freeoptional;AS DEFAULT#optional" },
                { @"CREATE APP", @"CREATE APP#free; FILES DIRECTORY#free" },
                { @"QUIT", @"QUIT#free" },
                { @"PROXY", @"PROXY#free" },
                { @"SET PROXY", @"SET PROXY#free;USER#freeoptional;PASSWORD#freeoptional;PUBLIC ACCOUNT#freeoptional" },
                { @"CLEAR", @"CLEAR#free" },
                { @"LISTEN ON", @"LISTEN ON#free" },
                { @"SHOW COMMANDS", @"SHOW COMMANDS#free" },
                { @"DB", @"DB#free" },
                { @"SERVER DB", @"SERVER DB#free" },
                { @"CHANGE SEVER MASTER", @"CHANGE SEVER MASTER#free;TO USER#free;PASSWORD#free" },
            };

            return commands;

        }
    }
}