using System.Net.Http.Headers;

namespace VKFoodTour.Mobile.Services;

public static class VkHttpClientExtensions
{
    public static void ConfigureVkApiClient(this HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        if (!client.DefaultRequestHeaders.Contains("ngrok-skip-browser-warning"))
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
        if (!client.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "VKFoodTourMobile/1.0 (MAUI)");
    }

    /// <summary>Client dùng cho ảnh/audio (Accept */*).</summary>
    public static void ConfigureVkMediaClient(this HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(120);
        if (!client.DefaultRequestHeaders.Contains("ngrok-skip-browser-warning"))
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "VKFoodTourMobile/1.0 (MAUI)");
    }
}
