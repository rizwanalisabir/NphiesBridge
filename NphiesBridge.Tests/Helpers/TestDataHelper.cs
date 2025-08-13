// File: NphiesBridge.Tests/Helpers/TestDataHelper.cs
using NphiesBridge.Core.Entities.IcdMapping;

namespace NphiesBridge.Tests.Helpers
{
    public static class TestDataHelper
    {
        public static List<NphiesServiceCode> GetSampleIcdCodes()
        {
            return new List<NphiesServiceCode>
            {
                new NphiesServiceCode
                {
                    Id = Guid.NewGuid(),
                    Code = "E11.9",
                    Description = "Type 2 diabetes mellitus without complications",
                    Category = "Endocrine diseases",
                    Chapter = "Chapter IV",
                    IsActive = true
                },
                new NphiesServiceCode
                {
                    Id = Guid.NewGuid(),
                    Code = "I10",
                    Description = "Essential hypertension",
                    Category = "Circulatory diseases",
                    Chapter = "Chapter IX",
                    IsActive = true
                },
                new NphiesServiceCode
                {
                    Id = Guid.NewGuid(),
                    Code = "J45.9",
                    Description = "Asthma, unspecified",
                    Category = "Respiratory diseases",
                    Chapter = "Chapter X",
                    IsActive = true
                },
                new NphiesServiceCode
                {
                    Id = Guid.NewGuid(),
                    Code = "K59.0",
                    Description = "Constipation",
                    Category = "Digestive diseases",
                    Chapter = "Chapter XI",
                    IsActive = true
                },
                new NphiesServiceCode
                {
                    Id = Guid.NewGuid(),
                    Code = "F32.9",
                    Description = "Major depressive disorder, single episode, unspecified",
                    Category = "Mental disorders",
                    Chapter = "Chapter V",
                    IsActive = true
                },
                new NphiesServiceCode
                {
                    Id = Guid.NewGuid(),
                    Code = "INACTIVE",
                    Description = "Inactive code for testing",
                    Category = "Test",
                    Chapter = "Test",
                    IsActive = false // This should be filtered out
                }
            };
        }

        public static List<NphiesServiceCode> GetLargeDataset()
        {
            var codes = new List<NphiesServiceCode>();
            var random = new Random(12345); // Fixed seed for reproducible tests

            // Generate 1000 test codes for performance testing
            for (int i = 0; i < 1000; i++)
            {
                codes.Add(new NphiesServiceCode
                {
                    Id = Guid.NewGuid(),
                    Code = $"T{i:D3}.{random.Next(0, 9)}",
                    Description = GenerateRandomMedicalDescription(random),
                    Category = $"Category {i % 10}",
                    Chapter = $"Chapter {i % 20}",
                    IsActive = true
                });
            }

            return codes;
        }

        private static string GenerateRandomMedicalDescription(Random random)
        {
            var medicalTerms = new[]
            {
                "diabetes", "hypertension", "asthma", "pneumonia", "bronchitis",
                "infection", "inflammation", "disorder", "syndrome", "disease",
                "acute", "chronic", "mild", "moderate", "severe", "unspecified",
                "type 1", "type 2", "primary", "secondary", "essential"
            };

            var bodyParts = new[]
            {
                "heart", "lung", "kidney", "liver", "brain", "blood",
                "respiratory", "cardiovascular", "gastrointestinal", "nervous"
            };

            var conditions = new[]
            {
                "failure", "insufficiency", "dysfunction", "malfunction",
                "stenosis", "obstruction", "bleeding", "pain"
            };

            var term1 = medicalTerms[random.Next(medicalTerms.Length)];
            var term2 = bodyParts[random.Next(bodyParts.Length)];
            var term3 = conditions[random.Next(conditions.Length)];

            return $"{term1} {term2} {term3}".Trim();
        }
    }
}