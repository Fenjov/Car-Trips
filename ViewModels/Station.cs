namespace CarTrips.Models
{
    public class Station
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string StationType { get; set; } = string.Empty; // "АЗС" або "Електро"
    }
}