namespace Catalog.Application.Exceptions;

public class DuplicateSkuException(string sku)
    : Exception($"Ya existe un producto con el SKU '{sku}'.");
