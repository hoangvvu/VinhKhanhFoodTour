using System.Text.Json;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

public interface IAuthSessionService
{
    AuthUserDto? CurrentUser { get; }
    bool IsLoggedIn { get; }
    bool IsAnonymous { get; }
    void SetUser(AuthUserDto user);
    void EnterAnonymous();
    void Logout();
}

public class AuthSessionService : IAuthSessionService
{
    private const string UserKey = "AuthUser";
    private const int AnonymousUserId = -1;
    private AuthUserDto? _currentUser;

    public AuthSessionService()
    {
        var raw = Preferences.Default.Get(UserKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                _currentUser = JsonSerializer.Deserialize<AuthUserDto>(raw);
            }
            catch
            {
                _currentUser = null;
            }
        }
    }

    public AuthUserDto? CurrentUser => _currentUser;
    public bool IsLoggedIn => _currentUser is not null;
    public bool IsAnonymous => _currentUser?.UserId == AnonymousUserId;

    public void SetUser(AuthUserDto user)
    {
        _currentUser = user;
        Preferences.Default.Set(UserKey, JsonSerializer.Serialize(user));
    }

    public void EnterAnonymous()
    {
        _currentUser = new AuthUserDto
        {
            UserId = AnonymousUserId,
            Name = "Khach",
            Email = string.Empty,
            Role = "Anonymous"
        };
        Preferences.Default.Set(UserKey, JsonSerializer.Serialize(_currentUser));
    }

    public void Logout()
    {
        _currentUser = null;
        Preferences.Default.Remove(UserKey);
    }
}
