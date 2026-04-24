using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Models;
using CarTrips.Services;

namespace CarTrips.ViewModels;
public class Trip
{
    public int Id { get; set; }
    public string Date { get; set; }
    public string Departure { get; set; }
    public string Destination { get; set; }
    public string Distance { get; set; }
    public string TripType { get; set; }
    public string Car { get; set; } 

    [JsonIgnore]
    public double EstimatedCost
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Distance)) return 0;
            
            // Дістаємо тільки цифри (якщо раптом ти ввів "150 км" замість просто "150")
            string cleanDistance = new string(Distance.Where(char.IsDigit).ToArray());
            
            if (double.TryParse(cleanDistance, out double dist))
            {
                // 7.2 грн за 1 км (8л/100км по 90грн)
                return dist * 7.2; 
            }
            return 0;
        }
    }

}
public partial class AddTripViewModel : ViewModelBase
{
    private readonly ObservableCollection<Trip> _trips;
    private readonly TripService _tripService;

    [ObservableProperty] private string _newDate;
    [ObservableProperty] private string _newDeparture;
    [ObservableProperty] private string _newDestination;
    [ObservableProperty] private string _newDistance;
    [ObservableProperty] private string _newTripType;
    [ObservableProperty] private string _newCar;
    [ObservableProperty] private string _errorMessage;

    public AddTripViewModel(ObservableCollection<Trip> trips, TripService service)
    {
        _trips = trips;
        _tripService = service;
    }

    [RelayCommand]
    private void AddTrip()
    {
        if (string.IsNullOrWhiteSpace(NewDate) || string.IsNullOrWhiteSpace(NewDeparture) || 
            string.IsNullOrWhiteSpace(NewDestination) || string.IsNullOrWhiteSpace(NewDistance))
        {
            ErrorMessage = "Заповніть всі основні поля!";
            return;
        }

        if (!int.TryParse(NewDistance, out _))
        {
            ErrorMessage = "Відстань має бути числом!";
            return;
        }

        int newId = _trips.Count > 0 ? _trips.Max(t => t.Id) + 1 : 1;

        var trip = new Trip
        {
            Id = newId,
            Date = NewDate,
            Departure = NewDeparture,
            Destination = NewDestination,
            Distance = NewDistance,
            TripType = NewTripType ?? "Стандарт",
            Car = NewCar ?? "Не вказано"
        };

        _trips.Insert(0, trip);
        _tripService.SaveTrips(_trips);

        // Очищення полів
        NewDate = NewDeparture = NewDestination = NewDistance = NewTripType = NewCar = ErrorMessage = string.Empty;
    }
}