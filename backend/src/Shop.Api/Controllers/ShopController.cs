using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shop.Api.Contracts;
using Shop.Api.Services;

namespace Shop.Api.Controllers;

// Customer-facing shop metadata (currently just the display name) — lets the frontend brand
// itself at runtime from whatever this shop instance's SHOP_NAME env var was set to, the same
// way it already gets the payment token contract address from Payments options, rather than
// hardcoding a name at build time.
[ApiController]
[Route("api/shop")]
public class ShopController(IOptions<ShopOptions> shopOptions) : ControllerBase
{
    [HttpGet]
    public ActionResult<ShopInfoDto> Get()
    {
        return Ok(new ShopInfoDto(shopOptions.Value.Name));
    }
}
