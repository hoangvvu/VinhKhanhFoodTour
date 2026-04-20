using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace Admin.Services;

public class TtsResult
{
    public string? Url { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Dịch vụ TTS cục bộ dùng Edge/Google và lưu file về UploadsData.
/// </summary>
public class TtsService
{
    private readonly IWebHostEnvironment _env;
    private readonly EdgeTtsService _edgeTts;

    public TtsService(IWebHostEnvironment env, EdgeTtsService edgeTts)
    {
        _env = env;
        _edgeTts = edgeTts;
    }

    public async Task<TtsResult> GenerateAudioAsync(string text, string voiceCode, CancellationToken cancellationToken = default)
    {
        return new TtsResult { ErrorMessage = "Hệ thống không dùng URL TTS remote. Hãy gọi GenerateAndPersistLocalAsync." };
    }

    /// <summary>Gọi Edge TTS rồi lưu file về UploadsData/narration.</summary>
    public async Task<TtsResult> GenerateAndPersistLocalAsync(string text, string voiceCode, string fileNameStem, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        byte[] bytes;
        var normalizedText = NormalizeInputText(text);
        if (string.IsNullOrWhiteSpace(normalizedText))
            return new TtsResult { ErrorMessage = "Nội dung mô tả đang trống." };

        try
        {
            bytes = await _edgeTts.SynthesizeAsync(normalizedText, voiceCode, cancellationToken);
            if (bytes == null || bytes.Length == 0)
            {
                var fallbackVoice = GetPreferredEdgeVoice(languageCode);
                if (!string.Equals(fallbackVoice, voiceCode, StringComparison.OrdinalIgnoreCase))
                    bytes = await _edgeTts.SynthesizeAsync(normalizedText, fallbackVoice, cancellationToken);
            }

            if (bytes == null || bytes.Length == 0)
                return new TtsResult { ErrorMessage = "Edge TTS không trả về dữ liệu âm thanh." };
        }
        catch (Exception ex)
        {
            return new TtsResult { ErrorMessage = $"Lỗi Edge TTS: {ex.Message}" };
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


    private static string NormalizeInputText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new System.Text.StringBuilder(input.Length);

        foreach (var ch in input)
        {
            switch (ch)
            {
                // ── Xuống dòng: chuẩn hóa về \n ────────────────────────
                case '\r':
                    // \r\n được xử lý khi gặp \n tiếp theo → bỏ qua \r đơn lẻ
                    continue;

                // ── Các dạng space đặc biệt → space thường ─────────────
                case '\u00A0': // Non-breaking space (NBSP) — hay gặp nhất khi copy từ SSMS/Excel
                case '\u202F': // Narrow no-break space
                case '\u2007': // Figure space
                case '\u2009': // Thin space
                case '\u200A': // Hair space
                case '\u3000': // Ideographic space (CJK)
                    sb.Append(' ');
                    break;

                // ── Ký tự zero-width / invisible → bỏ hẳn ─────────────
                case '\uFEFF': // BOM (Byte Order Mark) — xuất hiện ở đầu text copy từ SSMS
                case '\u200B': // Zero-width space
                case '\u200C': // Zero-width non-joiner
                case '\u200D': // Zero-width joiner
                case '\u2060': // Word joiner
                case '\u00AD': // Soft hyphen
                    // Bỏ qua hoàn toàn
                    break;

                // ── Smart quotes → ASCII thường ─────────────────────────
                case '\u201C': // "
                case '\u201D': // "
                case '\u201E': // „
                    sb.Append('"');
                    break;
                case '\u2018': // '
                case '\u2019': // '
                case '\u201A': // ‚
                    sb.Append('\'');
                    break;

                // ── Dấu gạch đặc biệt → hyphen thường ─────────────────
                case '\u2013': // En dash –
                case '\u2014': // Em dash —
                case '\u2015': // Horizontal bar ―
                    sb.Append('-');
                    break;

                // ── Ký tự điều khiển (ASCII < 32, trừ \t và \n) ────────
                default:
                    if (ch < 32 && ch != '\t' && ch != '\n')
                        break; // bỏ qua
                    // Ký tự Unicode điều khiển khác (U+0080–U+009F)
                    if (ch >= '\u0080' && ch <= '\u009F')
                        break;
                    sb.Append(ch);
                    break;
            }
        }

        // Sau khi xử lý từng ký tự: chuẩn hóa xuống dòng \r\n → \n
        return sb.ToString()
                 .Replace("\r\n", "\n")
                 .Replace('\r', '\n')
                 .Trim();
    }

    private static string GetPreferredEdgeVoice(string? languageCode)
    {
        return (languageCode ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "vi" => "vi-VN-HoaiMyNeural",
            "en" => "en-US-AriaNeural",
            "ja" => "ja-JP-NanamiNeural",
            "ko" => "ko-KR-SunHiNeural",
            "zh" => "zh-CN-XiaoxiaoNeural",
            "zh-cn" => "zh-CN-XiaoxiaoNeural",
            "zh-tw" => "zh-TW-HsiaoChenNeural",
            "fr" => "fr-FR-DeniseNeural",
            "de" => "de-DE-KatjaNeural",
            "es" => "es-ES-ElviraNeural",
            "th" => "th-TH-PremwadeeNeural",
            _ => "vi-VN-HoaiMyNeural"
        };
    }

}
