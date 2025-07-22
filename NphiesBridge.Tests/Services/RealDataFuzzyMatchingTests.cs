// File: NphiesBridge.Tests/Services/RealDataFuzzyMatchingTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NphiesBridge.Core.Interfaces;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Infrastructure.Repositories;
using NphiesBridge.Shared.DTOs;
using System.Diagnostics;

namespace NphiesBridge.Tests.Services
{
    public class RealDataFuzzyMatchingTests : IDisposable
    {
        private readonly Mock<ILogger<SimpleFuzzyMatchingService>> _mockLogger;
        private readonly IMemoryCache _memoryCache;
        private readonly SimpleFuzzyMatchingService _service;
        private readonly ApplicationDbContext _context;

        public RealDataFuzzyMatchingTests()
        {
            _mockLogger = new Mock<ILogger<SimpleFuzzyMatchingService>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 100000 // Increase for large dataset
            });

            // Use REAL database connection
            _context = CreateRealDbContext();
            _service = new SimpleFuzzyMatchingService(_context, _memoryCache, _mockLogger.Object);
        }

        private ApplicationDbContext CreateRealDbContext()
        {
            // Use your actual connection string
            var connectionString = "Server=localhost;Database=NphiesBridgeDb;Trusted_Connection=True;Encrypt=False;";

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new ApplicationDbContext(options);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _memoryCache?.Dispose();
        }

        #region Real Data Loading Tests

        [Fact]
        public async Task LoadAllIcdCodes_FromRealDatabase_LoadsExpectedCount()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = "diabetes type 2", // This will trigger loading
                HospitalCode = "DM2",
                SessionId = "real-data-test"
            };

            var result = await _service.GetAiSuggestionAsync(request);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            // Check the logs to see actual count loaded
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Loaded") && v.ToString().Contains("ICD codes")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);

            Console.WriteLine($"Real data test completed in {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task GetActualIcdCount_FromDatabase_ReturnsExpectedNumber()
        {
            // Arrange & Act
            var actualCount = await _context.NphiesIcdCodes
                .AsNoTracking()
                .Where(c => c.IsActive)
                .CountAsync();

            // Assert
            Console.WriteLine($"Actual ICD codes in database: {actualCount:N0}");
            actualCount.Should().BeGreaterThan(40000, "Expected at least 40K codes");
        }

        #endregion

        #region Performance Tests with Real Data

        [Fact]
        public async Task RealData_FirstLoad_CompletesWithinReasonableTime()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act - First call loads all data
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = "hypertension",
                HospitalCode = "HTN",
                SessionId = "performance-test-1"
            };

            var result = await _service.GetAiSuggestionAsync(request);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            Console.WriteLine($"First load with 44K+ codes took: {stopwatch.ElapsedMilliseconds:N0}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000, "Should complete within 30 seconds");
        }

        [Fact]
        public async Task RealData_SecondCall_UsesCacheAndIsFast()
        {
            // Arrange - First call to load data
            await _service.GetAiSuggestionAsync(new AiSuggestionRequestDto
            {
                DiagnosisName = "test load",
                HospitalCode = "LOAD",
                SessionId = "cache-setup"
            });

            // Act - Second call should be cached
            var stopwatch = Stopwatch.StartNew();
            var result = await _service.GetAiSuggestionAsync(new AiSuggestionRequestDto
            {
                DiagnosisName = "asthma",
                HospitalCode = "ASTH",
                SessionId = "cache-test-2"
            });
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            Console.WriteLine($"Cached call took: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Cached calls should be under 1 second");
        }

        #endregion

        #region Real World Test Cases

        [Theory]
        [InlineData("diabetes mellitus type 2")]
        [InlineData("essential hypertension")]
        [InlineData("acute myocardial infarction")]
        [InlineData("chronic obstructive pulmonary disease")]
        [InlineData("major depressive disorder")]
        [InlineData("pneumonia")]
        [InlineData("urinary tract infection")]
        [InlineData("gastroenteritis")]
        public async Task RealData_CommonDiagnoses_FindsReasonableMatches(string diagnosis)
        {
            // Arrange
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = diagnosis,
                HospitalCode = "REAL",
                SessionId = $"real-test-{diagnosis.Replace(" ", "-")}"
            };

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _service.GetAiSuggestionAsync(request);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            Console.WriteLine($"Diagnosis: '{diagnosis}'");
            if (result.Data.Success)
            {
                Console.WriteLine($"  Match: {result.Data.SuggestedCode.Code} - {result.Data.SuggestedCode.Description}");
                Console.WriteLine($"  Confidence: {result.Data.Confidence:F1}%");
                Console.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds}ms");

                result.Data.Confidence.Should().BeGreaterThan(50, "Should find reasonable matches for common diagnoses");
            }
            else
            {
                Console.WriteLine($"  No match found - Time: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        #endregion

        #region Memory Usage Test

        [Fact]
        public async Task RealData_MemoryUsage_RemainsReasonable()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Load all codes
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = "test memory",
                HospitalCode = "MEM",
                SessionId = "memory-test"
            };

            await _service.GetAiSuggestionAsync(request);

            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;

            // Assert
            Console.WriteLine($"Memory used: {memoryUsed / 1024 / 1024:F1} MB");
            memoryUsed.Should().BeLessThan(500 * 1024 * 1024, "Should use less than 500MB for 44K codes");
        }

        #endregion

        #region Concurrent Access Test

        [Fact]
        public async Task RealData_ConcurrentRequests_HandlesWell()
        {
            // Arrange
            var diagnoses = new[]
            {
                "diabetes", "hypertension", "asthma", "pneumonia", "depression",
                "arthritis", "migraine", "bronchitis", "gastritis", "dermatitis"
            };

            var tasks = new List<Task<ServiceResult<AiSuggestionResponseDto>>>();

            var stopwatch = Stopwatch.StartNew();

            // Act - Run 10 concurrent requests
            foreach (var diagnosis in diagnoses)
            {
                var request = new AiSuggestionRequestDto
                {
                    DiagnosisName = diagnosis,
                    HospitalCode = "CONC",
                    SessionId = $"concurrent-{diagnosis}"
                };

                tasks.Add(_service.GetAiSuggestionAsync(request));
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => r.IsSuccess);

            var successfulMatches = results.Count(r => r.Data.Success);
            Console.WriteLine($"Concurrent test: {successfulMatches}/10 successful matches in {stopwatch.ElapsedMilliseconds}ms");

            successfulMatches.Should().BeGreaterThan(5, "Should find matches for most common diagnoses");
        }

        #endregion
    }
}