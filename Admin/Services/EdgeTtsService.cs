using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Admin.Services;

/// <summary>
/// Dịch vụ Text-To-Speech miễn phí qua Google Translate API.
/// Hỗ trợ đa ngôn ngữ: vi, en, ja, ko, zh-CN...
/// Tự động validate MP3 output và retry khi bị tạm block.
/// </summary>
public class EdgeTtsService
{
    // Giới hạn số byte sau khi URL-encode cho mỗi request.
    // Ký tự Latin: 1 char ≈ 1-3 bytes. Ký tự CJK (Hàn/Trung/Nhật): 1 char ≈ 9 bytes (%XX%XX%XX).
    // Google Translate TTS dễ trả 400 nếu URL quá dài (đặc biệt với tiếng Việt/CJK sau khi encode).
    // Giới hạn bảo thủ để đảm bảo ổn định.
    private const int ChunkEncodedByteLimit = 850;

    public async Task<byte[]> SynthesizeAsync(string text, string voice, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<byte>();

        text = NormalizeInputText(text);
        var lang = GetGoogleLangCode(voice);
        Console.WriteLine($"[FreeTTS] Synthesizing: lang={lang}, voice={voice}, len={text.Length}");

        using var ms = new MemoryStream();
        var chunks = SplitIntoChunks(text);

        foreach (var chunk in chunks)
        {
            if (string.IsNullOrWhiteSpace(chunk)) continue;
            var chunkBytes = await DownloadChunkWithFallbackAsync(chunk, lang, cancellationToken);
            if (chunkBytes?.Length > 0)
                ms.Write(chunkBytes, 0, chunkBytes.Length);
        }

        var final = ms.ToArray();
        Console.WriteLine($"[FreeTTS] Done: {final.Length} bytes total.");
        return final;
    }

    private static async Task<byte[]?> DownloadChunkWithFallbackAsync(string text, string lang, CancellationToken ct)
    {
        return await DownloadChunkWithFallbackCoreAsync(text, lang, ct, 0);
    }

