// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

using System;
using System.IO;
using System.Text;


db.Prompt("OLLAMA", true);

string directory = "C:/AngelSQL/AngelNET/AngelSQLServer/dev";
CreateWebFile();

string CreateWebFile()
{

    string menu_html = File.ReadAllText($"{directory}/wwwroot/POSAdmin/menu.html");
    string businessinfo_csx = File.ReadAllText($"{directory}/scripts/POSApi/BusinessInfo.csx");

    db.Prompt("OLLAMA ADD SYSTEM MESSAGE archivo menu.html: " + menu_html, true);
    db.Prompt("OLLAMA ADD SYSTEM MESSAGE archivo BusinessInfo.csx: " + businessinfo_csx, true);

    string result;

    result = db.Prompt(@"OLLAMA PROMPT 
                    Creame un código html basado en la estructura del archivo menu.html, enfetizo el uso del estilo de menu.htmml, 
                    es una pantalla de captura para la clase BusinessInfo que te pase en el archivo BusinessInfo.csx, me puedes poner un 
                    espacio y un botón para subir la imagen, esa imagen la necesito en base 64, cuando de un clic en salvar, 
                    me creas un objeto BusinessInfo junto con la imagen en base 64, esta es la info
                    Que voy a mandar al backend, el resto de los campos son texto, y los voy a capturar en la pantalla,
                    Debe contener un botón para salvar los cambios, si puedes coloca los campos de captura en una tabla, la 
                    tabla debe de tener dos columnas, una con el nombre del campo de captura y en la otra el campo de captura, usa
                    todos los valores de la clase, la tabla debe de estar centrada, Estoy usando bootstrap, no me des 
                    comentarios, solo necesito el HTML. por favor, Gracias
                    "
                    , true);

    Console.WriteLine(result);

    File.WriteAllText($"{directory}/wwwroot/POSAdmin/businessconfig.html", result);
    return "Ok.";
}
