using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NutriIndex.Products.Data;
using NutriIndex.Products.Services;
using NutriIndex.Products.Services.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Register catalog business logic service
builder.Services.AddScoped<ProductCatalogService>();
// Register background consumer worker to start up automatically
builder.Services.AddHostedService<ProductScoredConsumer>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Auto-migrate db on startup
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
