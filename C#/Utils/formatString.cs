// Formata uma string com uma máscara desejada
public static string FormatStringMask(string mask, string value)
{
    if (string.IsNullOrEmpty(value))
        return "";

    var newValue = string.Empty;
    var position = 0;
    var maskCount = mask.Count(char.IsDigit);
    var valueExtraCharCount = value.Count(c => !char.IsDigit(c));
    value = value.PadLeft(maskCount + valueExtraCharCount, '0');

    foreach (var t in mask)
    {
        if (position > value.Length)
            break;

        if (!char.IsDigit(t))
            newValue = newValue + t;
        else
        {
            var vp = value[position];
            if (!char.IsDigit(vp))
            {
                position++;
                vp = value[position];
            }

            newValue = newValue + vp;
            position++;
        }
    }
    return newValue;
}
