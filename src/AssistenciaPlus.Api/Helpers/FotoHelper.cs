using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace AssistenciaPlus.Api.Helpers;

public static class FotoHelper
{
    private const int MidaMaxPx = 400;
    private const int QualitatiJpeg = 85;

    /// <summary>
    /// Llegeix el stream, redimensiona fins a MidaMaxPx×MidaMaxPx mantenint proporcions,
    /// i desa com JPEG al fitxer de destinació. Retorna la mida final en bytes.
    /// </summary>
    public static async Task<long> ResitzarIGuardarAsync(
        Stream origen, string rutaDesti, CancellationToken ct = default)
    {
        Image imatge;
        try
        {
            imatge = await Image.LoadAsync(origen, ct);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new ArgumentException("El fitxer no és una imatge vàlida o el format no està suportat.");
        }

        using (imatge)
        {
            if (imatge.Width > MidaMaxPx || imatge.Height > MidaMaxPx)
                imatge.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(MidaMaxPx, MidaMaxPx),
                    Mode = ResizeMode.Max
                }));

            var encoder = new JpegEncoder { Quality = QualitatiJpeg };
            await imatge.SaveAsJpegAsync(rutaDesti, encoder, ct);
        }

        return new FileInfo(rutaDesti).Length;
    }
}
