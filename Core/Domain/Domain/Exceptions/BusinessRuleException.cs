using System;

namespace Domain.Exceptions;

public class BusinessRuleException(string message) : Exception(message);