    private static async Task<byte[]?> DownloadChunkWithFallbackCoreAsync(string text, string lang, CancellationToken ct, int depth)
    {
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        var encoded = Uri.EscapeDataString(normalized);
        var textLen = normalized.Length;
        var urls = BuildTtsUrls(lang, encoded, textLen);
        var allBadRequest = true;

        foreach (var url in urls)
        {
            // Thử tối đa 2 lần cho mỗi endpoint
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    using var client = CreateHttpClient(url);
                    var bytes = await client.GetByteArrayAsync(url, ct);

                    if (IsValidMp3(bytes))
                    {
                        Console.WriteLine($"[FreeTTS] OK: {bytes.Length} bytes for '{text[..Math.Min(20, text.Length)]}...'");
                        return bytes;
                    }
                    else
                    {
                        Console.WriteLine($"[FreeTTS] INVALID MP3 from {url[..60]}: got {bytes.Length} bytes");
                        // Không phải MP3 → thử endpoint tiếp theo
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is HttpRequestException)
                    {
                        Console.WriteLine($"[FreeTTS] HTTP error attempt {attempt + 1}: {ex.Message}");
                    }
                    else
                    {
                        allBadRequest = false;
                        Console.WriteLine($"[FreeTTS] Error attempt {attempt + 1}: {ex.Message}");
                    }
                    if (attempt == 0)
                        await Task.Delay(500, ct); // chờ 500ms rồi thử lại
                }
            }
        }

        // Nếu tất cả endpoint đều 400, tự động cắt đôi chunk để tăng cơ hội thành công.
        if (allBadRequest && normalized.Length > 30 && depth < 3)
        {
            var splitIndex = FindSplitIndex(normalized);
            if (splitIndex > 10 && splitIndex < normalized.Length - 10)
            {
                var left = normalized[..splitIndex].Trim();
                var right = normalized[splitIndex..].Trim();

                var leftBytes = await DownloadChunkWithFallbackCoreAsync(left, lang, ct, depth + 1);
                var rightBytes = await DownloadChunkWithFallbackCoreAsync(right, lang, ct, depth + 1);
                if (leftBytes?.Length > 0 && rightBytes?.Length > 0)
                {
                    var merged = new byte[leftBytes.Length + rightBytes.Length];
                    Buffer.BlockCopy(leftBytes, 0, merged, 0, leftBytes.Length);
                    Buffer.BlockCopy(rightBytes, 0, merged, leftBytes.Length, rightBytes.Length);
                    return merged;
                }
            }
        }

        Console.WriteLine($"[FreeTTS] All endpoints failed for chunk: '{text[..Math.Min(30, text.Length)]}'");
        return null;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("Accept", "audio/mpeg,audio/*;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        return client;
    }

    private static HttpClient CreateHttpClient(string url)
    {
        var client = CreateHttpClient();
        if (url.Contains("googleapis.com", StringComparison.OrdinalIgnoreCase))
            client.DefaultRequestHeaders.Add("Referer", "https://translate.googleapis.com/");
        else if (url.Contains("google.com.vn", StringComparison.OrdinalIgnoreCase))
            client.DefaultRequestHeaders.Add("Referer", "https://translate.google.com.vn/");
        else
            client.DefaultRequestHeaders.Add("Referer", "https://translate.google.com/");
        return client;
    }

    private static string[] BuildTtsUrls(string lang, string encodedText, int textLen)
    {
        return new[]
        {
            $"https://translate.googleapis.com/translate_tts?ie=UTF-8&client=gtx&tl={lang}&q={encodedText}",
            $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={lang}&q={encodedText}",
            $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={lang}&total=1&idx=0&textlen={textLen}&q={encodedText}&prev=input",
            $"https://translate.google.com.vn/translate_tts?ie=UTF-8&client=tw-ob&tl={lang}&q={encodedText}"
        };
    }

    private static int FindSplitIndex(string text)
    {
        var middle = text.Length / 2;
        for (int i = middle; i > 10; i--)
        {
            var c = text[i - 1];
            if (c is '.' or '!' or '?' or ',' or ';' or ':' or ' ')
                return i;
        }
        return middle;
    }

    private static bool IsValidMp3(byte[]? bytes)
    {
        if (bytes == null || bytes.Length < 128) return false;

        // ID3 header (MP3 với metadata)
        if (bytes[0] == 'I' && bytes[1] == 'D' && bytes[2] == '3') return true;

        // MPEG frame sync bits
        if (bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0) return true;

        return false;
    }

    private static string GetGoogleLangCode(string voice)
    {
        if (voice.StartsWith("vi", StringComparison.OrdinalIgnoreCase)) return "vi";
        if (voice.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return "en";
        if (voice.StartsWith("ja", StringComparison.OrdinalIgnoreCase)) return "ja";
        if (voice.StartsWith("ko", StringComparison.OrdinalIgnoreCase)) return "ko";
        if (voice.StartsWith("zh-cn", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
        if (voice.StartsWith("zh-tw", StringComparison.OrdinalIgnoreCase)) return "zh-TW";
        if (voice.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
        if (voice.StartsWith("fr", StringComparison.OrdinalIgnoreCase)) return "fr";
        if (voice.StartsWith("de", StringComparison.OrdinalIgnoreCase)) return "de";
        if (voice.StartsWith("es", StringComparison.OrdinalIgnoreCase)) return "es";
        if (voice.StartsWith("th", StringComparison.OrdinalIgnoreCase)) return "th";

        // Nếu voice không theo chuẩn locale (không có '-'), mặc định tiếng Việt
        if (!voice.Contains('-')) return "vi";

        var parts = voice.Split('-');
        return parts[0].ToLower();
    }

    private static IEnumerable<string> SplitIntoChunks(string text)
    {
        text = NormalizeInputText(text).Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();

        var chunks = new List<string>();
        int start = 0;

        while (start < text.Length)
        {
            // Tính độ dài URL-encoded của từng ký tự để không vượt giới hạn
            // Ký tự CJK (Hàn/Trung/Nhật): mỗi char → ~9 byte encoded, nên ChunkCharLimit không đủ
            int end = start;
            int encodedLen = 0;

            while (end < text.Length)
            {
                var charEncoded = Uri.EscapeDataString(text[end].ToString());
                if (encodedLen + charEncoded.Length > ChunkEncodedByteLimit && end > start)
                    break;
                encodedLen += charEncoded.Length;
                end++;
            }

            // Ưu tiên cắt tại dấu câu gần nhất để câu tròn vị
            int cutAt = end;
            for (int i = end; i > start + 5; i--)
            {
                char c = text[i - 1];
                if (c is '.' or '!' or '?' or '\u3002' or '\uff01' or '\uff1f'
                       or ',' or '\uff0c' or ';' or '\uff1b')
                {
                    cutAt = i;
                    break;
                }
            }

            chunks.Add(text[start..cutAt].Trim());
            start = cutAt;
        }

        return chunks;
    }

    private static string NormalizeInputText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input
            .Replace('\u00A0', ' ')
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u2015', '-')
            .Replace('\u201C', '"')
            .Replace('\u201D', '"')
            .Replace('\u2018', '\'')
            .Replace('\u2019', '\'')
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        return normalized.Trim();
    }
}
