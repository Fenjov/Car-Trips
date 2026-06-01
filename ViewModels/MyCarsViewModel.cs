using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Models;
using CarTrips.Services;

namespace CarTrips.ViewModels;

public partial class MyCarsViewModel : ViewModelBase
{
    private string _carsFilePath => $"cars_{SessionManager.CurrentUsername}.json";
    private List<Car> _allCarsFromDb = new();

    public ObservableCollection<Car> MyCars { get; } = new();

    [ObservableProperty] private string _searchBrand = string.Empty;
    [ObservableProperty] private string _searchFuel = string.Empty;
    [ObservableProperty] private string _searchEngine = string.Empty;
    [ObservableProperty] private string _searchMileage = string.Empty;

    [ObservableProperty] private int _totalCarsCount;
    [ObservableProperty] private double _totalSpentAllCars;

    public MyCarsViewModel()
    {
        LoadCars();
    }

    [RelayCommand]
    public void ApplyFilter()
    {
        // Покращений та безпечний пошук по всіх полях
        var filtered = _allCarsFromDb.Where(c => 
            (string.IsNullOrWhiteSpace(SearchBrand) || (c.Brand != null && c.Brand.Contains(SearchBrand, StringComparison.OrdinalIgnoreCase))) &&
            (string.IsNullOrWhiteSpace(SearchFuel) || (c.FuelType != null && c.FuelType.Contains(SearchFuel, StringComparison.OrdinalIgnoreCase))) &&
            (string.IsNullOrWhiteSpace(SearchEngine) || (c.EngineVolume != null && c.EngineVolume.Contains(SearchEngine, StringComparison.OrdinalIgnoreCase))) &&
            (string.IsNullOrWhiteSpace(SearchMileage) || (c.Mileage != null && c.Mileage.Contains(SearchMileage, StringComparison.OrdinalIgnoreCase)))
        ).ToList();

        MyCars.Clear();
        foreach (var car in filtered) MyCars.Add(car);
        
        TotalCarsCount = MyCars.Count;
    }

    public void LoadCars()
    {
        _allCarsFromDb.Clear();
        if (File.Exists(_carsFilePath))
        {
            try
            {
                string json = File.ReadAllText(_carsFilePath);
                var cars = JsonSerializer.Deserialize<List<Car>>(json);
                if (cars != null) _allCarsFromDb = cars;
            }
            catch { _allCarsFromDb = new List<Car>(); }
        }
        
        ResetDisplay();
    }

    private void ResetDisplay()
    {
        MyCars.Clear();
        foreach (var car in _allCarsFromDb) MyCars.Add(car);
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        TotalCarsCount = _allCarsFromDb.Count;
        TotalSpentAllCars = _allCarsFromDb.Sum(c => c.TotalSpent);
    }

    [RelayCommand]
    public void DeleteCar(Car carToDelete)
    {
        if (carToDelete != null)
        {
            _allCarsFromDb.Remove(carToDelete);
            MyCars.Remove(carToDelete);
            SaveCarsToFile();
            UpdateStatistics();
        }
    }

    private void SaveCarsToFile()
    {
        string json = JsonSerializer.Serialize(_allCarsFromDb, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_carsFilePath, json);
    }
}