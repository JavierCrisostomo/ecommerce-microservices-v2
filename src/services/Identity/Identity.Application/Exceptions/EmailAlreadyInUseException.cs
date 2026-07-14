namespace Identity.Application.Exceptions;

public class EmailAlreadyInUseException(string email)
    : Exception($"El email '{email}' ya está registrado.");
