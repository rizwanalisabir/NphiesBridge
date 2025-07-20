using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NphiesBridge.Shared.DTOs
{
    public class AiSuggestionResponseDto
    {
        public bool Success { get; set; }
        public NphiesCodeDto? SuggestedCode { get; set; }
        public double Confidence { get; set; }
        public string MatchType { get; set; } = string.Empty;
        public string? Message { get; set; }
        public List<NphiesCodeDto> AlternativeSuggestions { get; set; } = new List<NphiesCodeDto>();
    }
}
