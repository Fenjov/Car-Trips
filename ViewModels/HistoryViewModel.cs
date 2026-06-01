using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Models;
using CarTrips.Services;

namespace CarTrips.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly ObservableCollection<Trip> _allTrips;
    private readonly TripService _tripService;

    // Посилання на інші автомобільні модулі для миттєвого відтворення змін при видаленні
    private readonly CarsViewModel _carsVM;
    private readonly MyCarsViewModel _myCarsVM;

    // Шлях до автомобілів поточного користувача для повернення пробігу
    private string CarsFilePath => $"cars_{SessionManager.CurrentUsername}.json";

    [ObservableProperty] private ObservableCollection<Trip> _filteredTrips;
    [ObservableProperty] private string _searchDate;
    [ObservableProperty] private string _searchId;
    [ObservableProperty] private string _searchDistance;
    [ObservableProperty] private string _searchType;

    public int TotalTripsCount => _allTrips.Count;
    public string TotalDistanceAllTrips => _allTrips.Sum(t => int.TryParse(t?.Distance, out int d) ? d : 0).ToString();
    public int TotalSpentAllTrips => (int)_allTrips.Sum(t => t?.EstimatedCost ?? 0);

    // ВИПРАВЛЕНО: Конструктор тепер приймає посилання на автомобілі
    public HistoryViewModel(ObservableCollection<Trip> trips, TripService service, CarsViewModel carsVM, MyCarsViewModel myCarsVM)
    {
        _allTrips = trips;
        _tripService = service;
        _carsVM = carsVM;
        _myCarsVM = myCarsVM;
        
        FilteredTrips = new ObservableCollection<Trip>(_allTrips);
        
        // Оновлюємо фільтр, коли змінюється основний список
        _allTrips.CollectionChanged += (s, e) => { SearchTrips(); UpdateStats(); };
    }

    [RelayCommand]
    private void SearchTrips()
    {
        var query = _allTrips.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchDate))
            query = query.Where(t => t.Date != null && t.Date.Contains(SearchDate));

        if (!string.IsNullOrWhiteSpace(SearchId) && int.TryParse(SearchId, out int id))
            query = query.Where(t => t.Id == id);

        if (!string.IsNullOrWhiteSpace(SearchDistance))
            query = query.Where(t => t.Distance != null && t.Distance.Contains(SearchDistance));

        if (!string.IsNullOrWhiteSpace(SearchType))
            query = query.Where(t => t.TripType != null && t.TripType.ToLower().Contains(SearchType.ToLower()));

        FilteredTrips = new ObservableCollection<Trip>(query);
        UpdateStats(); 
    }

    private void UpdateStats()
    {
        OnPropertyChanged(nameof(TotalTripsCount));
        OnPropertyChanged(nameof(TotalDistanceAllTrips));
        OnPropertyChanged(nameof(TotalSpentAllTrips));
    }

    [RelayCommand]
    private void DeleteTrip(Trip tripToDelete)
    {
        if (tripToDelete != null)
        {
            // 1. ПЕРЕД ВИДАЛЕННЯМ: Зменшуємо пробіг машини на відстань видаленої поїздки
            string rawDist = tripToDelete.Distance.Replace(',', '.');
            if (double.TryParse(rawDist, NumberStyles.Any, CultureInfo.InvariantCulture, out double distanceKm))
            {
                SubtractCarMileageFromFile(tripToDelete.Car, distanceKm);
            }

            // 2. Видаляємо саму поїздку
            _allTrips.Remove(tripToDelete);
            _tripService.SaveTrips(_allTrips);

            // 3. ВИПРАВЛЕНО: Наказуємо автомобільним вкладкам негайно перечитати файл з диска
            _carsVM?.LoadFromFile();
            _myCarsVM?.LoadCars();
        }
    }

    /// <summary>
    /// Метод зменшує пробіг машини у файлі при видаленні поїздки
    /// </summary>
    private void SubtractCarMileageFromFile(string carBrand, double kmDelta)
    {
        if (!File.Exists(CarsFilePath)) return;

        try
        {
            string json = File.ReadAllText(CarsFilePath);
            var cars = JsonSerializer.Deserialize<List<Car>>(json);

            if (cars != null)
            {
                var targetCar = cars.FirstOrDefault(c => c.Brand == carBrand);
                if (targetCar != null)
                {
                    // Витягуємо чисті цифри поточного пробігу автомобіля
                    var digits = targetCar.Mileage?.Where(char.IsDigit).ToArray();
                    double currentMileage = 0;

                    if (digits != null && digits.Length > 0)
                    {
                        double.TryParse(new string(digits), out currentMileage);
                    }

                    // Віднімаємо кілометраж скасованої поїздки
                    double newMileage = currentMileage - kmDelta;
                    if (newMileage < 0) newMileage = 0;

                    // Повертаємо назад у строку з валідацією на суфікс "км"
                    if (targetCar.Mileage != null && targetCar.Mileage.Contains("км"))
                    {
                        targetCar.Mileage = $"{newMileage:N0} км";
                    }
                    else
                    {
                        targetCar.Mileage = newMileage.ToString(CultureInfo.InvariantCulture);
                    }

                    // Перезаписуємо JSON файл автомобілів
                    string updatedJson = JsonSerializer.Serialize(cars, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(CarsFilePath, updatedJson);
                }
            }
        }
        catch { /* Захист від помилок доступу до файлу */ }
    }
}