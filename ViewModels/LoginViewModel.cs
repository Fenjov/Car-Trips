using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Services;

namespace CarTrips.ViewModels;

// Модель для збереження користувача
public class UserAccount 
{
    public string Username { get; set; }
    public string Password { get; set; } // В ідеалі паролі треба шифрувати, але для початку залишимо так
}

public partial class LoginViewModel : ViewModelBase
{
    private readonly string _usersFile = "users_db.json";
    private readonly Action _onLoginSuccess; // Дія, яка виконається, коли вхід успішний

    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _errorMessage = "";

    public LoginViewModel(Action onLoginSuccess)
    {
        _onLoginSuccess = onLoginSuccess;
    }

    [RelayCommand]
    public void Login()
    {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.Username == Username && u.Password == Password);
        
        if (user != null)
        {
            SessionManager.CurrentUsername = Username; // Запам'ятовуємо, хто увійшов
            ErrorMessage = "";
            _onLoginSuccess?.Invoke(); // Перемикаємось на головний екран
        }
        else
        {
            ErrorMessage = "Невірний логін або пароль!";
        }
    }

    [RelayCommand]
    public void Register()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введіть логін та пароль!";
            return;
        }

        var users = LoadUsers();
        if (users.Any(u => u.Username == Username))
        {
            ErrorMessage = "Такий користувач вже існує!";
            return;
        }

        users.Add(new UserAccount { Username = Username, Password = Password });
        File.WriteAllText(_usersFile, JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true }));
        
        ErrorMessage = "Реєстрація успішна! Тепер натисніть 'Увійти'.";
        Password = ""; // Очищаємо пароль для безпеки після реєстрації
    }

    private List<UserAccount> LoadUsers()
    {
        if (!File.Exists(_usersFile)) return new List<UserAccount>();
        string json = File.ReadAllText(_usersFile);
        return JsonSerializer.Deserialize<List<UserAccount>>(json) ?? new List<UserAccount>();
    }
}