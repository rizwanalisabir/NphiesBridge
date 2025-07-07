namespace NphiesBridge.Shared.Helpers
{
    public static class ColorHelper
    {
        private static readonly Random _random = new Random();

        // Option 1: Pure random hex color
        public static string GetRandomColor()
        {
            return _random.Next(0x1000000).ToString("X6").PadLeft(6, '0');
        }

        // Option 2: Random from predefined nice colors
        public static string GetRandomNiceColor()
        {
            string[] colors = {
            "667eea", "764ba2", "667db6", "0082c8",
            "ee9ca7", "ffdde1", "43cea2", "185a9d",
            "a8edea", "d299c2", "fad0c4", "ffd1ff",
            "ffecd2", "fcb69f", "c471ed", "f64f59"
        };
            return colors[_random.Next(colors.Length)];
        }

        // Option 3: Generate similar colors to 667eea (purple/blue range)
        public static string GetSimilarColor()
        {
            // Generate colors in purple/blue spectrum
            int red = _random.Next(80, 120);   // 50-78 hex
            int green = _random.Next(100, 140); // 64-8C hex  
            int blue = _random.Next(200, 255);  // C8-FF hex

            return $"{red:X2}{green:X2}{blue:X2}";
        }

        // Option 4: Generate user-specific color (deterministic based on user)
        public static string GetUserColor(string userId)
        {
            // Create consistent color for same user
            var hash = userId.GetHashCode();
            var color = Math.Abs(hash) % 0x1000000;
            return color.ToString("X6").PadLeft(6, '0');
        }

        // Option 5: Material Design inspired colors
        public static string GetMaterialColor()
        {
            string[] materialColors = {
            "667eea", // Indigo
            "764ba2", // Purple  
            "42a5f5", // Blue
            "26c6da", // Cyan
            "66bb6a", // Green
            "ffca28", // Amber
            "ff7043", // Deep Orange
            "ec407a", // Pink
            "ab47bc", // Purple
            "5c6bc0"  // Indigo
        };
            return materialColors[_random.Next(materialColors.Length)];
        }
    }
}
