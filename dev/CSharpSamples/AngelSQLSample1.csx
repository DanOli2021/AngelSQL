// GLOBALS
// These lines of code go in each script
// El proposito de estas lineas es simplemente evitar que Visual Studio Code muestre errores en el editor
// Durante la ejecucion de los scripts, estas lineas son ignoradas
#load "Globals.csx"
// END GLOBALS

using System;
using System.Collections.Generic;
using System.Data;

// Si ejecutas db.exe en la terminal, ejecuta el siguiente comando
// SCRIPT FILE dev/CSharpSamples/AngelSQLSample1.csx

// Si este ejercicio ya los has ejecutado, elimina la base de datos creada
// en el directorio /mydata/data2

// Cuando se ejecuta el script recibes dos objetos db y server_db
// db es un objeto de la clase AngelDB.DB que representa la base de datos que se usa en el
// contexto del script, si se ejecuta en el servidor tambien se recive server_db que es un objeto
// de la clase AngelDB.DB que representa la base de datos del servidor

string result = "";

// Primero nos firmamos en  una base de datos
// el usuario por defecto es db y la contraseña es db
// El metodo Prompt es el más importante de la clase DB
// El direcotrio por defecto se llama Data y se encuentra donde se ejecuta db.exe
result = db.Prompt("DB USER db PASSWORD db");

Console.WriteLine("Base de datos creada: " + result);

// Angel DB siempre devuelve un string con el resultado de la operación
// El resultado puede ser cualuiera de los siguientes:
// "Ok." - La operación se realizó con éxito
// "Error: " + mensaje - La operación no se realizó con éxito
// json - La operación se realizó con éxito y se devolvió un string JSon

// Nos podemos conectar a un direcotorio distinto al que se usa por defecto
result = db.Prompt("DB USER db PASSWORD db DATA DIRECTORY /mydata/data2");

Console.WriteLine("Base de datos creada en el directorio /mydata/data2: " + result);

// La seguridad es importante, cambiamos el usuario y la contraseña del usuario MASTER
// AngelDB tiene tres tipos de usuarios: MASTER, ACCOUNT_MASTER y DATABASE_USER
// MASTER es el usuario que tiene todos los permisos
// ACCOUNT_MASTER es el usuario que tiene permisos para crear bases de datos y usuarios 
// En una cuenta especifica puede haber varios ACCOUNT_MASTER
// DATABASE_USER es el usuario que tiene permisos para realizar operaciones en una base de datos

// Cambiamos el usuario y la contraseña del usuario MASTER
result = db.Prompt("CHANGE MASTER TO USER usuario_mater PASSWORD 7887667)=)=)((ññ;");

Console.WriteLine("Usuario MASTER cambiado: " + result);

// Creamos la cuenta angelsql con el usuario account_master y la contraseña 7887667)=)=)((ññ;
result = db.Prompt("CREATE ACCOUNT angelsql SUPERUSER account_master PASSWORD 7887667)=)=)((ññ;");

Console.WriteLine("Cuenta angelsql creada: " + result);

// Usamos la cuenta angelsql
result = db.Prompt("USE ACCOUNT angelsql");

Console.WriteLine("Cuenta angelsql usada: " + result);

// Creamos la base de datos angelsqldb
result = db.Prompt("CREATE DATABASE angelsqldb");
Console.WriteLine("Base de datos angelsqldb creada: " + result);

// Usamos la base de datos angelsqldb
result = db.Prompt("USE DATABASE angelsqldb");
Console.WriteLine("Base de datos angelsqldb usada: " + result); 


// Creamos la clase persona con los campos nombre y edad
class Persona
{
    // El campo Id es obligatorio
    public string Id { get; set; }
    public string Nombre { get; set; }
    public int Edad { get; set; }
}

Persona persona = new Persona();

// Creamos la tabla personas con los campos nombre y edad
result  = db.CreateTable(persona, "personas");

Console.WriteLine("Tabla personas creada: " + result);

persona.Id = "1";
persona.Nombre = "Angel";
persona.Edad = 50;

// Insertamos un registro en la tabla personas
result = db.UpsertInto("personas", persona);

Console.WriteLine("Registro insertado en la tabla personas: " + result);

// Mostramos los registros de la tabla personas
result = db.Prompt("SELECT * FROM personas");

Console.WriteLine("Registros de la tabla personas: " + result);

// Insertamos 30 registros en la tabla personas
List<Persona> personas = new List<Persona>();

// Creamos 30 registros de la clase Persona en una lista
// y los insertamos en la tabla personas
// Esto es más eficiente que insertar un registro a la vez
for (int i = 0; i < 30; i++)
{
    Persona p = new Persona();
    p.Id = i.ToString();
    p.Nombre = "Nombre" + i.ToString();
    p.Edad = 20 + i;
    personas.Add(p);
}

result = db.UpsertInto("personas", personas);

Console.WriteLine("30 registros insertados en la tabla personas: " + result);

// Mostramos los registros de la tabla personas
result = db.Prompt("SELECT * FROM personas");

Console.WriteLine("Registros de la tabla personas: " + result);

// Escrimos los resultados en un archivo
result = db.Prompt("WRITE FILE results.json VALUES " + result);

Console.WriteLine("Resultados escritos en el archivo results.json: " + result);

// Borramos la tabla personas
result = db.Prompt("DELETE FROM personas PARTITION KEY main WHERE 1 = 1");

Console.WriteLine("Tabla personas borrada: " + result);

// Leemos los registros del archivos results.json

result = db.Prompt("READ FILE results.json");

Console.WriteLine("Registros leidos del archivo results.json: " + result);

