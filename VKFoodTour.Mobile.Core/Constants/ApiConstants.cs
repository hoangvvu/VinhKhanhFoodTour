namespace VKFoodTour.Mobile.Core.Constants
{
    public static class ApiConstants
    {
        // 10.0.2.2 là địa chỉ IP đặc biệt để máy ảo Android "nhìn" thấy localhost của máy tính.
        // Lưu ý: Thay số 5242 bằng đúng số cổng HTTP của bạn (xem trong launchSettings.json)
        // Không dùng cổng HTTPS (như 7105) vì máy ảo Android sẽ chặn lỗi chứng chỉ bảo mật.

        public static string BaseApiUrl = "http://10.0.2.2:5242";
    }
}