using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shop.Api.Contracts;
using Shop.Api.Services;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/shop")]
public class ShopController(IOptions<ShopOptions> options) : ControllerBase
{
    // Customer-facing: the storefront reads this to render the shop's name instead of
    // hardcoding it, so it can vary per shop instance.
    [HttpGet]
    public ActionResult<ShopDto> Get() => Ok(new ShopDto(options.Value.Name, options.Value.Description));
}
