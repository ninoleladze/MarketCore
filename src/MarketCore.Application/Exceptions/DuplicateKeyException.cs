namespace MarketCore.Application.Exceptions;

public sealed class DuplicateKeyException : Exception
{

    public string EntityName { get; }

    public string FieldName { get; }

    public DuplicateKeyException(string entityName, string fieldName, string message)
        : base(message)
    {
        EntityName = entityName;
        FieldName = fieldName;
    }

    public DuplicateKeyException(string entityName, string fieldName, string message, Exception innerException)
        : base(message, innerException)
    {
        EntityName = entityName;
        FieldName = fieldName;
    }
}
