// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

using System;
using System.IO;
using System.Text;

string result;

string directory = "C:/Users/danie/OneDrive/UVM/ComputacionMatematicaCienciaDeDatos/Introducción a los modelos de demanda de transporte/Modulo5";

result = db.Prompt($"GPT READ FILES *.txt FROM DIRECTORY {directory} INCLUDE FILE NAME", true);
result = db.Prompt("GPT SET MODEL gpt-4o-mini");
result = db.Prompt("GPT PROMPT ¿Me puedes dar un resumen de los archivos?, por favor", true);

File.WriteAllText($"{ directory}/ Resumen.txt", result);

Console.WriteLine("Resumen:");
Console.WriteLine(result);

result = db.Prompt("GPT PROMPT Me das los puntos mas importantes a memorizar, pensando que pudiera presentar un examen", true);

File.WriteAllText($"{directory}/Examen.txt", result);

Console.WriteLine("Memorizar:");
Console.WriteLine(result);

result = SumFiles($"{directory}", "*.txt", "Allfiles.txt", true);


public string SumFiles(string directory_name, string file_extension, string file_destiny, bool includeFileTittle)
{
    try
    {
        string[] files = Directory.GetFiles(directory_name, file_extension);

        StringBuilder sb = new StringBuilder();

        foreach (var file in files)
        {
            string text = File.ReadAllText(file);

            if (includeFileTittle)
            {
                sb.AppendLine($"File: {file}");
            }

            sb.AppendLine(text);
            sb.AppendLine();
        }

        File.WriteAllText(directory_name + "/" + file_destiny, sb.ToString());

        return "Ok.";

    }
    catch (Exception e)
    {
        return $"Error: SumFiles() {e}";
    }
}


