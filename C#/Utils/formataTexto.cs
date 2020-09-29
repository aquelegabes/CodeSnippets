// Remover não numéricos de um texto
public static string RemoveNaoNumericos(string text)
{
    System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"[^0-9]");
    string ret = reg.Replace(text, string.Empty);
    return ret;
}

// Remover acentos
private string RemoveAcentos(string texto)
{
		String textoNormalizado = texto.Normalize(System.Text.NormalizationForm.FormD);
		System.Text.StringBuilder textoSemAcento = new System.Text.StringBuilder();
		for (int i = 0; i< textoNormalizado.Length; i++)
		{
				Char c = textoNormalizado[i];

				if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
						textoSemAcento.Append(c);
		}
		return textoSemAcento.ToString();
}
