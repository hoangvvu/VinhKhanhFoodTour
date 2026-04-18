using System.Net.Http.Headers;

namespace VKFoodTour.Mobile.Services;

public static class VkHttpClientExtensions
{
    public static void ConfigureVkApiClient(this HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        AddTunnelBypassHeaders(client);
        if (!client.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "VKFoodTourMobile/1.0 (MAUI)");
    }

    /// <summary>Client dùng cho ảnh/audio (Accept */*).</summary>
    public static void ConfigureVkMediaClient(this HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(120);
        AddTunnelBypassHeaders(client);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "VKFoodTourMobile/1.0 (MAUI)");
    }

    /// <summary>
    /// Thêm các header để bỏ qua trang cảnh báo của các dịch vụ tunnel:
    ///   • ngrok: "ngrok-skip-browser-warning"
    ///   • Visual Studio Dev Tunnels: "X-Tunnel-Authorization: tunnel" — đây là lý do ảnh
    ///     vendor không hiển thị trên mobile. Khi mobile request một URL *.devtunnels.ms
    ///     lần đầu, Microsoft chặn lại và trả về trang HTML cảnh báo "You're about to
    ///     visit a tunnel" thay vì ảnh thật. Trình duyệt bấm Continue xong lưu cookie
    ///     nên lần sau OK, nhưng HttpClient + Glide (MAUI) không giữ cookie nên luôn
    ///     nhận HTML → parse thất bại → hiển thị emoji placeholder.
    ///   Thêm header này để tunnel coi request đã "bypass" và trả thẳng nội dung thật.
    /// </summary>
    private static void AddTunnelBypassHeaders(HttpClient client)
    {
        if (!client.DefaultRequestHeaders.Contains("ngrok-skip-browser-warning"))
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");

        // Header chính thức của Visual Studio Dev Tunnels để bỏ trang anti-phishing.
        if (!client.DefaultRequestHeaders.Contains("X-Tunnel-Authorization"))
            client.DefaultRequestHeaders.Add("X-Tunnel-Authorization", "tunnel");
    }
}
