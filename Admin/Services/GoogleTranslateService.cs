using System.Text;
using System.Text.Json;

namespace Admin.Services;

public class GoogleTranslateService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GoogleTranslateService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GoogleTranslate:ApiKey"]
            ?? throw new InvalidOperationException("Chưa cấu hình GoogleTranslate:ApiKey trong appsettings.json");
    }

    /// <summary>Dịch văn bản từ ngôn ngữ nguồn sang ngôn ngữ đích.</summary>
    public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang)
    {
        var url = $"https://translation.googleapis.com/language/translate/v2?key={_apiKey}";

        var body = new
        {
            q = text,
            source = sourceLang,
            target = targetLang,
            format = "text"
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = TryExtractErrorMessage(responseBody);
            throw new Exception($"Google Translate API lỗi ({(int)response.StatusCode}): {errorMsg}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var translations = doc.RootElement
            .GetProperty("data")
            .GetProperty("translations");

        if (translations.GetArrayLength() == 0)
            throw new Exception("Google Translate không trả về kết quả dịch.");

        return translations[0].GetProperty("translatedText").GetString()!;
    }

    /// <summary>Lấy tên bản địa của ngôn ngữ theo mã code. Trả null nếu mã không hợp lệ.</summary>
    public async Task<string?> GetLanguageNameAsync(string languageCode, string displayLang = "vi")
    {
        var url = $"https://translation.googleapis.com/language/translate/v2/languages?key={_apiKey}&target={displayLang}";

        var response = await _httpClient.GetAsync(url);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = TryExtractErrorMessage(responseBody);
            throw new Exception($"Google Translate API lỗi ({(int)response.StatusCode}): {errorMsg}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var languages = doc.RootElement
            .GetProperty("data")
            .GetProperty("languages");

        foreach (var lang in languages.EnumerateArray())
        {
            var code = lang.GetProperty("language").GetString();
            if (string.Equals(code, languageCode, StringComparison.OrdinalIgnoreCase))
            {
                return lang.GetProperty("name").GetString();
            }
        }

        return null; // Mã ngôn ngữ không tồn tại
    }

    /// <summary>Kiểm tra mã ngôn ngữ có hợp lệ hay không.</summary>
    public async Task<bool> IsValidLanguageCodeAsync(string code)
    {
        var name = await GetLanguageNameAsync(code);
        return name != null;
    }

    private static string TryExtractErrorMessage(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var errorObj))
            {
                var message = errorObj.GetProperty("message").GetString();
                var status = errorObj.GetProperty("status").GetString();
                return $"{status} – {message}";
            }
        }
        catch { }

        return responseBody.Length > 200 ? responseBody[..200] : responseBody;
    }
}
