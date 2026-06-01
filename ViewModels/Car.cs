using System;
using System.Linq;

namespace CarTrips.Models
{
    public class Car
    {
        public string Brand { get; set; } = string.Empty;
        public string EngineVolume { get; set; } = string.Empty;
        public string LastService { get; set; } = string.Empty;
        public string FuelType { get; set; } = string.Empty;
        public double FuelConsumption { get; set; }
        public string Mileage { get; set; } = string.Empty;

        public double TotalSpent
        {
            get
            {
                // ЯКЩО ТИП ПАЛИВА ЕЛЕКТРО — ВИТРАТИ ЗАВЖДИ 0
                if (FuelType != null && FuelType.Contains("Електро", StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }

                // Витягуємо лише цифри з пробігу (наприклад, з "80 000 км" робимо "80000")
                var digits = Mileage?.Where(char.IsDigit).ToArray();
                if (digits != null && digits.Length > 0 && double.TryParse(new string(digits), out double mileageValue))
                {
                    // Визначаємо ціну палива
                    double price = FuelType.Contains("Дизель") ? 52.0 : 55.0;
                    return (mileageValue / 100.0) * FuelConsumption * price;
                }
                return 0;
            }
        }

        /// <summary>
        /// Метод для безпечного оновлення пробігу (додавання або віднімання кілометрів)
        /// </summary>
        public void UpdateMileage(double kilometersDelta)
        {
            var digits = Mileage?.Where(char.IsDigit).ToArray();
            double currentMileage = 0;
            
            if (digits != null && digits.Length > 0)
            {
                double.TryParse(new string(digits), out currentMileage);
            }

            double newMileage = currentMileage + kilometersDelta;
            if (newMileage < 0) newMileage = 0;

            // Зберігаємо назад у строку. Якщо старий рядок містив "км", підтримуємо цей стиль
            if (Mileage != null && Mileage.Contains("км"))
            {
                Mileage = $"{newMileage:N0} км";
            }
            else
            {
                Mileage = newMileage.ToString();
            }
        }
    }
}