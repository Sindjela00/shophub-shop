namespace Shop.Api.Services;

public class ShopOptions
{
    /// <summary>
    /// Display name for this shop instance, set by shophub-shop-operator via the flat
    /// SHOP_NAME env var on the container (not a nested Shop__Name key, unlike Payments'
    /// options). Falls back to a generic default when unset, e.g. local dev without the
    /// env var configured.
    /// </summary>
    public string Name { get; set; } = "ShopHub Store";
}
