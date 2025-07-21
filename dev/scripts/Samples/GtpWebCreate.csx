// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;


db.Prompt("OLLAMA URL DEFAULT", true);
db.Prompt("OLLAMA MODEL llama3.2", true);

string directory = "C:/AngelSQL/AngelNET/AngelSQLServer/dev";
CreateWebFile();


string CreateWebFile()
{

    string menu_html = File.ReadAllText($"{directory}/wwwroot/POSAdmin/menu.html");
    string businessinfo_csx = File.ReadAllText($"{directory}/scripts/POSApi/BusinessInfo.csx");

    db.Prompt("OLLAMA ADD SYSTEM MESSAGE archivo menu.html: " + menu_html, true);
    db.Prompt("OLLAMA ADD SYSTEM MESSAGE archivo BusinessInfo.csx: " + businessinfo_csx, true);

    string result;

    result = db.Prompt(@"OLLAMA 
                    Crea un código HTML basado en la estructura del archivo menu.html, enfatizo el uso del estilo de menu.html,  
                    es una pantalla de captura para la clase BusinessInfo que te pase en el archivo BusinessInfo.csx 
                    Coloca un  espacio y un botón para subir la imagen, e. Esa imagen la necesito en base64, 
                    cuando de un clic en salvar,  me creas un objeto BusinessInfo junto con la imagen en base64, 
                    esta es la información que voy a mandar al backend, el resto de los campos son texto, 
                    y los voy a capturar en la pantalla,  debe contener un botón para salvar los cambios, si puedes coloca 
                    los campos de captura en una tabla, la tabla debe de tener dos columnas, una con el nombre del campo de 
                    captura y en la otra el campo para que el usuario pueda capturarlo, usa todos los valores de la clase, 
                    la tabla debe de estar centrada, Estoy usando bootstrap, no me des  comentarios, solamente necesito el HTML. 
                    Por favor, gracias."
                    , true);

    Console.WriteLine(result);

    if( !Directory.Exists("C:/daniel/test1") )
    {
        Directory.CreateDirectory("C:/daniel/test1");
    }

    File.WriteAllText($"C:/daniel/test1/businessconfig.html", result);
    return "Ok.";
}




void TareaGrande() 
{
    db.Prompt("OLLAMA ADD SYSTEM MESSAGE Esta es una tarea muy grande que va a requerir muchos tokens y esfuerzo computacional", true);

    string result = db.Prompt("OLLAMA Regresame en formato JSON cada una de las treas enumueradas y descritas, y un prompt que explique el contexto de cada tarea, para que no tengas que entender todo el contexto, sino que solamente la tarea descrita en el nodo", true);

    List<TareaGrande> tareas = System.Text.Json.JsonSerializer.Deserialize<List<TareaGrande>>(result);

    foreach (var tarea in tareas)
    {
        Console.WriteLine($"TareaId: {tarea.TareaId}, Tarea: {tarea.Tarea}, Descripcion: {tarea.Descripcion}, Prompt: {tarea.Prompt}");
        db.Prompt( "OLLAMA CLEAR", true);
        db.Prompt($"OLLAMA ADD SYSTEM MESSAGE TAREA GRANDE: {tarea.Tarea} - {tarea.Descripcion}", true);
        result = db.Prompt($"OLLAMA {tarea.Prompt}", true);

        Console.WriteLine($"Resultado de la tarea {tarea.TareaId}: {result}");
        File.WriteAllText($"C:/daniel/test1/tarea_{tarea.TareaId}.txt", result);

    }

}



class TareaGrande
{
    // Un número progesivo para saber el orden de las tareas
    public string TareaId { get; set; }
    public string Tarea { get; set; }
    public string Descripcion { get; set; }
    public string Prompt { get; set; }
}

