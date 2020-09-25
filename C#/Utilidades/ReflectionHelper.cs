using System;
using System.Collections.Generic;
using System.Reflection;

public static class ReflectionHelper
{
    /// <summary>
    /// Get all enums from a class that holds enums.
    /// </summary>
    /// <param name="classContainingEnums">Type of class that holds enums.</param>
    /// <returns>A dictionary collection of enum names and values.</returns>
    public static object GetEnumNamespaceValues(Type classContainingEnums)
    {
        var enumTypes = classContainingEnums.GetNestedTypes(BindingFlags.Static | BindingFlags.Public);
        var result = new List<object>();

        foreach (var enumType in enumTypes)
        {
            var enumValues = new Dictionary<string, object>();
            // enum fields
            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var field in fields)
            {
                enumValues.Add(field.Name, (int)field.GetValue(null));
            }

            var values = new { enumName = enumType.Name, enumValues };
            result.Add(values);
        }

        return result;
    }
}
