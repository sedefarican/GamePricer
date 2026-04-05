namespace gamepricer.Configuration
{
    public class ItadOptions
    {
        public const string SectionName = "ITAD";

        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.isthereanydeal.com";
        /// <summary>ISO 3166-1 alpha-2 (ör. TR, US).</summary>
        public string DefaultCountry { get; set; } = "US";
    }
}
