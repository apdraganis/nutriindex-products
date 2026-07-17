namespace NutriIndex.Products.Models.DTOs;

public record ProductScoredEvent(
    Guid EventId,
    DateTime Timestamp,
    Guid CorrelationId,
    ProductScoredPayload Payload
);

public record ProductScoredPayload(
    string Barcode,
    ProductScores Scores
);

public record ProductScores(
    decimal CostPer100Kcal,
    decimal CostPer10gProtein
);
