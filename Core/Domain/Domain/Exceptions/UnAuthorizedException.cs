using System;

namespace Domain.Exceptions;

public class UnAuthorizedException(string message) : Exception(message);
