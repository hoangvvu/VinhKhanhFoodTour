using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using System.Net;

namespace Admin.Services;

public class TtsResult
{
    public string? Url { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsFallbackRemoteUrl { get; set; }
}

public class TtsService
{
    private readonly HttpClient _httpClient;
    private readonly IWebHostEnvironment _env;

    // Dán mã API Key của FPT.AI mà bạn vừa copy vào đây
    private readonly string _apiKey = "PtJ2wolAlCHog4FgK48FsaJOAtwxNHeG";

    public TtsService(HttpClient httpClient, IWebHostEnvironment env)
    {
        _httpClient = httpClient;
        _env = env;
    }

    public async Task<TtsResult> GenerateAudioAsync(string text, string voiceCode)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TtsResult { ErrorMessage = "Nội dung mô tả đang trống." };

        // voiceCode của FPT: banmai (nữ miền Nam), lannhi (nữ miền Nam), thuquynh (nữ miền Bắc)...
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.fpt.ai/hmi/tts/v5");
        request.Headers.Add("api-key", _apiKey);
        request.Headers.Add("voice", voiceCode);

        // FPT yêu cầu gửi text trực tiếp trong Body
        request.Content = new StringContent(text, Encoding.UTF8, "text/plain");

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            var audioUrl = TryExtractAudioUrl(jsonString);
            if (string.IsNullOrWhiteSpace(audioUrl))
            {
                Console.WriteLine($"[TTS] Không đọc được audio url từ phản hồi FPT: {jsonString}");
                return new TtsResult
                {
                    ErrorMessage = "FPT trả về thành công nhưng không có đường dẫn audio hợp lệ."
                };
            }

            // FPT cần khoảng 1-3 giây để sinh file trên server của họ
            // Mình delay nhẹ 2 giây để đảm bảo khi hiện lên giao diện là phát được ngay
            await Task.Delay(2000);

            return new TtsResult { Url = audioUrl };
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Lỗi API FPT: {error}");
            return new TtsResult { ErrorMessage = error };
        }
    }

    /// <summary>Gọi FPT TTS rồi tải file về thư mục UploadsData/narration để lưu lâu dài.</summary>
    public async Task<TtsResult> GenerateAndPersistLocalAsync(string text, string voiceCode, string fileNameStem)
    {
        var remote = await GenerateAudioAsync(text, voiceCode);
        if (!string.IsNullOrEmpty(remote.ErrorMessage) || string.IsNullOrEmpty(remote.Url))
            return remote;

        try
        {
            var bytes = await DownloadAudioWithRetryAsync(remote.Url);
            var dir = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "UploadsData", "narration"));
            Directory.CreateDirectory(dir);
            var safeStem = string.Join("_", fileNameStem.Split(Path.GetInvalidFileNameChars()));
            var fn = $"{safeStem}_{Guid.NewGuid():N}.mp3";
            await File.WriteAllBytesAsync(Path.Combine(dir, fn), bytes);
            return new TtsResult { Url = $"/uploads/narration/{fn}" };
        }
        catch (Exception ex)
        {
            // Fallback: vẫn dùng URL FPT để hệ thống nghe được ngay, không chặn luồng nghiệp vụ.
            Console.WriteLine($"[TTS] Lưu local thất bại, fallback URL FPT: {ex.Message}");
            return new TtsResult
            {
                Url = remote.Url,
                IsFallbackRemoteUrl = true
            };
        }
    }

    private async Task<byte[]> DownloadAudioWithRetryAsync(string remoteUrl)
    {
        const int maxAttempts = 12;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await _httpClient.GetByteArrayAsync(remoteUrl);
            }
            catch (HttpRequestException ex)
            {
                var status = ex.StatusCode;
                var transient = status == HttpStatusCode.NotFound ||
                                status == HttpStatusCode.Forbidden ||
                                status == HttpStatusCode.TooManyRequests ||
                                status == null;
                if (attempt == maxAttempts)
                    throw;
                if (!transient)
                    throw;

                await Task.Delay(1200 * attempt);
            }
        }

        throw new InvalidOperationException("Không thể tải file audio từ FPT sau nhiều lần thử.");
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