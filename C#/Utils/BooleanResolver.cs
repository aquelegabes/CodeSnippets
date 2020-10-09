using System;
using System.Linq;

/// <summary>
/// Resolves a boolean operation using parameterized arguments.
/// </summary>
public static class BooleanResolver
{
    public enum Operator : int
    {
        AND = 1,
        OR = 2
    }
    public enum Conditional : int
    {
        LessThan = 1,
        GreaterThan = 2,
        EqualTo = 3,
        DifferentFrom = 4,
        Between = 5,
        Anything = 6,
    }
    /// <summary>
    /// Resolve a condition between two objects.
    /// </summary>
    /// <param name="type">Type of the objects.</param>
    /// <param name="conditional">Condition to be resolved.</param>
    /// <param name="obj">Object to compare.</param>
    /// <param name="source">Source object to be compared.</param>
    /// <exception cref="ArgumentException">Invalid <paramref name="conditional"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="TypeLoadException">Type does not implements <see cref="IComparable"/>.</exception>
    /// <returns>The result of the condition.</returns>
    public static bool ResolveConditional(
        Type type, Conditional conditional, object obj, object source)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type), "Type must not be null.");

        if (conditional == default)
            throw new ArgumentException("Invalid conditional.", nameof(conditional));

        if (obj is null)
            throw new ArgumentNullException(nameof(obj), "Object reference must not be null.");

        if (source is null)
            throw new ArgumentNullException(nameof(source), "Object source must not be null.");

        if (!type.GetInterfaces().Contains(typeof(IComparable)))
            throw new TypeLoadException($"Type : '{nameof(type)}' does not implements {nameof(IComparable)}.");
        var comparableObj = (IComparable)obj;

        switch (conditional)
        {
            case Conditional.LessThan:
                return comparableObj.CompareTo(source) < 0;
            case Conditional.GreaterThan:
                return comparableObj.CompareTo(source) > 0;
            case Conditional.EqualTo:
                return comparableObj.CompareTo(source) == 0;
            case Conditional.DifferentFrom:
                return comparableObj.CompareTo(source) != 0;
            case Conditional.Between:
            case Conditional.Anything:
            default:
                return false;
        }
    }

    /// <summary>
    /// Resolve a condition between two objects.
    /// </summary>
    /// <param name="conditional">Condition to be resolved.</param>
    /// <param name="obj">Object to compare.</param>
    /// <param name="source">Source object to be compared.</param>
    /// <typeparam name="T">Type of the objects.</typeparam>
    /// <exception cref="ArgumentException">Invalid <paramref name="conditional"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <returns>The result of the condition.</returns>
    public static bool ResolveConditional<T>(
        Conditional conditional, T obj, T source)
        where T : IComparable
    {
        return ResolveConditional(typeof(T), conditional, obj, source);
    }

    /// <summary>
    /// Resolve a boolean operation using an <see cref="Operator"/>.
    /// </summary>
    /// <param name="operator">Operator to be resolved.</param>
    /// <param name="conditions">Conditions to resolve.</param>
    /// <returns>The operation result.</returns>
    /// <exception cref="ArgumentException">Invalid <paramref name="operator"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="conditions"/> is null.</exception>
    public static bool ResolveOperation(
        Operator @operator, params bool[] conditions)
    {
        if (@operator == default)
            throw new ArgumentException("Invalid operator.", nameof(@operator));
        if (conditions?.Any() == false)
            throw new ArgumentNullException(paramName: nameof(conditions), message: "At least one condition must be specified.");

        bool? result = null;
        foreach (var condition in conditions)
        {
            if (result == null)
                result = condition;

            if (Operator.AND == @operator)
                result = result.Value && condition;
            else
                result = result.Value || condition;
        }
        return result.Value;
    }
}
