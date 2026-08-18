namespace Shop.Api.Services;

public class ShopOptions
{
    public const string SectionName = "Shop";

    /// <summary>
    /// Stands in for what would eventually come from the Shop CRD's name field
    /// (see the shop-operator repo) once ShopHub provisions it.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Short tagline describing the shop, shown alongside the name. Same provisioning
    /// story as <see cref="Name"/>.
    /// </summary>
    public required string Description { get; set; }
}
