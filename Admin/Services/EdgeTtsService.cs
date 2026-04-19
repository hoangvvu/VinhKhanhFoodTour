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
    // Google Translate TTS an toàn dưới ~200 URL-encoded chars (~2000 byte thực tế).
    private const int ChunkEncodedByteLimit = 2000;

    // Endpoint thử lần lượt
    private static readonly string[] TtsEndpoints = new[]
    {
        "https://translate.googleapis.com/translate_tts?ie=UTF-8&client=gtx&tl={lang}&q={q}",
        "https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={lang}&q={q}",
    };

    public async Task<byte[]> SynthesizeAsync(string text, string voice, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<byte>();

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
        var encoded = Uri.EscapeDataString(text.Trim());
        
        foreach (var template in TtsEndpoints)
        {
            var url = template.Replace("{lang}", lang).Replace("{q}", encoded);

            // Thử tối đa 2 lần cho mỗi endpoint
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    using var client = CreateHttpClient();
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
                    Console.WriteLine($"[FreeTTS] Error attempt {attempt + 1}: {ex.Message}");
                    if (attempt == 0)
                        await Task.Delay(500, ct); // chờ 500ms rồi thử lại
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
        client.DefaultRequestHeaders.Add("Referer", "https://translate.google.com/");
        return client;
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

        // Nếu là FPT voice code (vd: lannhi, banmai) → mặc định tiếng Việt
        if (!voice.Contains('-')) return "vi";

        var parts = voice.Split('-');
        return parts[0].ToLower();
    }

    private static IEnumerable<string> SplitIntoChunks(string text)
    {
        text = text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();

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
}
