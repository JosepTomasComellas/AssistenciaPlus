using AssistenciaPlus.Application.DTOs;
using AssistenciaPlus.Domain.Entities;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace AssistenciaPlus.Infrastructure.Services;

/// <summary>
/// Importa alumnes des d'un fitxer Excel format Esfera/Alexia.
/// Columnes esperades (1a fila capçalera): Cognom1, Cognom2, Nom, DataNaixement (opcional).
/// Accepta variacions de noms (majúscules/minúscules, accents, espais).
/// </summary>
public class ImportacioAlumnesExcelService
{
    private readonly ILogger<ImportacioAlumnesExcelService> _logger;

    public ImportacioAlumnesExcelService(ILogger<ImportacioAlumnesExcelService> logger)
    {
        _logger = logger;
    }

    public (List<Alumne> Alumnes, List<string> Errors) ParsearExcel(Stream stream, Guid grupId)
    {
        var alumnes = new List<Alumne>();
        var errors = new List<string>();

        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();

        // Detectar columnes per capçalera
        var capçalera = ws.Row(1);
        int? colCognom1 = null, colCognom2 = null, colNom = null, colData = null;

        foreach (var cel in capçalera.Cells())
        {
            var nom = Normalitzar(cel.GetString());
            switch (nom)
            {
                case "cognom1" or "primer cognom" or "1er cognom" or "primercognom":
                    colCognom1 = cel.Address.ColumnNumber; break;
                case "cognom2" or "segon cognom" or "2on cognom" or "segoncognom":
                    colCognom2 = cel.Address.ColumnNumber; break;
                case "nom" or "name":
                    colNom = cel.Address.ColumnNumber; break;
                case "datanaixement" or "data naixement" or "data de naixement" or "birthdate" or "nascimento":
                    colData = cel.Address.ColumnNumber; break;
            }
        }

        if (colCognom1 == null || colNom == null)
        {
            errors.Add("No s'han trobat les columnes 'Cognom1' i 'Nom'. Revisa el format del fitxer.");
            return (alumnes, errors);
        }

        int fila = 2;
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var cognom1 = row.Cell(colCognom1.Value).GetString().Trim();
            var nom = row.Cell(colNom.Value).GetString().Trim();

            if (string.IsNullOrWhiteSpace(cognom1) && string.IsNullOrWhiteSpace(nom))
            {
                fila++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(cognom1) || string.IsNullOrWhiteSpace(nom))
            {
                errors.Add($"Fila {fila}: nom o cognom1 buit — ignorat.");
                fila++;
                continue;
            }

            DateOnly? dataNaix = null;
            if (colData.HasValue)
            {
                var celData = row.Cell(colData.Value);
                if (!celData.IsEmpty())
                {
                    try
                    {
                        if (celData.DataType == XLDataType.DateTime)
                            dataNaix = DateOnly.FromDateTime(celData.GetDateTime());
                        else if (DateOnly.TryParse(celData.GetString(), out var d))
                            dataNaix = d;
                    }
                    catch
                    {
                        // Ignora data malformada
                    }
                }
            }

            alumnes.Add(new Alumne
            {
                Nom = CapitalitzarNom(nom) ?? nom,
                Cognom1 = CapitalitzarNom(cognom1) ?? cognom1,
                Cognom2 = colCognom2.HasValue
                    ? CapitalitzarNom(row.Cell(colCognom2.Value).GetString().Trim())
                    : null,
                DataNaixement = dataNaix,
                GrupId = grupId,
                EsActiu = true,
                OrdreFusteta = 0,
            });

            fila++;
        }

        _logger.LogInformation("Importació Excel: {Total} alumnes llegits, {Errors} errors",
            alumnes.Count, errors.Count);
        return (alumnes, errors);
    }

    private static string Normalitzar(string s)
        => s.Trim().ToLowerInvariant()
            .Replace("à", "a").Replace("á", "a").Replace("è", "e").Replace("é", "e")
            .Replace("í", "i").Replace("ï", "i").Replace("ó", "o").Replace("ò", "o")
            .Replace("ú", "u").Replace("ü", "u").Replace("ç", "c").Replace("l·l", "l")
            .Replace(" ", "");

    private static string? CapitalitzarNom(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return string.Join(" ", s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Length > 0
                ? char.ToUpper(p[0]) + p[1..].ToLower()
                : p));
    }
}
