using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Models;

namespace CarTrips.ViewModels
{
    public partial class StationsViewModel : ViewModelBase
    {
        public ObservableCollection<Station> FoundStations { get; } = new();

        [ObservableProperty] private string _cityName = string.Empty;
        [ObservableProperty] private string _statusMessage = "Введіть назву міста для пошуку заправок";
        [ObservableProperty] private bool _isLoading = false;

        private readonly HttpClient _httpClient;

        public StationsViewModel()
        {
            _httpClient = new HttpClient();
            // Обов'язковий заголовок для OpenStreetMap
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CarTripsApp/1.0"); 
        }

        [RelayCommand]
        private async Task SearchStationsAsync()
        {
            if (string.IsNullOrWhiteSpace(CityName))
            {
                StatusMessage = "Будь ласка, введіть назву міста!";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Шукаємо станції у місті {CityName}...";
            FoundStations.Clear();

            // Хеш-сет для відстеження унікальних адрес (щоб уникнути дублікатів)
            var seenAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string cleanCity = CityName.Trim();

            try
            {
                // Шукаємо спочатку звичайні АЗС за чіткими категоріями окремо
                string[] fuelTerms = { "fuel", "АЗС", "заправка" };
                foreach (var term in fuelTerms)
                {
                    await FetchDataFromApi(term, "Паливна АЗС", cleanCity, seenAddresses);
                }

                // Додаємо електрозарядки окремими чистими запитами
                string[] chargingTerms = { "charging_station", "електрозарядка" };
                foreach (var term in chargingTerms)
                {
                    await FetchDataFromApi(term, "Електрозарядка", cleanCity, seenAddresses);
                }

                if (FoundStations.Count > 0)
                {
                    StatusMessage = $"Знайдено унікальних станцій: {FoundStations.Count}";
                }
                else
                {
                    StatusMessage = $"У місті {CityName} нічого не знайдено. Перевірте правопис або спробуйте інше місто.";
                }
            }
            catch (Exception)
            {
                StatusMessage = "Помилка інтернету. Перевірте з'єднання!";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FetchDataFromApi(string keyword, string displayType, string city, HashSet<string> seenAddresses)
        {
            // Запит містить лише ОДНЕ ключове слово + місто
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
                        {
                            name = parts[0].Trim();
                        }

                        // Лаконічне форматування: Вулиця, Номер будинку
                        if (parts.Length >= 3)
                        {
                            address = $"{parts[1].Trim()}, {parts[2].Trim()}";
                        }
                        else
                        {
                            address = fullAddress;
                        }
                    }

                    // Перевірка на технічні назви або збіг з адресою
                    if (string.IsNullOrEmpty(name) || name.ToLower().Contains("fuel") || name == address.Split(',')[0].Trim())
                    {
                        name = displayType == "Паливна АЗС" ? "АЗС" : "Електрозарядка";
                    }

                    // Пропускаємо, якщо назва заправки дорівнює назві самого міста
                    if (name.Equals(city, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Якщо такий об'єкт за адресою вже додано з попереднього кроку циклу — пропускаємо дублікат
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
                // Ігноруємо поодинокі помилки під-запитів, щоб програма продовжувала пошук інших категорій
            }
        }
    }
}