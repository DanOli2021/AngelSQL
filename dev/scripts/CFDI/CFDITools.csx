// GLOBALS
// These lines of code go in each script
#r "C:\AngelSQLNet\AngelSQL\db.dll"
#r "C:\AngelSQLNet\AngelSQL\Newtonsoft.Json.dll"
// END GLOBALS

using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using OpenSSL;

public static class CFDITools
{

    public static string GetXMlTimbrado(string json, string correo, string ventas = "", string datosAdicionales = "", string var1 = "", string var2 = "", string var3 = "", string var4 = "", string var5 = "")
    {

        if (json is null)
        {
            return "";
        }

        Comprobante c = JsonConvert.DeserializeObject<Comprobante>(json);

        string xml = ComprobanteToXML(c);

        string addenda = "";

        if (c.Addenda.Count > 0)
        {
            addenda = c.Addenda[0].ToString();
        }

        string result = ""; 

        // MyCFDI40SoapClient.EndpointConfiguration conf = new MyCFDI40SoapClient.EndpointConfiguration();
        // MyCFDI40SoapClient client = new MyCFDI40SoapClient(conf);
        // string result = client.TimbreCFDI40(RFC: c.Emisor.Rfc,
        //                     Nombre: c.Emisor.Nombre,
        //                     rfcReceptor: c.Receptor.Rfc,
        //                     nombreReceptor: c.Receptor.Nombre,
        //                     Serie: c.Serie,
        //                     Folio: c.Folio,
        //                     xmlString: xml,
        //                     Test: false,
        //                     Addenda: addenda,
        //                     datosAdicionales: datosAdicionales,
        //                     Correo: c.correo,
        //                     Ventas: c.Ventas,
        //                     Var1: var1,
        //                     Var2: var2,
        //                     Var3: var3,
        //                     Var4: var4,
        //                     Var5: var5);

        return result;

    }


    public static string SignDocument(Comprobante c, string privateKey_file, string certificate_file, string password) 
    {

        try
        {
            string certificate_string = "";
            string certificate_number = "";

            OpenSslKey.CertificateData( certificate_file, out certificate_string, out certificate_number);
            c.NoCetificado = certificate_number;
            c.Cetificado = certificate_string;

            string cadenaOriginal = CFDITools.CreateCadenaOriginal(c);
            string signed_string = OpenSslKey.SignString(cadenaOriginal, privateKey_file, password);
            c.Sello = signed_string;

            return "Ok.";
            
        }
        catch (System.Exception e)
        {
            return "Error: SignDocument " + e.Message;
        }

    }

