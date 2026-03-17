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
        if (await _context.Products.CountAsync(ct) >= 55)
        {
            _logger.LogInformation("Database already seeded — skipping.");
            return;
        }

        _logger.LogInformation("Seeding database with development data...");

        // ── Categories ──────────────────────────────────────────────────
        if (!await _context.Categories.AnyAsync(ct))
        {
            var cats = SeedCategories();
            await _context.Categories.AddRangeAsync(cats, ct);
            await _context.SaveChangesAsync(ct);
        }

        // ── Users ────────────────────────────────────────────────────────
        if (!await _context.Users.AnyAsync(ct))
        {
            var users = SeedUsers();
            await _context.Users.AddRangeAsync(users, ct);
            await _context.SaveChangesAsync(ct);
        }

        // ── Load existing entities ────────────────────────────────────────
        var electronics        = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Electronics", ct);
        var clothing           = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Clothing", ct);
        var books              = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Books", ct);
        var digitalElectronics = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Digital Electronics", ct);

        if (electronics is null || clothing is null || books is null || digitalElectronics is null)
        {
            _logger.LogWarning("Required seed categories not found, skipping product seeding.");
            return;
        }

        var seller1 = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == "Sarah", ct);
        var seller2 = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == "Mark", ct);
        var buyer1  = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == "Alice", ct);
        var buyer2  = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == "Bob", ct);
        var buyer3  = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == "Carol", ct);

        if (seller1 is null || seller2 is null || buyer1 is null || buyer2 is null || buyer3 is null)
        {
            _logger.LogWarning("Required seed users not found, skipping product seeding.");
            return;
        }

        // ── Products (additive — skip names that already exist) ───────────
        var existingNames = (await _context.Products.Select(p => p.Name).ToListAsync(ct)).ToHashSet();

        var allProducts = SeedProducts(electronics, clothing, books, digitalElectronics, seller1, seller2);
        var newProducts = allProducts.Where(p => !existingNames.Contains(p.Name)).ToList();

        if (newProducts.Count > 0)
        {
            await _context.Products.AddRangeAsync(newProducts, ct);
            await _context.SaveChangesAsync(ct);
        }

        // ── Carts / Orders (only if none exist) ───────────────────────────
        var allProducts2 = await _context.Products.ToListAsync(ct);

        if (!await _context.Carts.AnyAsync(ct))
        {
            var carts = SeedCarts(buyer1, buyer2, buyer3, allProducts2);
            await _context.Carts.AddRangeAsync(carts, ct);
            await _context.SaveChangesAsync(ct);
        }

        if (!await _context.Orders.AnyAsync(ct))
        {
            var orders = SeedOrders(buyer1, buyer2, allProducts2);
            await _context.Orders.AddRangeAsync(orders, ct);
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Database seeding complete.");
    }

    private List<Category> SeedCategories()
    {
        var electronics        = Category.Create("Electronics", "Electronic devices and accessories");
        var clothing           = Category.Create("Clothing", "Apparel and accessories");
        var books              = Category.Create("Books", "Physical and digital books");
        var digitalElectronics = Category.Create("Digital Electronics", "Software, e-books, digital media", electronics.Id);
        return [electronics, clothing, books, digitalElectronics];
    }

    private List<User> SeedUsers()
    {
        var hash = _passwordHasher.Hash("Password123!");

        var admin   = User.Create(new Email("admin@marketcore.dev"),   hash, "Adam",  "Admin",    UserRole.Admin);
        var seller1 = User.Create(new Email("seller1@marketcore.dev"), hash, "Sarah", "Seller",   UserRole.Customer);
        var seller2 = User.Create(new Email("seller2@marketcore.dev"), hash, "Mark",  "Merchant", UserRole.Customer);
        var buyer1  = User.Create(new Email("buyer1@marketcore.dev"),  hash, "Alice", "Buyer",    UserRole.Customer);
        var buyer2  = User.Create(new Email("buyer2@marketcore.dev"),  hash, "Bob",   "Customer", UserRole.Customer);
        var buyer3  = User.Create(new Email("buyer3@marketcore.dev"),  hash, "Carol", "Shopper",  UserRole.Customer);

        foreach (var u in new[] { admin, seller1, seller2, buyer1, buyer2, buyer3 })
            u.MarkEmailVerified();

        return [admin, seller1, seller2, buyer1, buyer2, buyer3];
    }

    private static List<Product> SeedProducts(
        Category electronics, Category clothing, Category books,
        Category digitalElectronics, User seller1, User seller2)
    {
        return
        [
            // ── Electronics (16) ───────────────────────────────────────────
            Product.Create("Wireless Noise-Cancelling Headphones",
                "Premium over-ear headphones with 30-hour battery life and active noise cancellation.",
                new Money(149.99m, "USD"), 45, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1505740420509-e7ef5fbe9a74?auto=format&fit=crop&w=400&q=80"),

            Product.Create("USB-C Fast Charger 65W",
                "GaN technology fast charger compatible with laptops, tablets, and smartphones.",
                new Money(34.99m, "USD"), 200, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1603371466160-7c04c74e7b55?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Mechanical Keyboard TKL",
                "Tenkeyless mechanical keyboard with Cherry MX Blue switches and RGB backlighting.",
                new Money(89.99m, "USD"), 30, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1587829741301-dc798b83add3?auto=format&fit=crop&w=400&q=80"),

            Product.Create("4K Webcam",
                "Ultra HD webcam with built-in ring light and noise-cancelling microphone.",
                new Money(79.99m, "USD"), 60, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Portable Bluetooth Speaker",
                "360-degree surround sound with 24-hour battery life and IPX7 waterproof rating.",
                new Money(59.99m, "USD"), 120, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Wireless Ergonomic Mouse",
                "Silent-click ergonomic design with adjustable 800–3200 DPI and 18-month battery life.",
                new Money(44.99m, "USD"), 90, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?auto=format&fit=crop&w=400&q=80"),

            Product.Create("27-inch 4K IPS Monitor",
                "Factory-calibrated 4K display with 99% sRGB coverage and USB-C 65W power delivery.",
                new Money(349.99m, "USD"), 25, electronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1555617981-dac3880eac6e?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Smart LED Desk Lamp",
                "Touch-controlled desk lamp with 5 colour temperatures, USB charging port and auto-dimming.",
                new Money(29.99m, "USD"), 75, electronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Noise-Isolating Earbuds",
                "In-ear earbuds with passive noise isolation, 10mm dynamic drivers and braided cable.",
                new Money(19.99m, "USD"), 200, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1590658268037-6bf12165a8df?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Portable SSD 1TB",
                "USB 3.2 Gen 2 external SSD with 1050 MB/s read speed in an aluminium enclosure.",
                new Money(99.99m, "USD"), 55, electronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1597848212624-a19eb35e2651?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Smart Watch Series X",
                "AMOLED display smartwatch with GPS, heart-rate monitor, 7-day battery and swim-proof design.",
                new Money(199.99m, "USD"), 40, electronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=400&q=80"),

            Product.Create("True Wireless Earbuds Pro",
                "Active noise-cancelling TWS earbuds with 30-hour total playback and IPX4 water resistance.",
                new Money(129.99m, "USD"), 70, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1606220945770-b5b6c2c55bf1?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Action Camera 4K 60fps",
                "Waterproof action camera with 4K 60fps video, image stabilisation and wide-angle lens.",
                new Money(249.99m, "USD"), 35, electronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1502920514313-52581002a659?auto=format&fit=crop&w=400&q=80"),

            Product.Create("10-Port USB Hub",
                "Powered USB 3.0 hub with 7 data ports and 3 fast-charging ports in an aluminium housing.",
                new Money(39.99m, "USD"), 150, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Wireless Charging Pad 15W",
                "Qi-certified 15W wireless charging pad with LED indicator, compatible with all Qi devices.",
                new Money(24.99m, "USD"), 180, electronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1601784551446-20c9e07cdbdb?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Adjustable Laptop Stand",
                "Aluminium vented laptop stand with 6 adjustable height settings and foldable design.",
                new Money(49.99m, "USD"), 110, electronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1611532736597-de2d4265fba3?auto=format&fit=crop&w=400&q=80"),

            // ── Clothing (14) ──────────────────────────────────────────────
            Product.Create("Premium Cotton T-Shirt",
                "100% organic cotton crew-neck t-shirt, available in 12 colours.",
                new Money(24.99m, "USD"), 150, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Slim Fit Chino Pants",
                "Stretch-fabric chinos with a tailored slim fit. Machine washable.",
                new Money(49.99m, "USD"), 80, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1542272604-787c3835535d?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Waterproof Running Jacket",
                "Lightweight windbreaker with taped seams and reflective detailing.",
                new Money(74.99m, "USD"), 40, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1551698618-1dfe5d97d256?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Merino Wool Crew Sweater",
                "Fine-gauge merino wool pullover, naturally temperature-regulating and machine washable.",
                new Money(89.99m, "USD"), 60, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1576566588028-4147f3842f27?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Canvas Sneakers Low-Top",
                "Classic low-top canvas shoes with a vulcanised rubber sole and cushioned insole.",
                new Money(39.99m, "USD"), 130, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1525966222134-fcfa99b8ae77?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Leather Bifold Wallet",
                "Full-grain vegetable-tanned leather wallet with 6 card slots and RFID blocking lining.",
                new Money(34.99m, "USD"), 110, clothing.Id, seller1.Id,
                "https://images.unsplash.com/photo-1627123424574-724758594e93?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Packable Down Vest",
                "800-fill-power down vest that packs into its own pocket. Wind-resistant shell fabric.",
                new Money(69.99m, "USD"), 45, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1539533018447-63fcce2678e3?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Structured Baseball Cap",
                "6-panel cotton twill cap with adjustable buckle closure and UV protection.",
                new Money(22.99m, "USD"), 180, clothing.Id, seller1.Id,
                "https://images.unsplash.com/photo-1588850561407-ed78c282e89b?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Slim Leather Belt",
                "Genuine cowhide leather belt with a brushed-nickel buckle. Available in 32–42 inches.",
                new Money(27.99m, "USD"), 95, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1624222247342-4bef59bf3f0c?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Zip-Up Fleece Hoodie",
                "Mid-weight polar-fleece hoodie with kangaroo pocket and YKK zip. Pill-resistant finish.",
                new Money(54.99m, "USD"), 85, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1556821840-3a63f15732ce?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Oxford Button-Down Shirt",
                "Classic Oxford weave shirt with a button-down collar and a relaxed fit. Easy-iron fabric.",
                new Money(44.99m, "USD"), 100, clothing.Id, seller1.Id,
                "https://images.unsplash.com/photo-1596755094514-f87e34085b2c?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Performance Running Shorts",
                "4-inch inseam running shorts with liner, reflective details and back-zip pocket.",
                new Money(32.99m, "USD"), 120, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1591195853828-11db59a44f43?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Denim Trucker Jacket",
                "Classic rigid denim jacket with chest pockets and adjustable side tabs. Unisex fit.",
                new Money(79.99m, "USD"), 55, clothing.Id, seller1.Id,
                "https://images.unsplash.com/photo-1576871337622-98d48d1cf531?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Ribbed Beanie Hat",
                "Soft acrylic rib-knit beanie with a fold-over cuff. One size fits most.",
                new Money(15.99m, "USD"), 200, clothing.Id, seller2.Id,
                "https://images.unsplash.com/photo-1576871337622-98d48d1cf531?auto=format&fit=crop&w=400&q=80"),

            // ── Books (15) ─────────────────────────────────────────────────
            Product.Create("Clean Code: A Handbook of Agile Software",
                "Robert C. Martin's definitive guide to writing clean, maintainable code.",
                new Money(39.99m, "USD"), 100, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1532012197267-da84d127e765?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Domain-Driven Design",
                "Eric Evans — Tackling Complexity in the Heart of Software.",
                new Money(54.99m, "USD"), 75, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?auto=format&fit=crop&w=400&q=80"),

            Product.Create("The Pragmatic Programmer",
                "David Thomas & Andrew Hunt — 20th Anniversary Edition covering modern software craftsmanship.",
                new Money(44.99m, "USD"), 85, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1550399105-c4db5fb85c18?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Designing Data-Intensive Applications",
                "Martin Kleppmann's guide to reliable, scalable, and maintainable distributed systems.",
                new Money(59.99m, "USD"), 70, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1519682337058-a94d519337bc?auto=format&fit=crop&w=400&q=80"),

            Product.Create("You Don't Know JS Yet (Vol. 1)",
                "Kyle Simpson's deep-dive into JavaScript's scope, closures, and the this keyword.",
                new Money(29.99m, "USD"), 120, books.Id, seller2.Id,
                "https://images.unsplash.com/photo-1516116216624-53e697fedbea?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Refactoring: Improving the Design of Existing Code",
                "Martin Fowler — the definitive catalogue of refactoring techniques with worked examples.",
                new Money(49.99m, "USD"), 65, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Introduction to Algorithms (CLRS)",
                "Cormen, Leiserson, Rivest & Stein — the comprehensive reference for algorithms and data structures.",
                new Money(79.99m, "USD"), 50, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1495446815901-a7297e633e8d?auto=format&fit=crop&w=400&q=80"),

            Product.Create("System Design Interview Vol. 2",
                "Alex Xu & Sahn Lam — insider's guide to answering system design questions at top tech companies.",
                new Money(34.99m, "USD"), 90, books.Id, seller2.Id,
                "https://images.unsplash.com/photo-1507842217343-583bb7270b66?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Atomic Habits",
                "James Clear — an easy and proven way to build good habits and break bad ones.",
                new Money(19.99m, "USD"), 200, books.Id, seller2.Id,
                "https://images.unsplash.com/photo-1512820790803-83ca734da794?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Deep Work",
                "Cal Newport — rules for focused success in a distracted world.",
                new Money(17.99m, "USD"), 150, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1481627834876-b7833e8f5570?auto=format&fit=crop&w=400&q=80"),

            Product.Create("The Psychology of Money",
                "Morgan Housel — timeless lessons on wealth, greed, and happiness.",
                new Money(16.99m, "USD"), 175, books.Id, seller2.Id,
                "https://images.unsplash.com/photo-1554224155-6726b3ff858f?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Clean Architecture",
                "Robert C. Martin — a craftsman's guide to software structure and design.",
                new Money(44.99m, "USD"), 80, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1524578271613-d550eacf6090?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Cracking the Coding Interview",
                "Gayle Laakmann McDowell — 189 programming questions and solutions for tech interviews.",
                new Money(37.99m, "USD"), 110, books.Id, seller2.Id,
                "https://images.unsplash.com/photo-1550399105-c4db5fb85c18?auto=format&fit=crop&w=400&q=80"),

            Product.Create("The Art of UNIX Programming",
                "Eric S. Raymond — design philosophy and principles behind the UNIX operating system.",
                new Money(42.99m, "USD"), 45, books.Id, seller1.Id,
                "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Staff Engineer: Leadership Beyond the Management Track",
                "Will Larson — paths to the staff-plus engineering role and the work involved.",
                new Money(28.99m, "USD"), 60, books.Id, seller2.Id,
                "https://images.unsplash.com/photo-1532012197267-da84d127e765?auto=format&fit=crop&w=400&q=80"),

            // ── Digital Electronics (15) ───────────────────────────────────
            Product.Create("Software Architecture Patterns (eBook)",
                "Digital download — five essential architecture patterns for modern distributed systems.",
                new Money(19.99m, "USD"), 999, digitalElectronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1484417894907-623942c8ee29?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Angular 21 Complete Course (eBook)",
                "Digital download — comprehensive guide covering standalone components, signals, and SSR.",
                new Money(24.99m, "USD"), 999, digitalElectronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1461749280684-dccba630e2f6?auto=format&fit=crop&w=400&q=80"),

            Product.Create(".NET 8 Microservices Handbook (eBook)",
                "Digital download — practical guide to building production-grade microservices with .NET 8.",
                new Money(29.99m, "USD"), 999, digitalElectronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1498050108023-c5249f4df085?auto=format&fit=crop&w=400&q=80"),

            Product.Create("SQL Performance Explained (eBook)",
                "Digital download — Markus Winand's essential guide to indexing and query optimisation.",
                new Money(22.99m, "USD"), 999, digitalElectronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1544383835-bda2bc66a55d?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Clean Architecture in Practice (Video Course)",
                "Digital download — 8-hour video series implementing Clean Architecture with .NET and Angular.",
                new Money(34.99m, "USD"), 999, digitalElectronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1487017159836-4e23ece2e4cf?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Linux Command Line Mastery (eBook)",
                "Digital download — comprehensive coverage of Bash scripting, tools, and system administration.",
                new Money(17.99m, "USD"), 999, digitalElectronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1518432031352-d6fc5c10da5a?auto=format&fit=crop&w=400&q=80"),

            Product.Create("TypeScript Deep Dive (eBook)",
                "Digital download — Basarat Ali Syed's free-to-web, paid-print guide to TypeScript internals.",
                new Money(14.99m, "USD"), 999, digitalElectronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1555066931-4365d14bab8c?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Docker & Kubernetes Masterclass (Video Course)",
                "Digital download — 12-hour course covering containerisation, orchestration, and Helm charts.",
                new Money(39.99m, "USD"), 999, digitalElectronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1542831371-29b0f74f9713?auto=format&fit=crop&w=400&q=80"),

            Product.Create("AWS Solutions Architect Study Notes (eBook)",
                "Digital download — concise notes and practice questions for the SAA-C03 exam.",
                new Money(27.99m, "USD"), 999, digitalElectronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1451187580459-43490279c0fa?auto=format&fit=crop&w=400&q=80"),

            Product.Create("React 19 & Next.js Complete Course (Video)",
                "Digital download — hands-on course building production apps with React 19 Server Components.",
                new Money(44.99m, "USD"), 999, digitalElectronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1515879218367-8466d910aaa4?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Python for Data Science Handbook (eBook)",
                "Digital download — Jake VanderPlas's reference for NumPy, Pandas, Matplotlib and Scikit-Learn.",
                new Money(24.99m, "USD"), 999, digitalElectronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?auto=format&fit=crop&w=400&q=80"),

            Product.Create("GraphQL API Design Guide (eBook)",
                "Digital download — patterns and best practices for designing scalable GraphQL APIs.",
                new Money(19.99m, "USD"), 999, digitalElectronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Redis in Action (eBook)",
                "Digital download — patterns for using Redis as cache, message broker, and primary datastore.",
                new Money(21.99m, "USD"), 999, digitalElectronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1558346490-a72e53ae2d4f?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Kubernetes Patterns (eBook)",
                "Digital download — reusable elements for designing cloud-native applications.",
                new Money(26.99m, "USD"), 999, digitalElectronics.Id, seller2.Id,
                "https://images.unsplash.com/photo-1461749280684-dccba630e2f6?auto=format&fit=crop&w=400&q=80"),

            Product.Create("Git Internals Pro (eBook)",
                "Digital download — understanding Git's object model, branching, and collaboration workflows.",
                new Money(12.99m, "USD"), 999, digitalElectronics.Id, seller1.Id,
                "https://images.unsplash.com/photo-1498050108023-c5249f4df085?auto=format&fit=crop&w=400&q=80"),
        ];
    }

    private static List<Cart> SeedCarts(User buyer1, User buyer2, User buyer3, List<Product> products)
    {
        var cart1 = Cart.Create(buyer1.Id);
        cart1.AddItem(products[0].Id, 1, products[0].Price);
        cart1.AddItem(products[30].Id, 2, products[30].Price);

        var cart2 = Cart.Create(buyer2.Id);

        var cart3 = Cart.Create(buyer3.Id);
        cart3.AddItem(products[16].Id, 3, products[16].Price);

        return [cart1, cart2, cart3];
    }

    private static List<Order> SeedOrders(User buyer1, User buyer2, List<Product> products)
    {
        var address1 = new Address("100 Tech Boulevard", "San Francisco", "CA", "94102", "US");
        var address2 = new Address("22 Commerce Street", "New York", "NY", "10001", "US");

        var order1 = Order.Create(buyer1.Id, address1);
        order1.AddItem(products[2].Id, products[2].Name, 1, products[2].Price);
        order1.AddItem(products[31].Id, products[31].Name, 1, products[31].Price);
        order1.Confirm();

        var order2 = Order.Create(buyer2.Id, address2);
        order2.AddItem(products[16].Id, products[16].Name, 2, products[16].Price);
        order2.AddItem(products[17].Id, products[17].Name, 1, products[17].Price);
        order2.Confirm();
        order2.Ship();

        return [order1, order2];
    }
}
