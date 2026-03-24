using System;

namespace InsightLogger.Application.Rules.Exceptions;

public sealed class RuleAlreadyExistsException : InvalidOperationException
{
    public RuleAlreadyExistsException(string name)
        : base($"A rule named '{name}' already exists.")
    {
        Name = name;
    }

    public string Name { get; }
}
