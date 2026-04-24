using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CarTrips.Models;
using CarTrips.Services;

namespace CarTrips.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly ObservableCollection<Trip> _allTrips;
    private readonly TripService _tripService;

    [ObservableProperty] private ObservableCollection<Trip> _filteredTrips;
    [ObservableProperty] private string _searchDate;
    [ObservableProperty] private string _searchId;
    [ObservableProperty] private string _searchDistance;
    [ObservableProperty] private string _searchType;

    public int TotalTripsCount => _allTrips.Count;
    public string TotalDistanceAllTrips => _allTrips.Sum(t => int.TryParse(t?.Distance, out int d) ? d : 0).ToString();
    public int TotalSpentAllTrips => (int)_allTrips.Sum(t => t?.EstimatedCost ?? 0);

    public HistoryViewModel(ObservableCollection<Trip> trips, TripService service)
    {
        _allTrips = trips;
        _tripService = service;
        FilteredTrips = new ObservableCollection<Trip>(_allTrips);
        
        // Оновлюємо фільтр, коли змінюється основний список
        _allTrips.CollectionChanged += (s, e) => { SearchTrips(); UpdateStats(); };
    }


// Додайте ці методи в клас HistoryViewModel
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
        UpdateStats(); // Обов'язково додайте сюди
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
            _allTrips.Remove(tripToDelete);
            _tripService.SaveTrips(_allTrips);
        }
    }


}