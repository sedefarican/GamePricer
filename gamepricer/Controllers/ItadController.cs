using gamepricer.Dtos.Itad;
using gamepricer.Services;
using Microsoft.AspNetCore.Mvc;

namespace gamepricer.Controllers
{
    [ApiController]
    [Route("itad")]
    public class ItadController : ControllerBase
    {
        private readonly IItadApiClient _itad;

        public ItadController(IItadApiClient itad)
        {
            _itad = itad;
        }

        /// <summary>IsThereAnyDeal üzerinde başlığa göre arama.</summary>
        [HttpGet("search")]
        public async Task<ActionResult<IReadOnlyList<ItadSearchItemDto>>> Search(
            [FromQuery] string title,
            [FromQuery] int results = 20,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest(new { message = "title parametresi gerekli." });

            try
            {
                var items = await _itad.SearchGamesAsync(title.Trim(), results, cancellationToken).ConfigureAwait(false);
                return Ok(items);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { message = "ITAD API'ye ulaşılamadı.", detail = ex.Message });
            }
        }

        /// <summary>ITAD oyun kimlikleri için güncel mağaza fiyatları (country: ISO ülke kodu, örn. TR).</summary>
        [HttpPost("prices")]
        public async Task<ActionResult<IReadOnlyList<ItadPricesByGameDto>>> Prices(
            [FromBody] IReadOnlyList<string> ids,
            [FromQuery] string? country = null,
            CancellationToken cancellationToken = default)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { message = "En az bir oyun kimliği gerekli." });

            try
            {
                var blocks = await _itad.GetPricesForGamesAsync(ids, country ?? "", cancellationToken).ConfigureAwait(false);
                return Ok(blocks);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { message = "ITAD API'ye ulaşılamadı.", detail = ex.Message });
            }
        }
    }
}
