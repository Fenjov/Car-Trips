using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Models;
using CarTrips.Services;

// ==========================================
// КЛАС TRIP (ОНОВЛЕНИЙ ДЛЯ ЗБЕРЕЖЕННЯ ЦІНИ)
// ==========================================
namespace CarTrips.Models
{
    public class Trip
    {
        public int Id { get; set; }
        public string Date { get; set; } = string.Empty;
        public string Departure { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Distance { get; set; } = string.Empty;
        public string TripType { get; set; } = string.Empty;
        public string Car { get; set; } = string.Empty;

        // Тепер ціна зберігається у файлі і не зникне, якщо машину видалять
        public double EstimatedCost { get; set; }
    }
}

// ==========================================
// VIEWMODEL ДЛЯ ДОДАННЯ ПОЇЗДКИ
// ==========================================
namespace CarTrips.ViewModels
{
    public partial class AddTripViewModel : ViewModelBase
    {
        private readonly ObservableCollection<Trip> _trips;
        private readonly TripService _tripService;

        // Посилання на інші автомобільні модулі для миттєвого відображення змін
        private readonly CarsViewModel _carsVM;
        private readonly MyCarsViewModel _myCarsVM;

        // Динамічний шлях до файлу машин конкретного користувача
        private string CarsFilePath => $"cars_{SessionManager.CurrentUsername}.json";

        [ObservableProperty] private string _newDate = string.Empty;
        [ObservableProperty] private string _newDeparture = string.Empty;
        [ObservableProperty] private string _newDestination = string.Empty;
        [ObservableProperty] private string _newDistance = string.Empty;
        [ObservableProperty] private string _newTripType = string.Empty;
        
        // Зберігаємо обраний об'чок машини Car
        [ObservableProperty] private Car? _selectedCar;
        [ObservableProperty] private string _errorMessage = string.Empty;

        // Виправлений конструктор: приймає посилання на CarsViewModel та MyCarsViewModel
        public AddTripViewModel(ObservableCollection<Trip> trips, TripService service, CarsViewModel carsVM, MyCarsViewModel myCarsVM)
        {
            _trips = trips;
            _tripService = service;
            _carsVM = carsVM;
            _myCarsVM = myCarsVM;
        }

        // Приватний метод для динамічного розрахунку вартості поїздки
        private double CalculateTripCost(double distance, Car car)
        {
            // ПЕРЕВІРКА НА ЕЛЕКТРО: якщо тип палива містить "Електро", витрати за поїздку дорівнюють 0
            if (car.FuelType != null && car.FuelType.Contains("Електро", StringComparison.OrdinalIgnoreCase))
            {
                return 0.0;
            }

            double fuelPrice = 55.0; // за замовчуванням (Бензин)

            if (car.FuelType != null)
            {
                if (car.FuelType.Contains("Дизель", StringComparison.OrdinalIgnoreCase))
                {
                    fuelPrice = 52.0;
                }
            }

            // Формула для звичайних авто: (Дистанція / 100) * Витрата авто * Ціна палива
            return (distance / 100.0) * car.FuelConsumption * fuelPrice;
        }

        [RelayCommand]
        private void AddTrip()
        {
            // 1. Валідація текстових полів
            if (string.IsNullOrWhiteSpace(NewDate) || string.IsNullOrWhiteSpace(NewDeparture) || 
                string.IsNullOrWhiteSpace(NewDestination) || string.IsNullOrWhiteSpace(NewDistance))
            {
                ErrorMessage = "Заповніть всі основні поля!";
                return;
            }

            // 2. Валідація вибору машини з ComboBox
            if (SelectedCar == null)
            {
                ErrorMessage = "Будь ласка, оберіть машину зі списку!";
                return;
            }

            // 3. Валідація відстані та конвертація в double
            string testDistance = NewDistance.Replace(',', '.');
            if (!double.TryParse(testDistance, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDistance))
            {
                ErrorMessage = "Відстань має бути числом!";
                return;
            }

            // 4. Динамічний розрахунок вартості на основі параметрів машини
            double cost = CalculateTripCost(parsedDistance, SelectedCar);

            int newId = _trips.Count > 0 ? _trips.Max(t => t.Id) + 1 : 1;

            // 5. ОНОВЛЕННЯ ПРОБІГУ В КЛАСІ ТА ФАЙЛІ
            UpdateCarMileageAndSave(SelectedCar.Brand, parsedDistance);

            // 6. Створення поїздки
            var trip = new Trip
            {
                Id = newId,
                Date = NewDate,
                Departure = NewDeparture,
                Destination = NewDestination,
                Distance = NewDistance,
                TripType = NewTripType ?? "Стандарт",
                Car = SelectedCar.Brand, // Записуємо марку
                EstimatedCost = Math.Round(cost, 2) // Записуємо вирахувану вартість з округленням
            };

            _trips.Insert(0, trip);
            _tripService.SaveTrips(_trips);

            // 7. ОНОВЛЕННЯ ЕКРАНІВ: Наказуємо іншим вкладкам миттєво перечитати нові дані з диска в оперативну пам'ять
            _carsVM?.LoadFromFile();
            _myCarsVM?.LoadCars();

            // 8. Очищення полів після успішного збереження
            NewDate = NewDeparture = NewDestination = NewDistance = NewTripType = ErrorMessage = string.Empty;
            SelectedCar = null; // Скидаємо виділену машину
        }

        /// <summary>
        /// Метод самостійно дістає цифри з пробігу, додає дистанцію та перезаписує файл
        /// </summary>
        private void UpdateCarMileageAndSave(string carBrand, double kmDelta)
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
                        // Очищаємо рядок пробігу від тексту ( залишаємо лише цифри )
                        var digits = targetCar.Mileage?.Where(char.IsDigit).ToArray();
                        double currentMileage = 0;

                        if (digits != null && digits.Length > 0)
                        {
                            double.TryParse(new string(digits), out currentMileage);
                        }

                        // Новий чистий пробіг
                        double newMileage = currentMileage + kmDelta;

                        // Зберігаємо назад у тому ж форматі, який був (з "км" чи просто числом)
                        if (targetCar.Mileage != null && targetCar.Mileage.Contains("км"))
                        {
                            targetCar.Mileage = $"{newMileage:N0} км";
                        }
                        else
                        {
                            targetCar.Mileage = newMileage.ToString(CultureInfo.InvariantCulture);
                        }

                        // Записуємо оновлений масив машин назад у JSON
                        string updatedJson = JsonSerializer.Serialize(cars, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(CarsFilePath, updatedJson);
                    }
                }
            }
            catch
            {
                // Захист від збоїв введення-виведення
            }
        }
    }
}