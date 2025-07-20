using NphiesBridge.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NphiesBridge.Shared.Models
{
    public class FuzzyMatchResult
    {
        public NphiesCodeDto NphiesCode { get; set; } = null!;
        public double SimilarityScore { get; set; }
        public string MatchType { get; set; } = string.Empty;
        public Dictionary<string, object> MatchDetails { get; set; } = new Dictionary<string, object>();
    }
}
