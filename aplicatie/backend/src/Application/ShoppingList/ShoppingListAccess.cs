using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.ShoppingList;

/// <summary>
/// Loading and saving shopping list lines. The list is used one-handed in a store, so a double tap or a second tab
/// can delete a line between this request's read and its write; the saves below turn that race into a clean answer.
/// </summary>
public static class ShoppingListAccess
{
    internal static Task<List<ShoppingListItem>> LoadAllAsync(this IApplicationDbContext db, Guid userId, CancellationToken cancellationToken) =>
        db.ShoppingListItems.Where(i => i.UserId == userId).ToListAsync(cancellationToken);

    /// <summary>Loads one of the user's items; others' are reported as not found.</summary>
    internal static async Task<ShoppingListItem> LoadOwnAsync(this IApplicationDbContext db, Guid userId, Guid itemId, CancellationToken cancellationToken) =>
        await db.ShoppingListItems.SingleOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, cancellationToken)
        ?? throw new NotFoundException(ShoppingListErrors.NotFound);

    /// <summary>Saves a change to one line; a line deleted meanwhile is reported as not found (404), as in the diary.</summary>
    public static async Task SaveOrNotFoundAsync(this IApplicationDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new NotFoundException(ShoppingListErrors.NotFound);
        }
    }

    /// <summary>Saves deletions; lines another request deleted first are already where this request wanted them.</summary>
    public static async Task SaveDeletesAsync(this IApplicationDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The transaction rolled back, but the concurrent request removed the same lines: the end state is the same.
        }
    }

    /// <summary>Saves a regenerated list; if another generation replaced the lines first, the caller gets 409 and retries.</summary>
    public static async Task SaveOrConflictAsync(this IApplicationDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(ShoppingListErrors.ConcurrentChange);
        }
    }
}
