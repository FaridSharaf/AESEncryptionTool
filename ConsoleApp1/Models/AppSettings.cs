namespace ConsoleApp1.Models
{
    public class AppSettings
    {
        public bool AutoCopy { get; set; } = true;
        public bool AutoDetect { get; set; } = true;
        public int RecentItemsCount { get; set; } = 10;
        public int MaxHistoryItems { get; set; } = 500;
        public int MaxBookmarkItems { get; set; } = 100;
        public string Theme { get; set; } = "Light";
        public double WindowWidth { get; set; } = 1000;
        public double WindowHeight { get; set; } = 700;
        public double WindowLeft { get; set; } = 100;
        public double WindowTop { get; set; } = 100;
        public bool IsMaximized { get; set; } = false;
    }
}