    public static string ComprobanteToXML(Comprobante c)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf - 8\"?>");

        sb.AppendLine("<cfdi:Comprobante xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
        sb.AppendLine("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"");
        sb.AppendLine("xsi:schemaLocation=\"http://www.sat.gob.mx/cfd/4 http://www.sat.gob.mx/sitio_internet/cfd/4/cfdv40.xsd\"");
        sb.AppendLine("xmlns:cfdi=\"http://www.sat.gob.mx/cfd/4\"");
        sb.AppendLine("Version=\"4.0\"");

        if (!string.IsNullOrEmpty(c.Sello))
        {
            sb.AppendLine($"Sello=\"{c.Sello}\"");
        }

        if (!string.IsNullOrEmpty(c.NoCetificado))
        {
            sb.AppendLine($"NoCertificado=\"{c.NoCetificado}\"");
        }

        if (!string.IsNullOrEmpty(c.Cetificado))
        {
            sb.AppendLine($"Certificado=\"{c.Cetificado}\"");
        }

        if (!string.IsNullOrEmpty(c.Serie))
        {
            sb.AppendLine($"Serie=\"{c.Serie}\"");
        }

        if (!string.IsNullOrEmpty(c.Folio))
        {
            sb.AppendLine($"Folio=\"{c.Folio}\"");
        }

        if (!string.IsNullOrEmpty(c.Fecha))
        {
            sb.AppendLine($"Fecha=\"{c.Fecha}\"");
        }

        if (!string.IsNullOrEmpty(c.FormaPago))
        {
            sb.AppendLine($"FormaPago=\"{c.FormaPago}\"");
        }

        if (!string.IsNullOrEmpty(c.CondicionesDePago))
        {
            sb.AppendLine($"CondicionesDePago=\"{c.CondicionesDePago}\"");
        }

        if (!string.IsNullOrEmpty(c.SubTotal))
        {
            sb.AppendLine($"SubTotal=\"{c.SubTotal}\"");
        }

        if (!string.IsNullOrEmpty(c.Moneda))
        {
            sb.AppendLine($"Moneda=\"{c.Moneda}\"");
        }

        if (!string.IsNullOrEmpty(c.Total))
        {
            sb.AppendLine($"Total=\"{c.Total}\"");
        }

        if (!string.IsNullOrEmpty(c.TipoDeComprobante))
        {
            sb.AppendLine($"TipoDeComprobante=\"{c.TipoDeComprobante}\"");
        }

        if (!string.IsNullOrEmpty(c.Exportacion))
        {
            sb.AppendLine($"Exportacion=\"{c.Exportacion}\"");
        }

        if (!string.IsNullOrEmpty(c.MetodoPago))
        {
            sb.AppendLine("MetodoPago=\"PUE\"");
        }

        if (!string.IsNullOrEmpty(c.LugarExpedicion))
        {
            sb.AppendLine($"LugarExpedicion=\"{EscapeXml(c.LugarExpedicion)}\"");
        }

        sb.Append(">");

        sb.AppendLine($"<cfdi:Emisor Rfc=\"{EscapeXml(c.Emisor.Rfc)}\" Nombre=\"{EscapeXml(c.Emisor.Nombre)}\" RegimenFiscal=\"{c.Emisor.RegimenFiscal}\" />");
        sb.AppendLine($"<cfdi:Receptor Rfc=\"{EscapeXml(c.Receptor.Rfc)}\" Nombre=\"{EscapeXml(c.Receptor.Nombre)}\" DomicilioFiscalReceptor=\"{c.Receptor.DomicilioFiscalReceptor}\" RegimenFiscalReceptor=\"{c.Receptor.RegimenFiscalReceptor}\" UsoCFDI=\"{c.Receptor.UsoCFDI}\" />");

        sb.AppendLine(" <cfdi:Conceptos>");

        foreach (Concepto concepto in c.Conceptos)
        {
            sb.AppendLine($"     <cfdi:Concepto ClaveProdServ=\"{concepto.ClaveProdServ}\" NoIdentificacion=\"{EscapeXml(concepto.NoIdentificacion)}\" Cantidad=\"{concepto.Cantidad}\" ClaveUnidad=\"{concepto.ClaveUnidad}\" Unidad=\"{EscapeXml(concepto.Unidad)}\" Descripcion=\"{EscapeXml(concepto.Descripcion)}\" ValorUnitario=\"{concepto.ValorUnitario}\" Importe=\"{concepto.Importe}\" ObjetoImp=\"{concepto.ObjetoImp}\">");

            foreach (Impuesto impuesto in concepto.Impuestos)
            {
                sb.AppendLine("         <cfdi:Impuestos>");

                if (impuesto.Traslados.Count > 0)
                {
                    sb.AppendLine("          <cfdi:Traslados>");

                    foreach (Traslado traslado in impuesto.Traslados)
                    {
                        sb.AppendLine($"                 <cfdi:Traslado Base=\"{traslado.Base}\" Impuesto=\"{traslado.Impuesto}\" TipoFactor=\"{traslado.TipoFactor}\" TasaOCuota=\"{traslado.TasaOCuota}\" Importe=\"{traslado.Importe}\" />");
                    }

                    sb.AppendLine("          </cfdi:Traslados>");
                }

                sb.AppendLine("         </cfdi:Impuestos>");
            }

            sb.AppendLine("      </cfdi:Concepto>");
        }

        sb.AppendLine(" </cfdi:Conceptos>");

        foreach (Impuesto impuesto in c.Impuestos)
        {
            sb.AppendLine($" <cfdi:Impuestos TotalImpuestosTrasladados=\"{impuesto.TotalImpuestosTrasladados}\">");

            if (impuesto.Traslados.Count > 0)
            {
                sb.AppendLine("     <cfdi:Traslados>");

                foreach (Traslado traslado in impuesto.Traslados)
                {
                    sb.AppendLine($"         <cfdi:Traslado Base=\"{traslado.Base}\" Impuesto=\"{traslado.Impuesto}\" TipoFactor=\"{traslado.TipoFactor}\" TasaOCuota=\"{traslado.TasaOCuota}\" Importe=\"{traslado.Importe}\" />");
                }

                sb.AppendLine("     </cfdi:Traslados>");

            }

            sb.AppendLine(" </cfdi:Impuestos>");
        }

        sb.AppendLine("</cfdi:Comprobante>");

        return sb.ToString();

    }

    public static string EscapeXml(string s)
    {
        string toxml = s;
        if (!string.IsNullOrEmpty(toxml))
        {
            // replace literal values with entities
            toxml = toxml.Replace("&", "&amp;");
            toxml = toxml.Replace("'", "&apos;");
            toxml = toxml.Replace("\"", "&quot;");
            toxml = toxml.Replace(">", "&gt;");
            toxml = toxml.Replace("<", "&lt;");
        }
        return toxml;
    }

    public static string UnescapeXml(string s)
    {
        string unxml = s;
        if (!string.IsNullOrEmpty(unxml))
        {
            // replace entities with literal values
            unxml = unxml.Replace("&apos;", "'");
            unxml = unxml.Replace("&    quot;", "\"");
            unxml = unxml.Replace("&gt;", ">");
            unxml = unxml.Replace("&lt;", "<");
            unxml = unxml.Replace("&amp;", "&");
        }
        return unxml;
    }
    

    public static string CreateCadenaOriginal(Comprobante cfd)
    {
        try
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("||");
            AddValue(sb, cfd.Version);
            AddValue(sb, cfd.Serie);
            AddValue(sb, cfd.Folio);
            AddValue(sb, cfd.Fecha);
            AddValue(sb, cfd.FormaPago);
            AddValue(sb, cfd.NoCetificado);
            AddValue(sb, cfd.CondicionesDePago);
            AddValue(sb, cfd.SubTotal);
            AddValue(sb, cfd.Descuento);
            AddValue(sb, cfd.Moneda);
            AddValue(sb, cfd.TipoCambio);
            AddValue(sb, cfd.Total);
            AddValue(sb, cfd.TipoDeComprobante);
            AddValue(sb, cfd.Exportacion);
            AddValue(sb, cfd.MetodoPago);
            AddValue(sb, cfd.LugarExpedicion);
            AddValue(sb, cfd.Confirmacion);

            foreach (InformacionGlobal item in cfd.InformacionGlobal)
            {
                AddValue(sb, item.Periodicidad);
                AddValue(sb, item.Meses);
                AddValue(sb, item.Año);
            }

            foreach (CfdiRelacionado item in cfd.CfdiRelacionados)
            {
                AddValue(sb, item.TipoRelacion);
                AddValue(sb, item.UUID);
            }

            AddValue(sb, cfd.Emisor.Rfc);
            AddValue(sb, cfd.Emisor.Nombre);
            AddValue(sb, cfd.Emisor.RegimenFiscal);
            AddValue(sb, cfd.Emisor.FacAtrAdquirente);
            AddValue(sb, cfd.Receptor.Rfc);
            AddValue(sb, cfd.Receptor.Nombre);
            AddValue(sb, cfd.Receptor.DomicilioFiscalReceptor);
            AddValue(sb, cfd.Receptor.ResidenciaFiscal);
            AddValue(sb, cfd.Receptor.NumRegIdTrib);
            AddValue(sb, cfd.Receptor.RegimenFiscalReceptor);
            AddValue(sb, cfd.Receptor.UsoCFDI);

            foreach (Concepto concepto in cfd.Conceptos)
            {
                AddValue(sb, concepto.ClaveProdServ);
                AddValue(sb, concepto.NoIdentificacion);
                AddValue(sb, concepto.Cantidad);
                AddValue(sb, concepto.ClaveUnidad);
                AddValue(sb, concepto.Unidad);
                AddValue(sb, concepto.Descripcion);
                AddValue(sb, concepto.ValorUnitario);
                AddValue(sb, concepto.Importe);
                AddValue(sb, concepto.Descuento);
                AddValue(sb, concepto.ObjetoImp);
                //AddValue(sb, concepto.ObjetoImp);

                foreach (Impuesto impuesto in concepto.Impuestos)
                {
                    foreach (Traslado traslado in impuesto.Traslados)
                    {
                        AddValue(sb, traslado.Base);
                        AddValue(sb, traslado.Impuesto);
                        AddValue(sb, traslado.TipoFactor);
                        AddValue(sb, traslado.TasaOCuota);
                        AddValue(sb, traslado.Importe);
                    }

                    foreach (Retencion retencion in impuesto.Retenciones)
                    {
                        AddValue(sb, retencion.Base);
                        AddValue(sb, retencion.Impuesto);
                        AddValue(sb, retencion.TipoFactor);
                        AddValue(sb, retencion.TasaOCuota);
                        AddValue(sb, retencion.Importe);
                    }

                    foreach (ACuentaTerceros acuentaTerceros in concepto.ACuentaTerceros)
                    {
                        AddValue(sb, acuentaTerceros.RfcACuentaTerceros);
                        AddValue(sb, acuentaTerceros.NombreACuentaTerceros);
                        AddValue(sb, acuentaTerceros.RegimenFiscalACuentaTerceros);
                        AddValue(sb, acuentaTerceros.DomicilioFiscalACuentaTerceros);
                    }

                    foreach (InformacionAduanera informacionAduanera in concepto.InformacionAduanera)
                    {
                        AddValue(sb, informacionAduanera.NumeroPedimento);
                    }

                    foreach (Parte parte in concepto.Partes)
                    {
                        AddValue(sb, parte.ClaveProdServ);
                        AddValue(sb, parte.NoIdentificacion);
                        AddValue(sb, parte.Cantidad);
                        AddValue(sb, parte.Unidad);
                        AddValue(sb, parte.Descripcion);
                        AddValue(sb, parte.ValorUnitario);
                        AddValue(sb, parte.Importe);

                        foreach (InformacionAduanera informacionAduanera in parte.InformacionAduanera)
                        {
                            AddValue(sb, informacionAduanera.NumeroPedimento);
                        }

                    }
                }
            }

            foreach (Impuesto impuesto in cfd.Impuestos)
            {

                foreach (Retencion retencion in impuesto.Retenciones)
                {
                    AddValue(sb, retencion.Impuesto);
                    AddValue(sb, retencion.Importe);
                }

                AddValue(sb, impuesto.TotalImpuestosRetenidos);

                foreach (Traslado traslado in impuesto.Traslados)
                {
                    AddValue(sb, traslado.Base);
                    AddValue(sb, traslado.Impuesto);
                    AddValue(sb, traslado.TipoFactor);
                    AddValue(sb, traslado.TasaOCuota);
                    AddValue(sb, traslado.Importe);
                }

                AddValue(sb, impuesto.TotalImpuestosTrasladados);

            }

            sb.Append("|");

            return sb.ToString();

        }
        catch (System.Exception e)
        {
            return $"Error: CreateCadenaOriginal {e}";
        }

    }


    public static void AddValue(StringBuilder sb, string value)
    {

        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        sb.Append(value.Trim());
        sb.Append("|");
    }

}


