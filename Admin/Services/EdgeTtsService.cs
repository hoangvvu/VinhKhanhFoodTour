using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Admin.Services;

/// <summary>
/// Dịch vụ Text-To-Speech dự phòng, sử dụng API hoàn toàn miễn phí của Google Translate.
/// (Được áp dụng để thay thế cho lỗi 401 Unauthorized mới cập nhật của Microsoft Edge).
/// </summary>
public class EdgeTtsService
{
    /// <summary>
    /// Chuyển đổi văn bản thành mảng byte MP3. Tự động cắt nhỏ văn bản để vượt qua giới hạn độ dài của Google.
    /// </summary>
    public async Task<byte[]> SynthesizeAsync(string text, string voice, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<byte>();

        // Lấy mã ngôn ngữ rút gọn, vd "ja-JP-Nanami" -> "ja"
        var lang = "en";
        var parts = voice.Split('-');
        if (parts.Length >= 1) lang = parts[0];
        
        // Fix riêng cho tiếng trung giản thể
        if (voice.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) lang = "zh-CN";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0 Safari/537.36");

        using var ms = new MemoryStream();
        
        // Chia văn bản thành từng câu/phần nhỏ (< 180 ký tự) để không bị Request-URI Too Long
        var words = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var chunk = new StringBuilder();
        
        foreach (var word in words)
        {
            if (chunk.Length + word.Length > 180)
            {
                await DownloadAndAppendChunkAsync(client, chunk.ToString(), lang, ms, cancellationToken);
                chunk.Clear();
            }
            chunk.Append(word).Append(' ');
        }
        
        if (chunk.Length > 0)
        {
            await DownloadAndAppendChunkAsync(client, chunk.ToString(), lang, ms, cancellationToken);
        }

        var finalBytes = ms.ToArray();
        Console.WriteLine($"[FreeTTS] Tổng kích thước Audio: {finalBytes.Length} byte.");
        return finalBytes;
    }

    private async Task DownloadAndAppendChunkAsync(HttpClient client, string text, string lang, MemoryStream ms, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        
        var url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={lang}&q={Uri.EscapeDataString(text.Trim())}";
        try
        {
            var bytes = await client.GetByteArrayAsync(url, ct);
            if (bytes != null && bytes.Length > 0)
            {
                ms.Write(bytes, 0, bytes.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FreeTTS] Lỗi tải chunk: {ex.Message}");
        }
    }
}
