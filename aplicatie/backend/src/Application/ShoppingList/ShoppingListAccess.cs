using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.ShoppingList;

internal static class ShoppingListAccess
{
    public static Task<List<ShoppingListItem>> LoadAllAsync(this IApplicationDbContext db, Guid userId, CancellationToken cancellationToken) =>
        db.ShoppingListItems.Where(i => i.UserId == userId).ToListAsync(cancellationToken);

    /// <summary>Loads one of the user's items; others' are reported as not found.</summary>
    public static async Task<ShoppingListItem> LoadOwnAsync(this IApplicationDbContext db, Guid userId, Guid itemId, CancellationToken cancellationToken) =>
        await db.ShoppingListItems.SingleOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, cancellationToken)
        ?? throw new NotFoundException(ShoppingListErrors.NotFound);
}
