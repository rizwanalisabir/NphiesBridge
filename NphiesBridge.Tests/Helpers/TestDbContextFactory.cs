// File: NphiesBridge.Tests/Helpers/TestDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using NphiesBridge.Infrastructure.Data;

namespace NphiesBridge.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext CreateInMemoryContext(string databaseName = null)
        {
            var dbName = databaseName ?? Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new ApplicationDbContext(options);

            // Ensure database is created
            context.Database.EnsureCreated();

            return context;
        }

        public static ApplicationDbContext CreateContextWithTestData(string databaseName = null)
        {
            var context = CreateInMemoryContext(databaseName);

            // Add test data
            var testData = TestDataHelper.GetSampleIcdCodes();
            context.NphiesIcdCodes.AddRange(testData);
            context.SaveChanges();

            return context;
        }
    }
}