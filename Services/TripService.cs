using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CarTrips.Models;
using CarTrips.ViewModels;

namespace CarTrips.Services;

public class TripService
{
    private readonly string _jsonFilePath = "my_new_trips.json";

    public List<Trip> LoadTrips()
    {
        try
        {
            if (File.Exists(_jsonFilePath))
            {
                string json = File.ReadAllText(_jsonFilePath);
                if (string.IsNullOrWhiteSpace(json)) return new List<Trip>();

                var trips = JsonSerializer.Deserialize<List<Trip>>(json);
                return trips ?? new List<Trip>();
            }
        }
        catch { /* Логування помилки за потреби */ }
        return new List<Trip>();
    }

    public void SaveTrips(ObservableCollection<Trip> trips)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(trips, options);
            File.WriteAllText(_jsonFilePath, json);
        }
        catch (Exception ex)
        {
            throw new Exception("Помилка збереження файлу: " + ex.Message);
        }
    }
}