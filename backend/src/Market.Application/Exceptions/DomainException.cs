namespace Market.Application.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with id '{id}' was not found.") { }
}

public class ValidationException : DomainException
{
    public ValidationException(string message) : base(message) { }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

public class InsufficientCreditsException : DomainException
{
    public InsufficientCreditsException(Guid traderId, decimal required, decimal available)
        : base($"Trader '{traderId}' has insufficient credits. Required: {required}, Available: {available}.") { }
}

public class InsufficientInventoryException : DomainException
{
    public InsufficientInventoryException(Guid traderId, Guid assetTypeId, decimal required, decimal available)
        : base($"Trader '{traderId}' has insufficient available quantity of asset '{assetTypeId}'. Required: {required}, Available: {available}.") { }
}

public class OrderNotFillableException : DomainException
{
    public OrderNotFillableException(Guid assetTypeId, decimal requestedQuantity, decimal availableQuantity)
        : base($"Cannot fill {requestedQuantity} units of asset '{assetTypeId}'. Only {availableQuantity} units available across open sell orders.") { }
}
