using AssistenciaPlus.Application.DTOs;
using ClosedXML.Excel;

namespace AssistenciaPlus.Infrastructure.Excel;

/// <summary>
/// Genera fitxers Excel d'informes d'assistència a partir dels DTOs del domini català.
/// </summary>
public class InformesExcelService
{
    private static readonly XLColor BlauEscola = XLColor.FromHtml("#003F8A");
    private static readonly XLColor VerdeOk = XLColor.FromHtml("#E8F5E9");
    private static readonly XLColor TaronjaAlerta = XLColor.FromHtml("#FFF8E1");
    private static readonly XLColor VermelCritic = XLColor.FromHtml("#FFEBEE");

    public byte[] ExportarInformeMensual(InformeMensualDto informe)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Informe mensual");

        var nomMes = new System.Globalization.CultureInfo("ca-ES")
            .DateTimeFormat.GetMonthName(informe.Mes);
        var titol = $"Informe d'assistència — {informe.GrupNom ?? informe.CicleNom} — {nomMes} {informe.Any}";

        EscriureCapcalera(ws, titol, informe.AnyAcademic, informe.TotalAlumnes);
        EscriureTaulaAlumnes(ws, informe.Alumnes, fila: 5);

        ws.Columns().AdjustToContents();
        ws.Column(1).Width = 32;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportarInformeTrimestral(InformeTrimestralDto informe)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Informe trimestral");

        var titol = $"Informe d'assistència — {informe.GrupNom ?? informe.CicleNom} — {informe.Trimestre}r trimestre";
        var periode = $"{informe.DataInici:dd/MM/yyyy} – {informe.DataFi:dd/MM/yyyy}";

        EscriureCapcalera(ws, titol, $"{informe.AnyAcademic} · {periode}", informe.TotalAlumnes);
        EscriureTaulaAlumnes(ws, informe.Alumnes, fila: 5);

        ws.Columns().AdjustToContents();
        ws.Column(1).Width = 32;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private void EscriureCapcalera(IXLWorksheet ws, string titol, string subTitol, int totalAlumnes)
    {
        // Títol
        ws.Cell(1, 1).Value = titol;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Font.FontColor = BlauEscola;
        ws.Range(1, 1, 1, 8).Merge();

        // Subtítol
        ws.Cell(2, 1).Value = subTitol;
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Range(2, 1, 2, 8).Merge();

        // Total alumnes
        ws.Cell(3, 1).Value = $"Total alumnes: {totalAlumnes}";
        ws.Cell(3, 1).Style.Font.Bold = true;
        ws.Range(3, 1, 3, 8).Merge();
    }

    private void EscriureTaulaAlumnes(IXLWorksheet ws, List<InformeAlumneDto> alumnes, int fila)
    {
        // Capçaleres de la taula
        var caps = new[] { "Alumne", "Grup", "Presents", "Absents", "Tard", "Parcial", "% Absència", "Estat" };
        for (int c = 0; c < caps.Length; c++)
        {
            var cell = ws.Cell(fila, c + 1);
            cell.Value = caps[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = BlauEscola;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        }

        fila++;
        foreach (var a in alumnes.OrderByDescending(x => x.PercentatgeAbsencia))
        {
            var color = a.EsCritic ? VermelCritic : a.EsAlerta ? TaronjaAlerta : VerdeOk;
            var rang = ws.Range(fila, 1, fila, 8);
            rang.Style.Fill.BackgroundColor = color;

            ws.Cell(fila, 1).Value = a.AlumneNom;
            ws.Cell(fila, 2).Value = a.GrupNom;
            ws.Cell(fila, 3).Value = a.FrangesPresents;
            ws.Cell(fila, 4).Value = a.FrangesAbsents;
            ws.Cell(fila, 5).Value = a.FrangesTard;
            ws.Cell(fila, 6).Value = a.FrangesAbsenciaParcial;

            var pctCell = ws.Cell(fila, 7);
            pctCell.Value = (double)a.PercentatgeAbsencia / 100;
            pctCell.Style.NumberFormat.Format = "0.0%";
            pctCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            if (a.EsCritic) pctCell.Style.Font.Bold = true;

            var estatCell = ws.Cell(fila, 8);
            estatCell.Value = a.EsCritic ? "⚠ Crític" : a.EsAlerta ? "! Alerta" : "✓ Correcte";
            estatCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Vora inferior lleugera
            rang.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
            rang.Style.Border.BottomBorderColor = XLColor.LightGray;

            fila++;
        }

        // Vora exterior de la taula
        ws.Range(fila - alumnes.Count, 1, fila - 1, 8)
            .Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Range(fila - alumnes.Count, 1, fila - 1, 8)
            .Style.Border.OutsideBorderColor = BlauEscola;

        // Congelar fila de capçaleres
        ws.SheetView.FreezeRows(5);
    }
}
