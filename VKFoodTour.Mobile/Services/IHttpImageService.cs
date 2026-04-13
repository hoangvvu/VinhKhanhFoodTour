namespace VKFoodTour.Mobile.Services;

public interface IHttpImageService
{
    Task<ImageSource?> GetImageSourceAsync(string? url, CancellationToken cancellationToken = default);
}
