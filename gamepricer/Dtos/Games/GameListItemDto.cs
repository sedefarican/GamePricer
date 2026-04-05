namespace gamepricer.Dtos.Games
{
    public class GameListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? CoverImageUrl { get; set; }
        public decimal? LowestPrice { get; set; }
        /// <summary>Canlı fiyat (ITAD) için para birimi; yoksa varsayılan TRY kullanılır.</summary>
        public string? PriceCurrency { get; set; }
        /// <summary>Mağazalardaki en yüksek indirim yüzdesi (ITAD).</summary>
        public int? BestDiscountPercent { get; set; }
    }
}