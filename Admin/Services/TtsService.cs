using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace Admin.Services;

public class TtsResult
{
    public string? Url { get; set; }
    public string? ErrorMessage { get; set; }
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
            var jsonDoc = JsonDocument.Parse(jsonString);

            // FPT trả về link file audio đã được host sẵn
            var audioUrl = jsonDoc.RootElement.GetProperty("async").GetString();

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
            var bytes = await _httpClient.GetByteArrayAsync(remote.Url);
            var dir = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "UploadsData", "narration"));
            Directory.CreateDirectory(dir);
            var safeStem = string.Join("_", fileNameStem.Split(Path.GetInvalidFileNameChars()));
            var fn = $"{safeStem}_{Guid.NewGuid():N}.mp3";
            await File.WriteAllBytesAsync(Path.Combine(dir, fn), bytes);
            return new TtsResult { Url = $"/uploads/narration/{fn}" };
        }
        catch (Exception ex)
        {
            return new TtsResult { Url = remote.Url, ErrorMessage = $"Đã tạo trên FPT nhưng lưu file thất bại: {ex.Message}" };
        }
    }
}