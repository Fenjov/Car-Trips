using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarTrips.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private bool _isEnglish = false;
    public bool IsEnglish
    {
        get => _isEnglish;
        set
        {
            if (SetProperty(ref _isEnglish, value))
            {
                ChangeLanguage(value);
            }
        }
    }

    private bool _isLightTheme = false;
    public bool IsLightTheme
    {
        get => _isLightTheme;
        set
        {
            if (SetProperty(ref _isLightTheme, value))
            {
                ChangeTheme(value);
            }
        }
    }

    public Action? OnLogoutRequest { get; set; }

    [RelayCommand]
    private void Logout()
    {
        OnLogoutRequest?.Invoke();
    }

    private void ChangeLanguage(bool isEnglish)
    {
        // ВИПРАВЛЕНО: Додано /Assets/ у шлях до файлів
        string langPath = isEnglish 
            ? "avares://CarTrips/Assets/LangEN.axaml" 
            : "avares://CarTrips/Assets/LangUA.axaml";

        UpdateResource("Language", langPath);
    }

    private void ChangeTheme(bool isLightTheme)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = isLightTheme 
                ? Avalonia.Styling.ThemeVariant.Light 
                : Avalonia.Styling.ThemeVariant.Dark;
        }

        // Переконайся, що файли тем (якщо вони є) лежать за цим шляхом або теж в Assets
        string themePath = isLightTheme 
            ? "avares://CarTrips/Assets/LightTheme.axaml" 
            : "avares://CarTrips/Assets/DarkTheme.axaml";

        UpdateResource("Theme", themePath);
    }

    private void UpdateResource(string type, string resourceUri)
    {
        if (Application.Current == null) return;

        try
        {
            // Використовуємо ResourceInclude для динамічного завантаження
            var newInclude = new ResourceInclude(new Uri("avares://CarTrips/App.axaml"))
            {
                Source = new Uri(resourceUri)
            };
            
            var mergedDicts = Application.Current.Resources.MergedDictionaries;
            Avalonia.Controls.IResourceProvider? oldDict = null;

            // Шукаємо старий словник, який треба замінити
            foreach (var dict in mergedDicts)
            {
                if (dict is ResourceInclude include && include.Source != null)
                {
                    string uriStr = include.Source.OriginalString.ToLower();
                    if (type == "Language" && (uriStr.Contains("langua") || uriStr.Contains("langen")))
                    {
                        oldDict = dict;
                        break;
                    }
                    if (type == "Theme" && (uriStr.Contains("lighttheme") || uriStr.Contains("darktheme")))
                    {
                        oldDict = dict;
                        break;
                    }
                }
                else if (dict is ResourceDictionary resDict)
                {
                    if (type == "Language" && (resDict.ContainsKey("TextTripCenter") || resDict.ContainsKey("TextSettings")))
                    {
                        oldDict = dict;
                        break;
                    }
                    if (type == "Theme" && resDict.ContainsKey("CardBackgroundColor"))
                    {
                        oldDict = dict;
                        break;
                    }
                }
            }

            // Якщо знайшли старий словник — видаляємо його
            if (oldDict != null)
            {
                mergedDicts.Remove(oldDict);
            }
            
            // Додаємо новий
            mergedDicts.Add(newInclude);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ПОМИЛКА завантаження ресурсу {resourceUri}: {ex.Message}");
        }
    }
}