using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NutriScan.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Design-time connection string for generating migrations, read from the environment
        // (same variable the API uses) so no credentials are stored in source control.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Variabila de mediu 'ConnectionStrings__DefaultConnection' nu este setată. " +
                "Exemplu: ConnectionStrings__DefaultConnection=\"Host=localhost;Port=5432;Database=nutriscan_db;" +
                "Username=nutriscan_user;Password=<din aplicatie/.env>\" dotnet ef migrations add <Nume>");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
        );

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
