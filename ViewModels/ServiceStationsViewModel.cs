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
    public partial class ServiceStationsViewModel : ViewModelBase
    {
        public ObservableCollection<Station> FoundServices { get; } = new();

        [ObservableProperty] private string _cityName = string.Empty;
        [ObservableProperty] private string _statusMessage = string.Empty; 
        [ObservableProperty] private bool _isLoading = false;

        private readonly HttpClient _httpClient;

        public ServiceStationsViewModel()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CarTripsApp/1.0"); 
        }

        // Той самий надійний метод-помічник
        private string GetLocalizedString(string key, string fallback)
        {
            if (Application.Current != null && Application.Current.TryGetResource(key, out var resource) && resource != null)
            {
                return resource.ToString()!;
            }
            return fallback;
        }

        [RelayCommand]
        private async Task SearchServicesAsync()
        {
            if (string.IsNullOrWhiteSpace(CityName))
            {
                StatusMessage = GetLocalizedString("StatusEmptyCity", "Будь ласка, введіть назву міста!");
                return;
            }

            IsLoading = true;
            StatusMessage = "...";
            FoundServices.Clear();

            var seenAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string cleanCity = CityName.Trim();

            string[] searchTerms = { "СТО", "car_repair", "автосервіс", "шиномонтаж" };

            try
            {
                foreach (string term in searchTerms)
                {
                    string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(term + " " + cleanCity)}&format=json&addressdetails=1&limit=20";

                    var response = await _httpClient.GetStringAsync(url);
                    using JsonDocument doc = JsonDocument.Parse(response);

                    if (doc.RootElement.ValueKind != JsonValueKind.Array) 
                        continue;

                    foreach (JsonElement element in doc.RootElement.EnumerateArray())
                    {
                        string name = "";
                        if (element.TryGetProperty("name", out JsonElement nameProp) && !string.IsNullOrEmpty(nameProp.GetString()))
                        {
                            name = nameProp.GetString()!;
                        }

                        string address = "Вулиця не вказана";
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

                        if (string.IsNullOrEmpty(name) || name.ToLower().Contains("repair") || name == address.Split(',')[0].Trim())
                        {
                            name = term.Equals("шиномонтаж", StringComparison.OrdinalIgnoreCase) ? "Шиномонтаж" : "Автосервіс (СТО)";
                        }

                        if (name.Equals(cleanCity, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!string.IsNullOrEmpty(fullAddress) && !seenAddresses.Add(fullAddress))
                            continue;

                        FoundServices.Add(new Station
                        {
                            Name = name,
                            Address = address,
                            StationType = term.Equals("шиномонтаж", StringComparison.OrdinalIgnoreCase) ? "Шиномонтаж" : "СТО / Автосервіс"
                        });
                    }
                }

                if (FoundServices.Count > 0)
                {
                    string pattern = GetLocalizedString("StatusFoundServicesPattern", "Знайдено унікальних СТО: {0}");
                    StatusMessage = string.Format(pattern, FoundServices.Count);
                }
                else
                {
                    string pattern = GetLocalizedString("StatusNotFoundPattern", "У місті {0} автосервісів не знайдено.");
                    StatusMessage = string.Format(pattern, CityName);
                }
            }
            catch (Exception)
            {
                StatusMessage = GetLocalizedString("StatusError", "Помилка завантаження даних. Перевірте інтернет!");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}