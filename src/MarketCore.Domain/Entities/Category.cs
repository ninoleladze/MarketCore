using MarketCore.Domain.Common;

namespace MarketCore.Domain.Entities;

public sealed class Category : BaseEntity
{

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Guid? ParentCategoryId { get; private set; }

    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private readonly List<Product> _products = new();

    private Category() { }

    private Category(string name, string description, Guid? parentCategoryId) : base()
    {
        Name = name;
        Description = description;
        ParentCategoryId = parentCategoryId;
    }

    public static Category Create(string name, string description = "", Guid? parentCategoryId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(name));

        return new Category(name.Trim(), description?.Trim() ?? string.Empty, parentCategoryId);
    }

    public void UpdateDetails(string name, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
    }
}