public class Comprobante
{
    public string Version { get; set; }
    public string Serie { get; set; }
    public string Folio { get; set; }
    public string Fecha { get; set; }
    public string Sello { get; set; }
    public string FormaPago { get; set; }
    public string NoCetificado { get; set; }
    public string Cetificado { get; set; }
    public string CondicionesDePago { get; set; }
    public string SubTotal { get; set; }
    public string Descuento { get; set; }
    public string Moneda { get; set; }
    public string TipoCambio { get; set; }
    public string Total { get; set; }
    public string TipoDeComprobante { get; set; }
    public string Exportacion { get; set; }
    public string MetodoPago { get; set; }
    public string LugarExpedicion { get; set; }
    public string Confirmacion { get; set; }
    public Emisor Emisor { get; set; } = new Emisor();
    public Receptor Receptor { get; set; } = new Receptor();
    public List<Concepto> Conceptos { get; set; } = new List<Concepto>();
    public List<Impuesto> Impuestos { get; set; } = new List<Impuesto>();
    public List<InformacionGlobal> InformacionGlobal { get; set; } = new List<InformacionGlobal>();
    public List<CfdiRelacionado> CfdiRelacionados { get; set; } = new List<CfdiRelacionado>();
    public List<string> Complemento { get; set; } = new List<string>();
    public List<string> Addenda { get; set; } = new List<string>();

