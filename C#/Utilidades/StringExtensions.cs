/// <summary>
/// Returns a byte[] string encoded with a chosen charset
/// </summary>
/// <param name="str"></param>
/// <param name="encoding" cref="Encoding">Check System.Text.Encoding properties members</param>
/// <example>
/// <c>foo.ToByteArray(Encoding.UTF8);</c>
/// </example>
public static byte[] ToByteArray(this string str, Encoding encoding)
{
    try
    {
        return encoding.GetBytes(str);
    }catch (Exception ex)
    {
        return default(byte[]);
        //throw;
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
