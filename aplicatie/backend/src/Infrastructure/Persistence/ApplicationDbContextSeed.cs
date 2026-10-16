using Microsoft.EntityFrameworkCore;
using NutriScan.Domain.Entities;

namespace NutriScan.Infrastructure.Persistence;

public static class ApplicationDbContextSeed
{
    public static async Task SeedInitialDataAsync(ApplicationDbContext context)
    {
        if (await context.FoodItems.AnyAsync())
        {
            return;
        }

        var defaultFoods = new List<FoodItem>
        {
            new()
            {
                Name = "Piept de pui fără piele (crud)",
                Brand = "Generic",
                Calories = 120.0,
                Proteins = 22.5,
                Carbohydrates = 0.0,
                Fats = 2.6,
                Fiber = 0.0,
                SaltGrams = 0.1625, // from 65 mg sodium (salt = sodium × 2.5)
                IsVerified = true
            },
            new()
            {
                Name = "Fulgi de ovăz integrali",
                Brand = "Generic",
                Calories = 389.0,
                Proteins = 16.9,
                Carbohydrates = 66.3,
                Fats = 6.9,
                Fiber = 10.6,
                SaltGrams = 0.005, // from 2 mg sodium (salt = sodium × 2.5)
                IsVerified = true
            },
            new()
            {
                Name = "Orez alb basmati (crud)",
                Brand = "Generic",
                Calories = 356.0,
                Proteins = 7.1,
                Carbohydrates = 78.0,
                Fats = 0.9,
                Fiber = 1.3,
                SaltGrams = 0.0025, // from 1 mg sodium (salt = sodium × 2.5)
                IsVerified = true
            },
            new()
            {
                Name = "Ou de găină întreg",
                Brand = "Generic",
                Calories = 143.0,
                Proteins = 12.6,
                Carbohydrates = 0.7,
                Fats = 9.5,
                Fiber = 0.0,
                SaltGrams = 0.355, // from 142 mg sodium (salt = sodium × 2.5)
                IsVerified = true
            },
            new()
            {
                Name = "Banană proaspătă",
                Brand = "Generic",
                Calories = 89.0,
                Proteins = 1.1,
                Carbohydrates = 22.8,
                Fats = 0.3,
                Fiber = 2.6,
                SaltGrams = 0.0025, // from 1 mg sodium (salt = sodium × 2.5)
                IsVerified = true
            },
            new()
            {
                Name = "File de somon proaspăt",
                Brand = "Generic",
                Calories = 208.0,
                Proteins = 20.4,
                Carbohydrates = 0.0,
                Fats = 13.4,
                Fiber = 0.0,
                SaltGrams = 0.1475, // from 59 mg sodium (salt = sodium × 2.5)
                IsVerified = true
            },
            new()
            {
                Name = "Broccoli crud",
                Brand = "Generic",
                Calories = 34.0,
                Proteins = 2.8,
                Carbohydrates = 6.6,
                Fats = 0.4,
                Fiber = 2.6,
                SaltGrams = 0.0825, // from 33 mg sodium (salt = sodium × 2.5)
                IsVerified = true
            },
            new()
            {
                Name = "Ulei de măsline extravirgin",
                Brand = "Generic",
                Calories = 884.0,
                Proteins = 0.0,
                Carbohydrates = 0.0,
                Fats = 100.0,
                Fiber = 0.0,
                SaltGrams = 0.005, // from 2 mg sodium (salt = sodium × 2.5)
                IsVerified = true
            },
            new()
            {
                Name = "Iaurt grecesc 2% grăsime",
                Brand = "Generic",
                Calories = 73.0,
                Proteins = 10.0,
                Carbohydrates = 3.6,
                Fats = 2.0,
                Fiber = 0.0,
                SaltGrams = 0.1125, // from 45 mg sodium (salt = sodium × 2.5)
                IsVerified = true
            },
            new()
            {
                Name = "Migdale crude",
                Brand = "Generic",
                Calories = 579.0,
                Proteins = 21.2,
                Carbohydrates = 21.6,
                Fats = 49.9,
                Fiber = 12.5,
                SaltGrams = 0.0025, // from 1 mg sodium (salt = sodium × 2.5)
                IsVerified = true
            }
        };

        await context.FoodItems.AddRangeAsync(defaultFoods);
        await context.SaveChangesAsync();
    }
}
