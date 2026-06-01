namespace CarTrips.Services;

public static class SessionManager
{
    // Тут буде зберігатися логін поточного користувача
    public static string CurrentUsername { get; set; } = string.Empty;
}