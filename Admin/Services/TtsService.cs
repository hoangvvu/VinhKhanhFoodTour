using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Admin.Services;

public class TtsResult
{
    public string? Url { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// TTS qua FPT.AI: gọi API tạo URL, sau đó tải MP3 về UploadsData (không lưu URL FPT vào DB — link thường ngắn hạn).
/// </summary>
public class TtsService
{
    public const string HttpClientFptApi = "FptTtsApi";
    public const string HttpClientFptDownload = "FptTtsDownload";

    private readonly IHttpClientFactory _httpFactory;
    private readonly IWebHostEnvironment _env;
    private readonly string _apiKey;

    public TtsService(IHttpClientFactory httpFactory, IWebHostEnvironment env, IConfiguration configuration)
    {
        _httpFactory = httpFactory;
        _env = env;
        _apiKey = configuration["FptAi:Tts:ApiKey"]
                  ?? Environment.GetEnvironmentVariable("FPT_AI_TTS_API_KEY")
                  ?? string.Empty;
    }

    public async Task<TtsResult> GenerateAudioAsync(string text, string voiceCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return new TtsResult
            {
                ErrorMessage = "Chưa cấu hình FPT TTS: thêm FptAi:Tts:ApiKey trong User Secrets / appsettings hoặc biến môi trường FPT_AI_TTS_API_KEY."
            };

        if (string.IsNullOrWhiteSpace(text))
            return new TtsResult { ErrorMessage = "Nội dung mô tả đang trống." };

        var client = _httpFactory.CreateClient(HttpClientFptApi);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.fpt.ai/hmi/tts/v5");
        request.Headers.TryAddWithoutValidation("api-key", _apiKey);
        request.Headers.TryAddWithoutValidation("voice", voiceCode);
        request.Content = new StringContent(text, Encoding.UTF8, "text/plain");

        using var response = await client.SendAsync(request, cancellationToken);
        var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[TTS] FPT API HTTP {(int)response.StatusCode}: {jsonString}");
            return new TtsResult { ErrorMessage = string.IsNullOrWhiteSpace(jsonString) ? $"FPT API lỗi {(int)response.StatusCode}." : jsonString };
        }

        var audioUrl = TryExtractAudioUrl(jsonString);
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            Console.WriteLine($"[TTS] Không đọc được audio url từ FPT: {jsonString}");
            return new TtsResult { ErrorMessage = "FPT trả về thành công nhưng không có đường dẫn audio hợp lệ." };
        }

        return new TtsResult { Url = audioUrl };
    }

    /// <summary>Gọi FPT TTS rồi tải file về thư mục UploadsData/narration. Chỉ trả về URL /uploads/... khi đã có file trên đĩa.</summary>
    public async Task<TtsResult> GenerateAndPersistLocalAsync(string text, string voiceCode, string fileNameStem, CancellationToken cancellationToken = default)
    {
        var remote = await GenerateAudioAsync(text, voiceCode, cancellationToken);
        if (!string.IsNullOrEmpty(remote.ErrorMessage) || string.IsNullOrWhiteSpace(remote.Url))
            return remote;

        // FPT thường cần vài giây (đôi khi 10–20s) mới phục vụ file ổn định trên CDN.
        await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);

        byte[] bytes;
        try
        {
            bytes = await DownloadAudioWithRetryAsync(remote.Url!, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TTS] Tải MP3 từ FPT thất bại: {ex}");
            return new TtsResult
            {
                ErrorMessage =
                    "Không tải được file âm thanh từ FPT sau nhiều lần thử (file có thể chưa sẵn sàng hoặc mạng/CDN chặn). " +
                    "Hãy bấm tạo lại sau 15–30 giây. Chi tiết: " + ex.Message
            };
        }

        try
        {
            var dir = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "UploadsData", "narration"));
            Directory.CreateDirectory(dir);
            var safeStem = string.Join("_", fileNameStem.Split(Path.GetInvalidFileNameChars()));
            var fn = $"{safeStem}_{Guid.NewGuid():N}.mp3";
            var fullPath = Path.Combine(dir, fn);
            await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
            return new TtsResult { Url = $"/uploads/narration/{fn}" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TTS] Ghi file local thất bại: {ex}");
            return new TtsResult { ErrorMessage = $"Không ghi được file trên máy chủ: {ex.Message}" };
        }
    }

    private async Task<byte[]> DownloadAudioWithRetryAsync(string remoteUrl, CancellationToken cancellationToken)
    {
        var client = _httpFactory.CreateClient(HttpClientFptDownload);
        const int maxAttempts = 28;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, remoteUrl);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound ||
                    response.StatusCode == HttpStatusCode.RequestTimeout ||
                    (int)response.StatusCode == 429)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (attempt >= maxAttempts - 2)
                        throw new HttpRequestException($"HTTP {(int)response.StatusCode} khi tải audio: {body.AsSpan(0, Math.Min(200, body.Length))}…");

                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, cancellationToken);
                var bytes = ms.ToArray();

                if (bytes.Length < 128)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                if (!LooksLikeMp3OrBinaryAudio(bytes))
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                return bytes;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
            catch (HttpRequestException)
            {
                if (attempt == maxAttempts)
                    throw;
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
        }

        throw new IOException($"Không tải được file audio từ FPT sau {maxAttempts} lần thử.");
    }

    private static async Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        // 2s, 3s, 4s … tối đa ~14s mỗi lần — tổng thời gian chờ đủ cho CDN FPT.
        var seconds = Math.Min(2 + attempt, 14);
        await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
    }

    private static bool LooksLikeMp3OrBinaryAudio(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 3 && b[0] == (byte)'I' && b[1] == (byte)'D' && b[2] == (byte)'3')
            return true;
        if (b.Length >= 2 && b[0] == 0xFF && (b[1] & 0xE0) == 0xE0)
            return true;

        var start = 0;
        while (start < b.Length && start < 32 && b[start] is (byte)' ' or (byte)'\r' or (byte)'\n' or (byte)'\t')
            start++;
        if (start >= b.Length)
            return false;

        var take = Math.Min(b.Length - start, 256);
        var head = Encoding.ASCII.GetString(b.Slice(start, take));
        if (head.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            head.Contains("<html", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static string? TryExtractAudioUrl(string jsonString)
    {
        using var jsonDoc = JsonDocument.Parse(jsonString);
        var root = jsonDoc.RootElement;

        if (root.TryGetProperty("async", out var asyncProp) && asyncProp.ValueKind == JsonValueKind.String)
            return asyncProp.GetString();

        if (root.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
            return urlProp.GetString();

        if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Object)
        {
            if (dataProp.TryGetProperty("async", out var nestedAsync) && nestedAsync.ValueKind == JsonValueKind.String)
                return nestedAsync.GetString();
            if (dataProp.TryGetProperty("url", out var nestedUrl) && nestedUrl.ValueKind == JsonValueKind.String)
                return nestedUrl.GetString();
        }

        return null;
    }
}
