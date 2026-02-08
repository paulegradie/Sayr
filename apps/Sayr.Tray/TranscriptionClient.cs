using System.Net.Http.Headers;
using System.Text.Json;

namespace Sayr.Tray;

internal sealed class TranscriptionClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _model;

    public TranscriptionClient(HttpClient http, string baseUrl, string model)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
    }

    public async Task<string> TranscribeAsync(byte[] wavData, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(wavData);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "file", "audio.wav");
        content.Add(new StringContent(_model), "model");

        using var response = await _http.PostAsync($"{_baseUrl}/v1/audio/transcriptions", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Transcription failed: {response.StatusCode} {body}");
        }

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("text", out var text))
        {
            return text.GetString() ?? string.Empty;
        }

        throw new InvalidOperationException("Transcription response missing 'text' field.");
    }
}
