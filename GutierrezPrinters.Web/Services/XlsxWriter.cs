using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace GutierrezPrinters.Web.Services;

// Genera un archivo de Excel (.xlsx) sencillo sin librerías externas.
// Un .xlsx es un .zip con varios archivos XML adentro; aquí se arman a mano.
public class XlsxWriter
{
    private readonly List<(string Name, string[] Headers, List<object?[]> Rows)> _sheets = new();

    public void AddSheet(string name, string[] headers, IEnumerable<object?[]> rows) =>
        _sheets.Add((name, headers, rows.ToList()));

    public byte[] Build()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(zip, "[Content_Types].xml", ContentTypes());
            Write(zip, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");
            Write(zip, "xl/workbook.xml", Workbook());
            Write(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
            Write(zip, "xl/styles.xml", Styles());

            for (var i = 0; i < _sheets.Count; i++)
            {
                Write(zip, $"xl/worksheets/sheet{i + 1}.xml", Sheet(_sheets[i].Headers, _sheets[i].Rows));
            }
        }

        return stream.ToArray();
    }

    private static void Write(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private string ContentTypes()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
        sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
        sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
        sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
        sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
        for (var i = 0; i < _sheets.Count; i++)
        {
            sb.Append($"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        }
        sb.Append("</Types>");
        return sb.ToString();
    }

    private string Workbook()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
        for (var i = 0; i < _sheets.Count; i++)
        {
            sb.Append($"<sheet name=\"{Escape(SafeSheetName(_sheets[i].Name))}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
        }
        sb.Append("</sheets></workbook>");
        return sb.ToString();
    }

    private string WorkbookRels()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        for (var i = 0; i < _sheets.Count; i++)
        {
            sb.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
        }
        sb.Append($"<Relationship Id=\"rId{_sheets.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    // Estilo 0 = normal, estilo 1 = negrita (encabezados).
    private static string Styles() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
        "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
        "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs>" +
        "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
        "</styleSheet>";

    private static string Sheet(string[] headers, List<object?[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
        sb.Append("<cols>");
        for (var c = 0; c < headers.Length; c++)
        {
            sb.Append($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"20\" customWidth=\"1\"/>");
        }
        sb.Append("</cols><sheetData>");

        AppendRow(sb, 1, headers.Cast<object?>().ToArray(), style: 1);
        for (var r = 0; r < rows.Count; r++)
        {
            AppendRow(sb, r + 2, rows[r], style: 0);
        }

        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, int rowNumber, object?[] values, int style)
    {
        sb.Append($"<row r=\"{rowNumber}\">");
        for (var c = 0; c < values.Length; c++)
        {
            var reference = ColumnName(c) + rowNumber;
            var styleAttr = style > 0 ? $" s=\"{style}\"" : "";
            switch (values[c])
            {
                case null:
                    break;
                case int or long or decimal or double or float:
                    var number = Convert.ToString(values[c], CultureInfo.InvariantCulture);
                    sb.Append($"<c r=\"{reference}\"{styleAttr}><v>{number}</v></c>");
                    break;
                default:
                    var text = values[c] is bool b ? (b ? "Sí" : "No") : values[c]!.ToString() ?? "";
                    sb.Append($"<c r=\"{reference}\" t=\"inlineStr\"{styleAttr}><is><t xml:space=\"preserve\">{Escape(text)}</t></is></c>");
                    break;
            }
        }
        sb.Append("</row>");
    }

    // 0 -> A, 25 -> Z, 26 -> AA ...
    private static string ColumnName(int index)
    {
        var name = "";
        index++;
        while (index > 0)
        {
            var mod = (index - 1) % 26;
            name = (char)('A' + mod) + name;
            index = (index - 1) / 26;
        }
        return name;
    }

    private static string SafeSheetName(string name)
    {
        var clean = new string(name.Where(ch => !"[]:*?/\\".Contains(ch)).ToArray());
        return clean.Length > 31 ? clean[..31] : clean;
    }

    // Escapa caracteres especiales de XML y quita caracteres de control inválidos.
    private static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            switch (ch)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\t' or '\n' or '\r': sb.Append(ch); break;
                default:
                    if (ch >= 0x20)
                    {
                        sb.Append(ch);
                    }
                    break;
            }
        }
        return sb.ToString();
    }
}
