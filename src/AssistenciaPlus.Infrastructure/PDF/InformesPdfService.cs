using AssistenciaPlus.Application.DTOs;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace AssistenciaPlus.Infrastructure.PDF;

/// <summary>
/// Genera fitxers PDF d'informes individuals d'assistència per alumne.
/// </summary>
public class InformesPdfService
{
    private const double MargeLateral = 50;
    private const double AmpleA4 = 595;
    private const double AltA4 = 842;

    public byte[] GenerarInformeAlumne(
        InformeAlumneDto alumne,
        string periodeNom,
        string anyAcademic)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        var fontNegreta = builder.AddStandard14Font(Standard14Font.HelveticaBold);
        var fontNormal = builder.AddStandard14Font(Standard14Font.Helvetica);

        double y = AltA4 - 50;

        // Capçalera verda
        page.SetTextAndFillColor(46, 125, 50);
        page.AddText("AssistenciaPlus", 18, new PdfPoint(MargeLateral, y), fontNegreta);
        y -= 16;
        page.SetTextAndFillColor(113, 128, 150);
        page.AddText("Escola Marta Mata · Viladecans", 9, new PdfPoint(MargeLateral, y), fontNormal);
        y -= 5;

        // Línia separadora
        page.SetStrokeColor(200, 200, 200);
        page.DrawLine(new PdfPoint(MargeLateral, y), new PdfPoint(AmpleA4 - MargeLateral, y));
        y -= 22;

        // Títol de l'informe
        page.SetTextAndFillColor(46, 125, 50);
        page.AddText("Informe d'assistència individual", 14, new PdfPoint(MargeLateral, y), fontNegreta);
        y -= 22;

        // Dades de l'alumne
        page.SetTextAndFillColor(0, 0, 0);
        y = EscriureCampNegreta(page, fontNegreta, fontNormal, "Alumne:", alumne.AlumneNom, y);
        y = EscriureCampNegreta(page, fontNegreta, fontNormal, "Grup:", alumne.GrupNom, y);
        y = EscriureCampNegreta(page, fontNegreta, fontNormal, "Any acadèmic:", anyAcademic, y);
        y = EscriureCampNegreta(page, fontNegreta, fontNormal, "Període:", periodeNom, y);
        y = EscriureCampNegreta(page, fontNegreta, fontNormal, "Data generació:",
            DateTime.Now.ToString("dd/MM/yyyy HH:mm"), y);
        y -= 10;

        // Línia separadora
        page.SetStrokeColor(230, 230, 230);
        page.DrawLine(new PdfPoint(MargeLateral, y), new PdfPoint(AmpleA4 - MargeLateral, y));
        y -= 20;

        // Resum d'assistència
        page.SetTextAndFillColor(46, 125, 50);
        page.AddText("Resum d'assistència", 12, new PdfPoint(MargeLateral, y), fontNegreta);
        y -= 18;

        // Taula de resum
        var stats = new (string Label, string Valor, bool Destacat)[]
        {
            ("Franges lectives possibles", alumne.TotalFrangesPossibles.ToString(), false),
            ("Presents",                   alumne.FrangesPresents.ToString(), false),
            ("Absents",                    alumne.FrangesAbsents.ToString(), alumne.FrangesAbsents > 0),
            ("Tard",                       alumne.FrangesTard.ToString(), alumne.FrangesTard > 0),
            ("Absència parcial",           alumne.FrangesAbsenciaParcial.ToString(), false),
            ("% Absència",                 $"{alumne.PercentatgeAbsencia:F1}%", alumne.EsAlerta),
        };

        foreach (var (label, valor, destacat) in stats)
        {
            page.SetTextAndFillColor(80, 80, 80);
            page.AddText(label, 10, new PdfPoint(MargeLateral + 10, y), fontNormal);

            if (destacat)
                page.SetTextAndFillColor(198, 40, 40);
            else
                page.SetTextAndFillColor(0, 0, 0);

            page.AddText(valor, 10, new PdfPoint(MargeLateral + 200, y), fontNegreta);
            y -= 14;
        }

        // Estat general
        y -= 4;
        string estatText;
        byte r, g, b;
        if (alumne.EsCritic)
        {
            estatText = "⚠  Situació crítica (absència superior al llindar)";
            (r, g, b) = (198, 40, 40);
        }
        else if (alumne.EsAlerta)
        {
            estatText = "!  En alerta (absència elevada)";
            (r, g, b) = (245, 127, 23);
        }
        else
        {
            estatText = "✓  Assistència correcta";
            (r, g, b) = (46, 125, 50);
        }
        page.SetTextAndFillColor(r, g, b);
        page.AddText(estatText, 10, new PdfPoint(MargeLateral + 10, y), fontNegreta);
        y -= 20;

        // Motius d'absència (si n'hi ha)
        if (alumne.DetallMotius.Any())
        {
            page.SetStrokeColor(230, 230, 230);
            page.DrawLine(new PdfPoint(MargeLateral, y), new PdfPoint(AmpleA4 - MargeLateral, y));
            y -= 18;

            page.SetTextAndFillColor(46, 125, 50);
            page.AddText("Motius d'absència declarats", 12, new PdfPoint(MargeLateral, y), fontNegreta);
            y -= 18;

            foreach (var (motiu, count) in alumne.DetallMotius.OrderByDescending(kv => kv.Value))
            {
                var motiuText = motiu switch
                {
                    "Metge"          => "Visita mèdica",
                    "NoEsTrobaBe"    => "No es troba bé",
                    "FamiliaVeBuscar" => "La família ve a buscar-lo/a",
                    "ServeiExtern"   => "Servei extern / activitat",
                    "Tard"           => "Retard",
                    _                => "Sense motiu especificat"
                };
                page.SetTextAndFillColor(80, 80, 80);
                page.AddText($"•  {motiuText}", 10, new PdfPoint(MargeLateral + 10, y), fontNormal);
                page.SetTextAndFillColor(0, 0, 0);
                page.AddText(count.ToString(), 10, new PdfPoint(MargeLateral + 200, y), fontNegreta);
                y -= 14;
            }
        }

        // Peu de pàgina
        page.SetStrokeColor(200, 200, 200);
        page.DrawLine(new PdfPoint(MargeLateral, 40), new PdfPoint(AmpleA4 - MargeLateral, 40));
        page.SetTextAndFillColor(160, 160, 160);
        page.AddText(
            "Generat automàticament per AssistenciaPlus · Escola Marta Mata · Viladecans",
            7, new PdfPoint(MargeLateral, 28), fontNormal);

        return builder.Build();
    }

    private static double EscriureCampNegreta(
        PdfPageBuilder page,
        PdfDocumentBuilder.AddedFont fontNegreta,
        PdfDocumentBuilder.AddedFont fontNormal,
        string label, string valor, double y)
    {
        page.SetTextAndFillColor(80, 80, 80);
        page.AddText(label, 10, new PdfPoint(MargeLateral + 10, y), fontNormal);
        page.SetTextAndFillColor(0, 0, 0);
        page.AddText(valor, 10, new PdfPoint(MargeLateral + 130, y), fontNegreta);
        return y - 14;
    }
}
