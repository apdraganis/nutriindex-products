namespace NutriIndex.Products.Models.DTOs;

public record ProductScoredEvent(
    Guid CorrelationId,
    string Barcode,
    Dictionary<string, decimal> Scores
);

public record ProductScoredPayload(
    string Barcode,
    ProductScores Scores
);

public record ProductScores(
    decimal CostPer100Kcal,
    decimal CostPer10gProtein
);
