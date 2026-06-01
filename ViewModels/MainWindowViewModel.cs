using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Models;
using CarTrips.Services;

namespace CarTrips.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private TripService _tripService;
    public ObservableCollection<Trip> RecentTrips { get; } = new();

    // Дочірні ViewModels
    [ObservableProperty] private AddTripViewModel _addTripVM;
    [ObservableProperty] private HistoryViewModel _historyVM;
    [ObservableProperty] private CarsViewModel _carsVM;
    [ObservableProperty] private MyCarsViewModel _myCarsVM; 
    [ObservableProperty] private SettingsViewModel _settingsVM;
    [ObservableProperty] private LoginViewModel _loginVM;
    [ObservableProperty] private StationsViewModel _stationsVM;
    [ObservableProperty] private ServiceStationsViewModel _serviceStationsVM; // ДОДАНО: ViewModel для СТО

    #region Навігація та Видимість
    [ObservableProperty] private bool _isLoginVisible = true;      
    [ObservableProperty] private bool _isMainContentVisible = false; 
    
    [ObservableProperty] private bool _isHomeVisible = false;
    [ObservableProperty] private bool _isAddCarVisible = false;
    [ObservableProperty] private bool _isMyCarVisible = false;
    [ObservableProperty] private bool _isHistory = false;
    [ObservableProperty] private bool _isSettingsVisible = false;
    [ObservableProperty] private bool _isStationsVisible = false;
    [ObservableProperty] private bool _isServiceStationsVisible = false; // ДОДАНО: Видимість екрану СТО
    #endregion

    public MainWindowViewModel()
    {
        IsLoginVisible = true;        
        IsMainContentVisible = false; 
    
        SettingsVM = new SettingsViewModel();
        SettingsVM.OnLogoutRequest = () => Logout();

        LoginVM = new LoginViewModel(() => 
        {
            InitializeUserData();
            IsLoginVisible = false;
            IsMainContentVisible = true;
            GoToPage("Головна");
        });
    }

    /// <summary>
    /// Метод для завантаження даних конкретного користувача після входу
    /// </summary>
    private void InitializeUserData()
    {
        _tripService = new TripService();

        RecentTrips.Clear();
        var loaded = _tripService.LoadTrips();
        foreach (var t in loaded) RecentTrips.Add(t);

        // 1. Спочатку створюємо автомобільні класи користувача
        CarsVM = new CarsViewModel(() => GoToPage("Машина"));
        MyCarsVM = new MyCarsViewModel();

        // 2. Передаємо автомобільні моделі в обидві ViewModel для миттєвої синхронізації
        AddTripVM = new AddTripViewModel(RecentTrips, _tripService, CarsVM, MyCarsVM);
        
        HistoryVM = new HistoryViewModel(RecentTrips, _tripService, CarsVM, MyCarsVM);

        // Ініціалізація нових вкладок після успішного входу
        StationsVM = new StationsViewModel();
        ServiceStationsVM = new ServiceStationsViewModel(); // ДОДАНО: Створення інстансу для СТО
    }

    [RelayCommand]
    public void GoToPage(string pageName)
    {
        if (IsLoginVisible) return;

        // Очищаємо всі прапорці видимості перед активацією потрібного
        IsHomeVisible = IsAddCarVisible = IsMyCarVisible = IsHistory = IsSettingsVisible = IsStationsVisible = IsServiceStationsVisible = false;

        switch (pageName)
        {
            case "Головна": 
                IsHomeVisible = true; 
                break;
            case "Додати машину": 
                IsAddCarVisible = true; 
                CarsVM?.LoadFromFile(); 
                break;
            case "Машина": 
                IsMyCarVisible = true; 
                MyCarsVM?.LoadCars(); 
                break;
            case "Історія поїздок": 
                IsHistory = true; 
                break;
            case "Налаштування": 
                IsSettingsVisible = true; 
                break;
            case "Заправки": 
                IsStationsVisible = true; 
                break;
            case "СТО": // ДОДАНО: Логіка переходу на екран СТО
                IsServiceStationsVisible = true;
                break;
        }
    }

    [RelayCommand]
    public void Logout()
    {
        IsMainContentVisible = false;
        IsLoginVisible = true;
        SessionManager.CurrentUsername = string.Empty;
        
        RecentTrips.Clear();
        AddTripVM = null;
        HistoryVM = null;
        CarsVM = null;
        MyCarsVM = null;
        StationsVM = null;
        ServiceStationsVM = null; // ДОДАНО: Очищення під час логауту
    }
}