    //Only for WebMiCFDI
    public string CadenaOriginal { get; set; }
    public string Ventas { get; set; }
    public string correo { get; set; }

}


public class Concepto
{
    public string ClaveProdServ { get; set; }
    public string NoIdentificacion { get; set; }
    public string Cantidad { get; set; }
    public string ClaveUnidad { get; set; }
    public string Unidad { get; set; }
    public string Descripcion { get; set; }
    public string ValorUnitario { get; set; }
    public string Importe { get; set; }
    public string Descuento { get; set; }
    public string ObjetoImp { get; set; }
    public List<Impuesto> Impuestos { get; set; } = new List<Impuesto>();
    public List<Retencion> Retenciones { get; set; } = new List<Retencion>();
    public List<ACuentaTerceros> ACuentaTerceros { get; set; } = new List<ACuentaTerceros>();
    public List<InformacionAduanera> InformacionAduanera { get; set; } = new List<InformacionAduanera>();
    public List<CuentaPredial> CuentaPredial { get; set; } = new List<CuentaPredial>();
    public List<string> ComplementoConcepto { get; set; } = new List<string>();
    public Parte[] Partes { get; set; } = new Parte[0];
}


public class Emisor
{
    public string Rfc { get; set; }
    public string Nombre { get; set; }
    public string RegimenFiscal { get; set; }
    public string FacAtrAdquirente { get; set; }
}

