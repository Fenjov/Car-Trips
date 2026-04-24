

using System.Linq;

namespace CarTrips.Models;

public class Car
{
    public string Brand { get; set; } = string.Empty;
    public string EngineVolume { get; set; } = string.Empty;
    public string LastService { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public double FuelConsumption { get; set; }
    public string Mileage { get; set; } = string.Empty;

    // Властивість для розрахунку витрат (не зберігається в JSON, рахується "на льоту")
    public double TotalSpent
    {
        get
        {
            string cleanMileage = new string(Mileage?.Where(char.IsDigit).ToArray() ?? new char[0]);
            if (double.TryParse(cleanMileage, out double mileageValue))
            {
                double pricePerLiter = (FuelType == "Бензин") ? 55.0 : (FuelType == "Дизель" ? 52.0 : 0);
                return (mileageValue / 100.0) * FuelConsumption * pricePerLiter;
            }
            return 0;
        }
    }
}
