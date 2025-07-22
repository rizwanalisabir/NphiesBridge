// File: NphiesBridge.Tests/Services/SimpleFuzzyMatchingServiceTests.cs
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Infrastructure.Repositories;
using NphiesBridge.Shared.DTOs;
using NphiesBridge.Tests.Helpers;
using System.Diagnostics;

namespace NphiesBridge.Tests.Services
{
    public class SimpleFuzzyMatchingServiceTests : IDisposable
    {
        private readonly Mock<ILogger<SimpleFuzzyMatchingService>> _mockLogger;
        private readonly IMemoryCache _memoryCache;
        private readonly SimpleFuzzyMatchingService _service;
        private readonly ApplicationDbContext _context;

        public SimpleFuzzyMatchingServiceTests()
        {
            _mockLogger = new Mock<ILogger<SimpleFuzzyMatchingService>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _context = TestDbContextFactory.CreateContextWithTestData();

            _service = new SimpleFuzzyMatchingService(_context, _memoryCache, _mockLogger.Object);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _memoryCache?.Dispose();
        }

        #region Basic Functionality Tests

        [Fact]
        public async Task GetAiSuggestionAsync_WithValidDiagnosis_ReturnsSuccessResult()
        {
            // Arrange
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = "diabetes type 2",
                HospitalCode = "DM2",
                SessionId = "test-session"
            };

            // Act
            var result = await _service.GetAiSuggestionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Success.Should().BeTrue();
            result.Data.SuggestedCode.Should().NotBeNull();
            result.Data.SuggestedCode.Code.Should().Be("E11.9");
            result.Data.Confidence.Should().BeGreaterThan(60);
        }

        [Fact]
        public async Task GetAiSuggestionAsync_WithHypertension_ReturnsCorrectMatch()
        {
            // Arrange
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = "high blood pressure",
                HospitalCode = "HTN",
                SessionId = "test-session"
            };

            // Act
            var result = await _service.GetAiSuggestionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Success.Should().BeTrue();
            result.Data.SuggestedCode.Code.Should().Be("I10");
            result.Data.Confidence.Should().BeGreaterThan(50);
        }

        [Fact]
        public async Task GetAiSuggestionAsync_WithAsthma_ReturnsCorrectMatch()
        {
            // Arrange
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = "asthma breathing problems",
                HospitalCode = "ASTH",
                SessionId = "test-session"
            };

            // Act
            var result = await _service.GetAiSuggestionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Success.Should().BeTrue();
            result.Data.SuggestedCode.Code.Should().Be("J45.9");
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task GetAiSuggestionAsync_WithEmptyDiagnosis_ReturnsLowConfidence()
        {
            // Arrange
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = "",
                HospitalCode = "EMPTY",
                SessionId = "test-session"
            };

            // Act
            var result = await _service.GetAiSuggestionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetAiSuggestionAsync_WithUnknownDiagnosis_ReturnsNoMatch()
        {
            // Arrange
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = "completely unknown rare disease xyz123",
                HospitalCode = "UNK",
                SessionId = "test-session"
            };

            // Act
            var result = await _service.GetAiSuggestionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Success.Should().BeFalse();
            result.Data.Confidence.Should().Be(0);
        }

        [Theory]
        [InlineData("Type 2 Diabetes")]
        [InlineData("type 2 diabetes")]
        [InlineData("TYPE 2 DIABETES")]
        [InlineData("diabetes type 2")]
        public async Task GetAiSuggestionAsync_WithDifferentCasing_ReturnsSameResult(string diagnosis)
        {
            // Arrange
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = diagnosis,
                HospitalCode = "DM2",
                SessionId = "test-session"
            };

            // Act
            var result = await _service.GetAiSuggestionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Success.Should().BeTrue();
            result.Data.SuggestedCode.Code.Should().Be("E11.9");
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task GetAiSuggestionAsync_FirstCall_LoadsCodesAndCompletes()
        {
            // Arrange
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = "diabetes",
                HospitalCode = "DM",
                SessionId = "perf-test"
            };

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _service.GetAiSuggestionAsync(request);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
        }

        [Fact]
        public async Task GetAiSuggestionAsync_SecondCall_UsesCache()
        {
            // Arrange
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = "diabetes",
                HospitalCode = "DM",
                SessionId = "cache-test"
            };

            // Act - First call
            await _service.GetAiSuggestionAsync(request);

            var stopwatch = Stopwatch.StartNew();
            var result = await _service.GetAiSuggestionAsync(request);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should be very fast from cache
        }

        #endregion

        #region Confidence Level Tests

        [Theory]
        [InlineData("Type 2 diabetes mellitus without complications", 95)] // Exact match
        [InlineData("diabetes type 2", 85)] // High similarity
        [InlineData("diabetes", 70)] // Moderate similarity
        public async Task GetAiSuggestionAsync_ReturnsExpectedConfidenceRange(string diagnosis, int expectedMinConfidence)
        {
            // Arrange
            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = diagnosis,
                HospitalCode = "DM",
                SessionId = "confidence-test"
            };

            // Act
            var result = await _service.GetAiSuggestionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Success.Should().BeTrue();
            result.Data.Confidence.Should().BeGreaterThanOrEqualTo(expectedMinConfidence);
        }

        #endregion

        #region Multiple Diagnoses Test

        [Fact]
        public async Task GetAiSuggestionAsync_WithMultipleSimilarDiagnoses_ReturnsHighestMatch()
        {
            // Arrange
            var testCases = new[]
            {
                new { Diagnosis = "diabetes", ExpectedCode = "E11.9" },
                new { Diagnosis = "hypertension", ExpectedCode = "I10" },
                new { Diagnosis = "asthma", ExpectedCode = "J45.9" },
                new { Diagnosis = "depression", ExpectedCode = "F32.9" }
            };

            // Act & Assert
            foreach (var testCase in testCases)
            {
                var request = new AiSuggestionRequestDto
                {
                    DiagnosisName = testCase.Diagnosis,
                    HospitalCode = "TEST",
                    SessionId = $"multi-test-{testCase.Diagnosis}"
                };

                var result = await _service.GetAiSuggestionAsync(request);

                result.Should().NotBeNull();
                result.IsSuccess.Should().BeTrue();
                result.Data.Success.Should().BeTrue();
                result.Data.SuggestedCode.Code.Should().Be(testCase.ExpectedCode);
            }
        }

        #endregion

        #region Large Dataset Performance Test

        [Fact]
        public async Task GetAiSuggestionAsync_WithLargeDataset_PerformsWell()
        {
            // Arrange - Create context with large dataset
            using var largeContext = TestDbContextFactory.CreateInMemoryContext("large-dataset");
            var largeDataset = TestDataHelper.GetLargeDataset();
            largeContext.NphiesIcdCodes.AddRange(largeDataset);
            await largeContext.SaveChangesAsync();

            var largeService = new SimpleFuzzyMatchingService(largeContext, _memoryCache, _mockLogger.Object);

            var request = new AiSuggestionRequestDto
            {
                DiagnosisName = "diabetes heart failure",
                HospitalCode = "LARGE",
                SessionId = "large-test"
            };

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await largeService.GetAiSuggestionAsync(request);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should complete within 10 seconds even with 1000 records
        }

        #endregion
    }
}