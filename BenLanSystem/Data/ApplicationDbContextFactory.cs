using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BenLanSystem.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=BenLanDB_Dev;User Id=sa;Password=BenLan@Dev2026!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
