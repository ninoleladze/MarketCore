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

        if (await _context.Products.CountAsync(ct) >= 10)
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

            // --- Electronics ---
            Product.Create(
                "Portable Bluetooth Speaker",
                "360-degree surround sound with 24-hour battery life and IPX7 waterproof rating.",
                new Money(59.99m, "USD"), 120, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Wireless Ergonomic Mouse",
                "Silent-click ergonomic design with adjustable 800–3200 DPI and 18-month battery life.",
                new Money(44.99m, "USD"), 90, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "27-inch 4K IPS Monitor",
                "Factory-calibrated 4K display with 99% sRGB coverage and USB-C 65W power delivery.",
                new Money(349.99m, "USD"), 25, electronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1555617981-dac3880eac6e?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Smart LED Desk Lamp",
                "Touch-controlled desk lamp with 5 colour temperatures, USB charging port and auto-dimming.",
                new Money(29.99m, "USD"), 75, electronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Noise-Isolating Earbuds",
                "In-ear earbuds with passive noise isolation, 10mm dynamic drivers and braided cable.",
                new Money(19.99m, "USD"), 200, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1590658268037-6bf12165a8df?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Portable SSD 1TB",
                "USB 3.2 Gen 2 external SSD with 1050 MB/s read speed in an aluminium enclosure.",
                new Money(99.99m, "USD"), 55, electronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1597848212624-a19eb35e2651?auto=format&fit=crop&w=400&q=80"),

            // --- Clothing ---
            Product.Create(
                "Merino Wool Crew Sweater",
                "Fine-gauge merino wool pullover, naturally temperature-regulating and machine washable.",
                new Money(89.99m, "USD"), 60, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1576566588028-4147f3842f27?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Canvas Sneakers Low-Top",
                "Classic low-top canvas shoes with a vulcanised rubber sole and cushioned insole.",
                new Money(39.99m, "USD"), 130, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1525966222134-fcfa99b8ae77?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Leather Bifold Wallet",
                "Full-grain vegetable-tanned leather wallet with 6 card slots and RFID blocking lining.",
                new Money(34.99m, "USD"), 110, clothing.Id, seller1.Id,
                "https://images.unsplash.com/photo-1627123424574-724758594e93?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Packable Down Vest",
                "800-fill-power down vest that packs into its own pocket. Wind-resistant shell fabric.",
                new Money(69.99m, "USD"), 45, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1539533018447-63fcce2678e3?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Structured Baseball Cap",
                "6-panel cotton twill cap with adjustable buckle closure and UV protection.",
                new Money(22.99m, "USD"), 180, clothing.Id, seller1.Id,
                "https://images.unsplash.com/photo-1588850561407-ed78c282e89b?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Slim Leather Belt",
                "Genuine cowhide leather belt with a brushed-nickel buckle. Available in 32–42 inches.",
                new Money(27.99m, "USD"), 95, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1624222247342-4bef59bf3f0c?auto=format&fit=crop&w=400&q=80"),

            // --- Books ---
            Product.Create(
                "The Pragmatic Programmer",
                "David Thomas & Andrew Hunt — 20th Anniversary Edition covering modern software craftsmanship.",
                new Money(44.99m, "USD"), 85, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1550399105-c4db5fb85c18?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Designing Data-Intensive Applications",
                "Martin Kleppmann's guide to the principles behind reliable, scalable, and maintainable systems.",
                new Money(59.99m, "USD"), 70, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1519682337058-a94d519337bc?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "You Don't Know JS Yet (Vol. 1)",
                "Kyle Simpson's deep-dive into JavaScript's scope, closures, and the this keyword.",
                new Money(29.99m, "USD"), 120, books.Id, seller2.Id,
                "https://images.unsplash.com/photo-1516116216624-53e697fedbea?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Refactoring: Improving the Design of Existing Code",
                "Martin Fowler — the definitive catalogue of refactoring techniques with worked examples.",
                new Money(49.99m, "USD"), 65, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=400&q=80"),

            // --- Digital Electronics ---
            Product.Create(
                "Angular 21 Complete Course (eBook)",
                "Digital download — comprehensive guide covering standalone components, signals, and SSR.",
                new Money(24.99m, "USD"), 999, digitalElectronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1461749280684-dccba630e2f6?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                ".NET 8 Microservices Handbook (eBook)",
                "Digital download — practical guide to building production-grade microservices with .NET 8.",
                new Money(29.99m, "USD"), 999, digitalElectronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1498050108023-c5249f4df085?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "SQL Performance Explained (eBook)",
                "Digital download — Markus Winand's essential guide to indexing and query optimisation.",
                new Money(22.99m, "USD"), 999, digitalElectronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1544383835-bda2bc66a55d?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Clean Architecture in Practice (Video Course)",
                "Digital download — 8-hour video series implementing Clean Architecture with .NET and Angular.",
                new Money(34.99m, "USD"), 999, digitalElectronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1487017159836-4e23ece2e4cf?auto=format&fit=crop&w=400&q=80"),

            Product.Create(
                "Linux Command Line Mastery (eBook)",
                "Digital download — comprehensive coverage of Bash scripting, tools, and system administration.",
                new Money(17.99m, "USD"), 999, digitalElectronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1518432031352-d6fc5c10da5a?auto=format&fit=crop&w=400&q=80"),
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
