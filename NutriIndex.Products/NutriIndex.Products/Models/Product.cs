namespace NutriIndex.Products.Models;

public class Product
{
    public string Barcode { get; set; } = string.Empty; // Primary Key?
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;

    // Core computed efficiency metrics saved from the scoring engine
    public decimal CostPer100Kcal { get; set; }
    public decimal CostPer10gProtein { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
