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
    public partial class ServiceStationsViewModel : ViewModelBase
    {
        public ObservableCollection<Station> FoundServices { get; } = new();

        [ObservableProperty] private string _cityName = string.Empty;
        [ObservableProperty] private string _statusMessage = "Введіть назву міста для пошуку СТО";
        [ObservableProperty] private bool _isLoading = false;

        private readonly HttpClient _httpClient;

        public ServiceStationsViewModel()
        {
            _httpClient = new HttpClient();
            // Обов'язковий заголовок, щоб OSM не блокував запити
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CarTripsApp/1.0"); 
        }

        [RelayCommand]
        private async Task SearchServicesAsync()
        {
            if (string.IsNullOrWhiteSpace(CityName))
            {
                StatusMessage = "Будь ласка, введіть назву міста!";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Шукаємо автосервіси у місті {CityName}...";
            FoundServices.Clear();

            // Хеш-сет для відстеження дублікатів за повним текстом адреси
            var seenAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string cleanCity = CityName.Trim();

            // Окремі чисті ключові слова, які Nominatim чудово розуміє як категорії
            string[] searchTerms = { "СТО", "car_repair", "автосервіс", "шиномонтаж" };

            try
            {
                foreach (string term in searchTerms)
                {
                    // Формуємо точний запит: "Категорія Місто"
                    string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(term + " " + cleanCity)}&format=json&addressdetails=1&limit=20";

                    var response = await _httpClient.GetStringAsync(url);
                    using JsonDocument doc = JsonDocument.Parse(response);

                    if (doc.RootElement.ValueKind != JsonValueKind.Array) 
                        continue;

                    foreach (JsonElement element in doc.RootElement.EnumerateArray())
                    {
                        // 1. Отримуємо назву СТО
                        string name = "";
                        if (element.TryGetProperty("name", out JsonElement nameProp) && !string.IsNullOrEmpty(nameProp.GetString()))
                        {
                            name = nameProp.GetString()!;
                        }

                        // 2. Отримуємо та форматуємо адресу
                        string address = "Вулиця не вказана";
                        string fullAddress = "";
                        
                        if (element.TryGetProperty("display_name", out JsonElement addrProp))
                        {
                            fullAddress = addrProp.GetString()!;
                            string[] parts = fullAddress.Split(',');

                            if (string.IsNullOrEmpty(name) && parts.Length > 0)
                            {
                                name = parts[0].Trim();
                            }

                            // Витягуємо вулицю та номер будинку
                            if (parts.Length >= 3)
                            {
                                address = $"{parts[1].Trim()}, {parts[2].Trim()}";
                            }
                            else
                            {
                                address = fullAddress;
                            }
                        }

                        // Якщо назви взагалі немає або вона збігається з початком адреси, ставимо дефолт
                        if (string.IsNullOrEmpty(name) || name.ToLower().Contains("repair") || name == address.Split(',')[0].Trim())
                        {
                            name = term.Equals("шиномонтаж", StringComparison.OrdinalIgnoreCase) ? "Шиномонтаж" : "Автосервіс (СТО)";
                        }

                        // Пропускаємо, якщо назва збігається з назвою самого міста
                        if (name.Equals(cleanCity, StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Захист від дублікатів: якщо така адреса вже була додана з попереднього кроку циклу — ігноруємо
                        if (!string.IsNullOrEmpty(fullAddress) && !seenAddresses.Add(fullAddress))
                            continue;

                        // Додаємо знайдену унікальну станцію
                        FoundServices.Add(new Station
                        {
                            Name = name,
                            Address = address,
                            StationType = term.Equals("шиномонтаж", StringComparison.OrdinalIgnoreCase) ? "Шиномонтаж" : "СТО / Автосервіс"
                        });
                    }
                }

                // Виводимо фінальний статус
                if (FoundServices.Count > 0)
                {
                    StatusMessage = $"Знайдено унікальних СТО: {FoundServices.Count}";
                }
                else
                {
                    StatusMessage = $"У місті {CityName} автосервісів не знайдено. Спробуйте інше місто.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Помилка завантаження даних. Перевірте інтернет!";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}