public class Receptor
{
    public string Rfc { get; set; }
    public string Nombre { get; set; }
    public string DomicilioFiscalReceptor { get; set; }
    public string ResidenciaFiscal { get; set; }
    public string NumRegIdTrib { get; set; }
    public string RegimenFiscalReceptor { get; set; }
    public string UsoCFDI { get; set; }
}


public class Impuesto
{
    public string TotalImpuestosRetenidos { get; set; }
    public string TotalImpuestosTrasladados { get; set; }
    public List<Retencion> Retenciones { get; set; } = new List<Retencion>();
    public List<Traslado> Traslados { get; set; } = new List<Traslado>();
}

public class Traslado
{
    public string Base { get; set; }
    public string Impuesto { get; set; }
    public string TipoFactor { get; set; }
    public string TasaOCuota { get; set; }
    public string Importe { get; set; }
}


public class Retencion
{
    public string Base { get; set; }
    public string Impuesto { get; set; }
    public string TipoFactor { get; set; }
    public string TasaOCuota { get; set; }
    public string Importe { get; set; }
}


public class InformacionGlobal
{
    public string Periodicidad { get; set; }
    public string Meses { get; set; }
    public string Año { get; set; }
    public string SubTotal { get; set; }
    public string Descuento { get; set; }
}



public class CfdiRelacionado
{
    public string UUID { get; set; }
    public string TipoRelacion { get; set; }

}


public class ACuentaTerceros
{
    public string RfcACuentaTerceros { get; set; }
    public string NombreACuentaTerceros { get; set; }
    public string RegimenFiscalACuentaTerceros { get; set; }

    public string DomicilioFiscalACuentaTerceros { get; set; }

}


public class InformacionAduanera
{
    public string NumeroPedimento { get; set; }
}


public class CuentaPredial
{
    public string Numero { get; set; }
}


public class Parte
{
    public List<InformacionAduanera> InformacionAduanera { get; set; } = new List<InformacionAduanera>();
    public string ClaveProdServ { get; set; }
    public string NoIdentificacion { get; set; }
    public string Cantidad { get; set; }
    public string Unidad { get; set; }
    public string Descripcion { get; set; }
    public string ValorUnitario { get; set; }
    public string Importe { get; set; }

}


public class TimbreFiscalDigital
{
    public string Version { get; set; }
    public string UUID { get; set; }
    public string FechaTimbrado { get; set; }
    public string SelloCFD { get; set; }
    public string NoCertificadoSAT { get; set; }
    public string SelloSAT { get; set; }
}

