using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Models;
using CarTrips.Services;

namespace CarTrips.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly TripService _tripService = new();
    public ObservableCollection<Trip> RecentTrips { get; } = new();

    // Дочірні ViewModels
    public AddTripViewModel AddTripVM { get; }
    public HistoryViewModel HistoryVM { get; }
    public CarsViewModel CarsVM { get; }
    public SettingsViewModel SettingsVM { get; }

    #region Навігація (Властивості IsVisible)
    [ObservableProperty] private bool _isHomeVisible = true;
    [ObservableProperty] private bool _isAddCarVisible = false;
    [ObservableProperty] private bool _isMyCarVisible = false;
    [ObservableProperty] private bool _isHistory = false;
    [ObservableProperty] private bool _isSettingsVisible = false;
    #endregion

    public MainWindowViewModel()
    {
        // Завантажуємо дані
        var loaded = _tripService.LoadTrips();
        foreach (var t in loaded) RecentTrips.Add(t);

        // Ініціалізуємо під-моделі
        AddTripVM = new AddTripViewModel(RecentTrips, _tripService);
        HistoryVM = new HistoryViewModel(RecentTrips, _tripService);
        CarsVM = new CarsViewModel(() => GoToPage("Машина"));
        SettingsVM = new SettingsViewModel();
    }

    [RelayCommand]
    public void GoToPage(string pageName)
    {
        IsHomeVisible = IsAddCarVisible = IsMyCarVisible = IsHistory = IsSettingsVisible = false;

        switch (pageName)
        {
            case "Головна": IsHomeVisible = true; break;
            case "Додати машину": IsAddCarVisible = true; break;
            case "Машина": IsMyCarVisible = true; break;
            case "Історія поїздок": IsHistory = true; break;
            case "Налаштування": IsSettingsVisible = true; break;
        }
    }
}