using System;
using System.Collections.Generic;

public class Translations
{
    private readonly Dictionary<string, string> spanish = [];

    public string SpanishValues()
    {
        spanish.Add("Id is null", "Id es nulo");
        return "Ok.";
    }

    public string Get(string key, string language)
    {        

        if( language == "" || language == "en" || language == null )
        {
            return key;
        }

        if( language == "es" ) 
        {
            if( !spanish.ContainsKey(key) ) 
            {
                return key;
            } 
            else 
            {
                return spanish[key];
            }
        }

        return key;
    }
}


