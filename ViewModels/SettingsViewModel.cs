using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CarTrips.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    // Приватні поля
    private bool _isLightTheme;
    private bool _isEnglish;

    // Властивість для теми
    public bool IsLightTheme
    {
        get => _isLightTheme;
        set
        {
            // SetProperty оновлює значення і повідомляє інтерфейс про зміну.
            if (SetProperty(ref _isLightTheme, value))
            {
                if (Application.Current != null)
                {
                    // Міняємо тему всієї програми
                    Application.Current.RequestedThemeVariant = value ? ThemeVariant.Light : ThemeVariant.Dark;
                }
            }
        }
    }

    // Властивість для мови
    public bool IsEnglish
    {
        get => _isEnglish;
        set
        {
            if (SetProperty(ref _isEnglish, value))
            {
                // Викликаємо метод зміни мови при натисканні перемикача
                UpdateLanguage(value);
            }
        }
    }

    // Логіка підміни файлів перекладу
    private void UpdateLanguage(bool isEnglish)
    {
        var app = Application.Current;
        if (app == null) return;

        // 1. Шлях до потрібного файлу (перевір, щоб папка Assets та файли існували)
        string langPath = isEnglish 
            ? "avares://CarTrips/Assets/LangEN.axaml" 
            : "avares://CarTrips/Assets/LangUA.axaml";

        try
        {
            // 2. Завантажуємо новий словник ресурсів
            var newLanguage = (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri(langPath));

            // 3. Шукаємо старий мовний словник (шукаємо по ключу "TextHome", який має бути у файлі)
            var oldLanguage = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d is ResourceDictionary dict && dict.ContainsKey("TextHome"));

            // 4. Замінюємо старий словник на новий
            if (oldLanguage != null)
            {
                app.Resources.MergedDictionaries.Remove(oldLanguage);
            }
            
            app.Resources.MergedDictionaries.Add(newLanguage);
        }
        catch (Exception ex)
        {
            // Якщо шлях до файлу неправильний, програма не впаде, а просто проігнорує зміну
            Console.WriteLine($"Помилка завантаження мови: {ex.Message}");
        }
    }
}