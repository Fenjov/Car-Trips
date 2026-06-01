using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls; // <--- ОСЬ ЦЕЙ РЯДОК ВИРІШУЄ ПОМИЛКУ
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Models;

namespace CarTrips.ViewModels
{
    public partial class StationsViewModel : ViewModelBase
    {
        public ObservableCollection<Station> FoundStations { get; } = new();

        [ObservableProperty] private string _cityName = string.Empty;
        [ObservableProperty] private string _statusMessage = string.Empty; 
        [ObservableProperty] private bool _isLoading = false;

        private readonly HttpClient _httpClient;

        public StationsViewModel()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CarTripsApp/1.0"); 
        }

        // Метод-помічник для безпечного отримання перекладу зі словників
        private string GetLocalizedString(string key, string fallback)
        {
            if (Application.Current != null && Application.Current.TryGetResource(key, out var resource) && resource != null)
            {
                return resource.ToString()!;
            }
            return fallback;
        }

        [RelayCommand]
        private async Task SearchStationsAsync()
        {
            if (string.IsNullOrWhiteSpace(CityName))
            {
                StatusMessage = GetLocalizedString("StatusEmptyCity", "Будь ласка, введіть назву міста!");
                return;
            }

            IsLoading = true;
            StatusMessage = "..."; 
            FoundStations.Clear();

            var seenAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string cleanCity = CityName.Trim();

            try
            {
                string[] fuelTerms = { "fuel", "АЗС", "заправка" };
                foreach (var term in fuelTerms)
                {
                    await FetchDataFromApi(term, "Паливна АЗС", cleanCity, seenAddresses);
                }

                string[] chargingTerms = { "charging_station", "електрозарядка" };
                foreach (var term in chargingTerms)
                {
                    await FetchDataFromApi(term, "Електрозарядка", cleanCity, seenAddresses);
                }

                if (FoundStations.Count > 0)
                {
                    string pattern = GetLocalizedString("StatusFoundServicesPattern", "Знайдено унікальних станцій: {0}");
                    StatusMessage = string.Format(pattern, FoundStations.Count);
                }
                else
                {
                    string pattern = GetLocalizedString("StatusNotFoundPattern", "У місті {0} нічого не знайдено.");
                    StatusMessage = string.Format(pattern, CityName);
                }
            }
            catch (Exception)
            {
                StatusMessage = GetLocalizedString("StatusError", "Помилка інтернету. Перевірте з'єднання!");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FetchDataFromApi(string keyword, string displayType, string city, HashSet<string> seenAddresses)
        {
            string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(keyword + " " + city)}&format=json&addressdetails=1&limit=20";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(response);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return;

                foreach (JsonElement element in doc.RootElement.EnumerateArray())
                {
                    string name = "";
                    if (element.TryGetProperty("name", out JsonElement nameProp) && !string.IsNullOrEmpty(nameProp.GetString()))
                    {
                        name = nameProp.GetString()!;
                    }

                    string address = "Адреса не вказана";
                    string fullAddress = "";

                    if (element.TryGetProperty("display_name", out JsonElement addrProp))
                    {
                        fullAddress = addrProp.GetString()!;
                        string[] parts = fullAddress.Split(',');

                        if (string.IsNullOrEmpty(name) && parts.Length > 0)
                            name = parts[0].Trim();

                        if (parts.Length >= 3)
                            address = $"{parts[1].Trim()}, {parts[2].Trim()}";
                        else
                            address = fullAddress;
                    }

                    if (string.IsNullOrEmpty(name) || name.ToLower().Contains("fuel") || name == address.Split(',')[0].Trim())
                    {
                        name = displayType == "Паливна АЗС" ? "АЗС" : "Електрозарядка";
                    }

                    if (name.Equals(city, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!string.IsNullOrEmpty(fullAddress) && !seenAddresses.Add(fullAddress))
                        continue;

                    FoundStations.Add(new Station
                    {
                        Name = name,
                        Address = address,
                        StationType = displayType
                    });
                }
            }
            catch
            {
                // Ігноруємо поодинокі помилки під-запитів
            }
        }
    }
}