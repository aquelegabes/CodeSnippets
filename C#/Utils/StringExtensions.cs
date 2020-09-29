/// <summary>
/// Splits a string into a maximum number of substrings based on the characters in the bool expression. 
/// </summary>
/// <param name="value">String value</param>
/// <param name="expression">Expression to when split string</param>
/// <param name="count">The maximum number of substrings to return. Default value is <see cref="Int32.MaxValue"/></param>
/// <param name="options"><see cref="StringSplitOptions.RemoveEmptyEntries"/> to omit empty array elements from the array returned; or None to include empty array elements in the array returned. Default value is <see cref="StringSplitOptions.None"/></param>
/// <returns cref="String[]">An array whose elements contain the substrings in this string that are delimited by one or more characters in separator.</returns>
/// <exception cref="ArgumentNullException">Any of the parameters are <see cref="null"/>.</exception>
/// <exception cref="ArgumentOutOfRangeException">Count is negative.</exception>
/// <exception cref="ArgumentException">options is not one of the <see cref="StringSplitOptions"/> values.</exception>
public static string[] Split(this string value, Func<char, bool> expression, Int32 count = Int32.MaxValue, StringSplitOptions options = StringSplitOptions.None)
{
    if (string.IsNullOrWhiteSpace(value))
        throw new ArgumentNullException("Base string value cannot be null or white spaced.");

    if (expression is null)
        throw new ArgumentNullException("Expression cannot be null");

    var split = value.Where(expression).ToArray();

    try 
    {
        var result = value.Split(split, count, options);
        return result;
    }
    catch (ArgumentOutOfRangeException)
    {
        throw;
    }
    catch (ArgumentException)
    {
        throw;
    }
}

/// <summary>
/// Returns a byte[] string encoded with a chosen charset
/// </summary>
/// <param name="str"></param>
/// <param name="encoding" cref="Encoding">Check System.Text.Encoding properties members</param>
/// <example>
/// <c>foo.ToByteArray(Encoding.UTF8);</c>
/// </example>
/// <exception cref="ArgumentNullException"></exception>
/// <exception cref="EncoderFallbackException"></exception>
public static byte[] ToByteArray(this string str, Encoding encoding)
{
    if (string.IsNullOrWhiteSpace(str))
        throw new ArgumentNullException(nameof(str), "In order to encode value can not be null");
    if (encoding == null)
        throw new ArgumentNullException(nameof(encoding), "Encoding can not be null");
    try
    {
        return encoding.GetBytes(str);
    }
    catch (EncoderFallbackException ex)
    {
        throw new Exception("Wasn't possible to encode specified value, see inner exception for details", ex);
    }
    catch (Exception ex)
    {
        throw;
    }
}

/// <summary>
/// Strips HTML tags from an input string
/// Warning: This does not work for all cases and should not be used to process untrusted user input.
/// </summary>
/// <param name="value">string to remove the HTML tags</param>
/// <returns></returns>
public static string StripHTML(this string value)
{
    return Regex.Replace(value, "<.*?>", string.Empty);
}


/// <summary>
/// Return a normalized string
/// </summary>
/// <param name="value"></param>
/// <returns></returns>
public static string RemoveAccents(this string value)
{
    return new string(value.Normalize(NormalizationForm.FormD).Where(ch => char.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray());
}

/// <summary>
/// Return the string reversed
/// <param name="value"></param>
/// <returns></returns>
public static string Reverse(this string val)
{
    return new string(val.Reverse().ToArray());
}
