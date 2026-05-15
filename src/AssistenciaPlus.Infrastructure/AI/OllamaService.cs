using AssistenciaPlus.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssistenciaPlus.Infrastructure.AI;

public class OllamaSettings
{
    public string Url { get; set; } = "http://ollama:11434";
    public string Model { get; set; } = "llama3.2";
    public int TimeoutSeconds { get; set; } = 120;
}

/// <summary>
/// Servei d'IA local usant Ollama per a consultes en llenguatge natural.
/// Només realitza consultes de lectura sobre les dades proporcionades.
/// </summary>
public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaService> _logger;

    private const string SystemPrompt = """
        Ets un assistent de consultes per a un sistema de gestió d'assistència escolar anomenat AssistenciaPlus.
        Tens accés a dades d'assistència en format JSON.
        
        REGLES IMPORTANTS:
        - Respon SEMPRE en el mateix idioma en què et pregunten (català o castellà).
        - Proporciona respostes concises i útils.
        - Pots calcular estadístiques, percentatges i tendències a partir de les dades.
        - No inventis dades que no estiguin en el context proporcionat.
        - Si no pots respondre amb les dades disponibles, indica-ho clarament.
        - Pots suggerir exportar els resultats si la consulta és complexa.
        - NO modifiquis ni suggereixis modificar cap dada.
        """;

    public OllamaService(
        HttpClient httpClient,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.BaseAddress = new Uri(_settings.Url);
        _logger = logger;
    }

    public async Task<string> QueryAsync(
        string naturalLanguageQuery, string dataContext, CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new
                    {
                        role = "user",
                        content = $"""
                            Context de dades (JSON):
                            {dataContext}
                            
                            Pregunta: {naturalLanguageQuery}
                            """
                    }
                },
                stream = false,
                options = new { temperature = 0.1, top_p = 0.9 }
            };

            var response = await _httpClient.PostAsJsonAsync("/api/chat", request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "No s'ha pogut obtenir resposta.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultant Ollama per query: {Query}", naturalLanguageQuery);
            return "Error: No s'ha pogut processar la consulta. Comprova que el servei d'IA està disponible.";
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
