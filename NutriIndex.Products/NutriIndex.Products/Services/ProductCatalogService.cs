using Microsoft.EntityFrameworkCore;
using NutriIndex.Products.Data;
using NutriIndex.Products.Models;
using NutriIndex.Products.Models.DTOs;

namespace NutriIndex.Products.Services;

public class ProductCatalogService(AppDbContext _context)
{
    public async Task HandleProductScoredAsync(ProductScoredEvent scoredEvent)
    {
        // 1. Try to find an existing product record
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == scoredEvent.Barcode);

        if (product == null)
        {
            // 2. Map new event payload to a new entity
            product = new Product
            {
                Barcode = scoredEvent.Barcode,
                CostPer100Kcal = scoredEvent.Scores.GetValueOrDefault("KCAL"),
                CostPer10gProtein = scoredEvent.Scores.GetValueOrDefault("PROTEIN_G"),
                LastUpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
        }
        else
        {
            // 3. Map and update existing entity scores
            product.CostPer100Kcal = scoredEvent.Scores.GetValueOrDefault("KCAL");
            product.CostPer10gProtein = scoredEvent.Scores.GetValueOrDefault("PROTEIN_G");
            product.LastUpdatedAt = DateTime.UtcNow;
        }

        // 4. Commit changes to postgres
        await _context.SaveChangesAsync();
    }
}
