namespace VKFoodTour.Mobile.Services;

public interface ILocalizationService
{
    /// <summary>Mã ngôn ngữ hiện tại (vi, en, …).</summary>
    string CurrentLanguageCode { get; }

    /// <summary>Lấy chuỗi đã dịch; nếu có <paramref name="formatArgs"/> thì <see cref="string.Format(string, object?[])"/>.</summary>
    string GetString(string key, params object[]? formatArgs);

    /// <summary>Lưu Preferences và phát sự kiện để các màn hình cập nhật chữ.</summary>
    void SetLanguageCode(string code);

    event EventHandler? LanguageChanged;
}
