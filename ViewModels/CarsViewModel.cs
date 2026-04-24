using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Models;

namespace CarTrips.ViewModels;

public partial class CarsViewModel : ViewModelBase
{
    private readonly Action? _onCarAdded;
    private const string FilePath = "cars_data.json";

    public ObservableCollection<Car> MyCars { get; } = new();

    // Поля для форми "Додати машину"
    [ObservableProperty] private string _newBrand = "";
    [ObservableProperty] private string _newEngine = "";
    [ObservableProperty] private string _newService = "";
    [ObservableProperty] private string _newFuel = "Бензин";
    [ObservableProperty] private string _newConsumption = "";
    [ObservableProperty] private string _newMileage = "";
    [ObservableProperty] private string _errorMessage = "";

    public CarsViewModel(Action? onCarAdded)
    {
        _onCarAdded = onCarAdded;
        LoadFromFile(); // Завантажуємо дані при старті
    }

    [RelayCommand]
    private void AddCar()
    {
        // 1. Валідація
        if (string.IsNullOrWhiteSpace(NewBrand) || string.IsNullOrWhiteSpace(NewMileage))
        {
            ErrorMessage = "Заповніть хоча б назву та пробіг!";
            return;
        }

        // 2. Створення об'єкта
        var car = new Car
        {
            Brand = NewBrand,
            EngineVolume = NewEngine,
            LastService = NewService,
            FuelType = NewFuel,
            FuelConsumption = double.TryParse(NewConsumption.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double res) ? res : 0,
            Mileage = NewMileage
        };

        // 3. Додавання в список
        MyCars.Insert(0, car);
        
        // 4. Збереження у файл
        SaveToFile();

        // 5. Очищення полів
        NewBrand = NewEngine = NewService = NewConsumption = NewMileage = "";
        ErrorMessage = "";

        // 6. Перехід на іншу сторінку (якщо налаштовано)
        _onCarAdded?.Invoke();
    }

    private void SaveToFile()
    {
        try
        {
            var json = JsonSerializer.Serialize(MyCars);
            File.WriteAllText(FilePath, json);
        }
        catch { /* Обробка помилок запису */ }
    }

    private void LoadFromFile()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                var json = File.ReadAllText(FilePath);
                var items = JsonSerializer.Deserialize<ObservableCollection<Car>>(json);
                if (items != null)
                {
                    MyCars.Clear();
                    foreach (var item in items) MyCars.Add(item);
                }
            }
            catch { /* Обробка помилок читання */ }
        }
    }
    
    [RelayCommand]
    private void DeleteCar(Car carToDelete)
    {
        if (carToDelete != null)
        {
            // 1. Видаляємо зі списку в пам'яті
            MyCars.Remove(carToDelete);
            
            // Рядок UpdateStatistics(); ми поки що прибрали, щоб не було помилки!
            
            // 2. Зберігаємо оновлений список у JSON файл
            SaveToFile(); 
        }
    }
}