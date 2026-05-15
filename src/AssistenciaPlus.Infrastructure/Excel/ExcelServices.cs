using AssistenciaPlus.Core.Entities;
using AssistenciaPlus.Core.Interfaces;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace AssistenciaPlus.Infrastructure.Excel;

/// <summary>
/// Importació d'alumnes des del format Excel exportat per Esfera (gestió Generalitat).
/// Les columnes s'adapten al format real un cop es disposi del fitxer de mostra.
/// </summary>
public class ExcelImportService : IExcelImportService
{
    private readonly ILogger<ExcelImportService> _logger;

    // Noms de columnes esperats a l'Excel d'Esfera (ajustar amb el fitxer real)
    private static class EsferaColumns
    {
        public const string LastName = "Cognom 1";
        public const string SecondLastName = "Cognom 2";
        public const string FirstName = "Nom";
        public const string IdNumber = "NIA";
        public const string Email = "Correu electrònic";
        public const string Phone = "Telèfon";
    }

    public ExcelImportService(ILogger<ExcelImportService> logger)
    {
        _logger = logger;
    }

    public async Task<ExcelImportResult<Student>> ImportStudentsFromEsferaAsync(
        Stream fileStream, Guid groupId, CancellationToken ct = default)
    {
        var result = new ExcelImportResult<Student>();
        var imported = new List<Student>();
        var errors = new List<ExcelImportError>();

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.First();
            var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1).ToList() ?? [];

            int rowNumber = 2;
            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var lastName = GetCellValue(row, EsferaColumns.LastName, worksheet);
                    var firstName = GetCellValue(row, EsferaColumns.FirstName, worksheet);

                    if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                    {
                        errors.Add(new ExcelImportError(rowNumber, "Nom/Cognom", "Nom o cognom buit"));
                        rowNumber++;
                        continue;
                    }

                    var student = new Student
                    {
                        FirstName = firstName.Trim(),
                        LastName = lastName.Trim(),
                        SecondLastName = GetCellValue(row, EsferaColumns.SecondLastName, worksheet)?.Trim(),
                        IdNumber = GetCellValue(row, EsferaColumns.IdNumber, worksheet)?.Trim(),
                        Email = GetCellValue(row, EsferaColumns.Email, worksheet)?.Trim().ToLowerInvariant(),
                        Phone = GetCellValue(row, EsferaColumns.Phone, worksheet)?.Trim(),
                        GroupId = groupId,
                        IsActive = true
                    };

                    imported.Add(student);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error important fila {Row}", rowNumber);
                    errors.Add(new ExcelImportError(rowNumber, "General", ex.Message));
                }

                rowNumber++;
            }
        }, ct);

        return result with
        {
            Imported = imported,
            Errors = errors,
            TotalRows = imported.Count + errors.Count
        };
    }

    public async Task<ExcelImportResult<Schedule>> ImportScheduleAsync(
        Stream fileStream, Guid academicYearId, CancellationToken ct = default)
    {
        // Implementació pendent del format de fitxer d'horaris
        // Es completarà quan es disposi del fitxer de mostra
        _logger.LogInformation("ImportScheduleAsync: pendent del format de fitxer");
        await Task.CompletedTask;
        return new ExcelImportResult<Schedule>
        {
            TotalRows = 0,
            Imported = [],
            Errors = [new ExcelImportError(0, "N/A", "Funcionalitat pendent del format de fitxer d'horaris")]
        };
    }

    private static string? GetCellValue(IXLRow row, string columnName, IXLWorksheet worksheet)
    {
        // Buscar la columna per nom a la primera fila
        var headerRow = worksheet.Row(1);
        var col = headerRow.Cells().FirstOrDefault(c =>
            c.Value.ToString().Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));

        if (col == null) return null;

        var cell = row.Cell(col.Address.ColumnNumber);
        var value = cell.Value.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

/// <summary>
/// Exportació d'informes d'assistència a Excel.
/// </summary>
public class ExcelExportService : IExcelExportService
{
    public async Task<byte[]> ExportAttendanceReportAsync(
        AttendanceReportFilter filter, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Informe d'assistència");

            // Capçaleres
            var headers = new[]
            {
                "Alumne", "Grup", "Data", "Sessió", "Matèria",
                "Professor", "Estat", "Justificat", "Observació"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976D2");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Aquí s'omplirà amb les dades de la consulta
            // (implementat al servei d'assistència que injecta les dades)

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }, ct);
    }
}
