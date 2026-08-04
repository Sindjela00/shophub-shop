namespace Shop.Api.Models;

/// <summary>
/// An anonymous, pre-checkout cart. Identified only by its Id (the client persists it,
/// e.g. in localStorage) — there is no customer login for the Shop app.
/// </summary>
public class Cart
{
    public Guid Id { get; set; }
    public List<CartItem> Items { get; set; } = [];
}
