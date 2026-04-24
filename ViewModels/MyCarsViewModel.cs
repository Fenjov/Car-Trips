using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Models;
using CarTrips.Views;

namespace CarTrips.ViewModels;

public partial class MyCarsViewModel : ViewModelBase
{
    private readonly string _carsFilePath = "my_cars_data.json";

    // Список машин, який виводиться на екран
    public ObservableCollection<Car> MyCars { get; } = new();

    // Статистика для верхніх карток
    [ObservableProperty] private int _totalCarsCount;
    [ObservableProperty] private double _totalMileageAllCars;
    [ObservableProperty] private double _totalSpentAllCars;
    public MyCarsViewModel()
    {
        LoadCars();
    }
    private void LoadCars()
    {
        if (File.Exists(_carsFilePath))
        {
            string json = File.ReadAllText(_carsFilePath);
            var cars = JsonSerializer.Deserialize<Car[]>(json);
            if (cars != null)
            {
                MyCars.Clear();
                foreach (var car in cars)
                {
                    MyCars.Add(car);
                }
            }
        }
        UpdateStatistics();
    }
    // Оновлює цифри у верхніх картках
    private void UpdateStatistics()
    {
        TotalCarsCount = MyCars.Count;
    
        // Змінні для підсумків
        double totalKms = 0;
        double totalSpent = 0; // Додали змінну для підрахунку грошей

        foreach (var car in MyCars)
        {
            // 1. Рахуємо загальний пробіг безпечно (твій код)
            string cleanMileage = new string(car.Mileage?.Where(char.IsDigit).ToArray() ?? new char[0]);
            if (double.TryParse(cleanMileage, out double km))
            {
                totalKms += km;
            }

            // 2. Рахуємо витрачені гроші (додаємо витрати поточної машини до загальної суми)
            totalSpent += car.TotalSpent;
        }
    
        // Записуємо результати
        TotalMileageAllCars = totalKms;
        TotalSpentAllCars = totalSpent; // Записуємо витрати, щоб вони відобразились в UI
    }
    // Логіка для червоної кнопки видалення (корзини)
    [RelayCommand]
    private void DeleteCar(Car carToDelete)
    {
        if (carToDelete != null)
        {
            MyCars.Remove(carToDelete);
            SaveCarsToFile();
            UpdateStatistics();
        }
    }

    private void SaveCarsToFile()
    {
        string json = JsonSerializer.Serialize(MyCars, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_carsFilePath, json);
    }
}