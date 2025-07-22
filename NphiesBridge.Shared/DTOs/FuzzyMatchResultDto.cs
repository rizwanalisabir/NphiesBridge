namespace NphiesBridge.Shared.DTOs
{
    public class FuzzyMatchResultDto
    {
        public string IcdCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
        public int Rank { get; set; }
    }
}
