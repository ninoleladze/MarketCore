using MarketCore.Application.Interfaces;
using MarketCore.Domain.Entities;
using MarketCore.Domain.Enums;
using MarketCore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketCore.Infrastructure.Persistence.Seeders;

public sealed class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {

        await _context.Database.MigrateAsync(ct);

        if (await _context.Users.AnyAsync(ct))
        {
            _logger.LogInformation("Database already seeded — skipping.");
            return;
        }

        _logger.LogInformation("Seeding database with development data...");

        var categories = SeedCategories();
        await _context.Categories.AddRangeAsync(categories, ct);
        await _context.SaveChangesAsync(ct);

        var users = SeedUsers();
        await _context.Users.AddRangeAsync(users, ct);
        await _context.SaveChangesAsync(ct);

        var (admin, seller1, seller2, buyer1, buyer2, buyer3) = (
            users[0], users[1], users[2], users[3], users[4], users[5]);

        var (electronics, clothing, books, digitalElectronics) = (
            categories[0], categories[1], categories[2], categories[3]);

        var products = SeedProducts(electronics, clothing, books, digitalElectronics, seller1, seller2);
        await _context.Products.AddRangeAsync(products, ct);
        await _context.SaveChangesAsync(ct);

        var carts = SeedCarts(buyer1, buyer2, buyer3, products);
        await _context.Carts.AddRangeAsync(carts, ct);
        await _context.SaveChangesAsync(ct);

        var orders = SeedOrders(buyer1, buyer2, products);
        await _context.Orders.AddRangeAsync(orders, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Database seeding complete.");
    }

    private List<Category> SeedCategories()
    {
        var electronics = Category.Create("Electronics", "Electronic devices and accessories");
        var clothing = Category.Create("Clothing", "Apparel and accessories");
        var books = Category.Create("Books", "Physical and digital books");

        var digitalElectronics = Category.Create("Digital Electronics", "Software, e-books, digital media", electronics.Id);

        return new List<Category> { electronics, clothing, books, digitalElectronics };
    }

    private List<User> SeedUsers()
    {
        var hash = _passwordHasher.Hash("Password123!");

        var admin = User.Create(
            new Email("admin@marketcore.dev"),
            hash,
            "Adam",
            "Admin",
            UserRole.Admin);

        var seller1 = User.Create(
            new Email("seller1@marketcore.dev"),
            hash,
            "Sarah",
            "Seller",
            UserRole.Customer);

        var seller2 = User.Create(
            new Email("seller2@marketcore.dev"),
            hash,
            "Mark",
            "Merchant",
            UserRole.Customer);

        var buyer1 = User.Create(
            new Email("buyer1@marketcore.dev"),
            hash,
            "Alice",
            "Buyer",
            UserRole.Customer);

        var buyer2 = User.Create(
            new Email("buyer2@marketcore.dev"),
            hash,
            "Bob",
            "Customer",
            UserRole.Customer);

        var buyer3 = User.Create(
            new Email("buyer3@marketcore.dev"),
            hash,
            "Carol",
            "Shopper",
            UserRole.Customer);

        foreach (var user in new[] { admin, seller1, seller2, buyer1, buyer2, buyer3 })
            user.MarkEmailVerified();

        return new List<User> { admin, seller1, seller2, buyer1, buyer2, buyer3 };
    }

    private static List<Product> SeedProducts(
        Category electronics,
        Category clothing,
        Category books,
        Category digitalElectronics,
        User seller1,
        User seller2)
    {
        return new List<Product>
        {
            Product.Create(
                "Wireless Noise-Cancelling Headphones",
                "Premium over-ear headphones with 30-hour battery life and active noise cancellation.",
                new Money(149.99m, "USD"), 45, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1505740420509-e7ef5fbe9a74?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "USB-C Fast Charger 65W",
                "GaN technology fast charger compatible with laptops, tablets, and smartphones.",
                new Money(34.99m, "USD"), 200, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1603371466160-7c04c74e7b55?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Mechanical Keyboard TKL",
                "Tenkeyless mechanical keyboard with Cherry MX Blue switches and RGB backlighting.",
                new Money(89.99m, "USD"), 30, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1587829741301-dc798b83add3?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "4K Webcam",
                "Ultra HD webcam with built-in ring light and noise-cancelling microphone.",
                new Money(79.99m, "USD"), 60, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Premium Cotton T-Shirt",
                "100% organic cotton crew-neck t-shirt, available in 12 colours.",
                new Money(24.99m, "USD"), 150, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Slim Fit Chino Pants",
                "Stretch-fabric chinos with a tailored slim fit. Machine washable.",
                new Money(49.99m, "USD"), 80, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1542272604-787c3835535d?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Waterproof Running Jacket",
                "Lightweight windbreaker with taped seams and reflective detailing.",
                new Money(74.99m, "USD"), 40, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1551698618-1dfe5d97d256?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Clean Code: A Handbook of Agile Software",
                "Robert C. Martin's definitive guide to writing clean, maintainable code.",
                new Money(39.99m, "USD"), 100, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1532012197267-da84d127e765?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Domain-Driven Design",
                "Eric Evans — Tackling Complexity in the Heart of Software.",
                new Money(54.99m, "USD"), 75, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Software Architecture Patterns (eBook)",
                "Digital download — five essential architecture patterns for modern distributed systems.",
                new Money(19.99m, "USD"), 999, digitalElectronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1484417894907-623942c8ee29?auto=format&fit=crop&w=400&q=80"),
        };
    }

    private static List<Cart> SeedCarts(User buyer1, User buyer2, User buyer3, List<Product> products)
    {

        var cart1 = Cart.Create(buyer1.Id);
        cart1.AddItem(products[0].Id, 1, products[0].Price);
        cart1.AddItem(products[7].Id, 2, products[7].Price);

        var cart2 = Cart.Create(buyer2.Id);

        var cart3 = Cart.Create(buyer3.Id);
        cart3.AddItem(products[4].Id, 3, products[4].Price);

        return new List<Cart> { cart1, cart2, cart3 };
    }

    private static List<Order> SeedOrders(User buyer1, User buyer2, List<Product> products)
    {
        var address1 = new Address("100 Tech Boulevard", "San Francisco", "CA", "94102", "US");
        var address2 = new Address("22 Commerce Street", "New York", "NY", "10001", "US");

        var order1 = Order.Create(buyer1.Id, address1);
        order1.AddItem(products[2].Id, products[2].Name, 1, products[2].Price);
        order1.AddItem(products[8].Id, products[8].Name, 1, products[8].Price);
        order1.Confirm();

        var order2 = Order.Create(buyer2.Id, address2);
        order2.AddItem(products[4].Id, products[4].Name, 2, products[4].Price);
        order2.AddItem(products[5].Id, products[5].Name, 1, products[5].Price);
        order2.Confirm();
        order2.Ship();

        return new List<Order> { order1, order2 };
    }
}
