public static bool ValidaCPF(string cpf)
{
    if (string.IsNullOrEmpty(cpf.Trim()))
        return true;
        
    // Remove formatação do número, ex: "123.456.789-01" vira: "12345678901"
    // Procurar pelo formataTexto.cs
    cpf = RemoveNaoNumericos(cpf);

    if (cpf.Length > 11)
        return false;

    while (cpf.Length != 11)
        cpf = '0' + cpf;

    bool igual = true;
    for (int i = 1; i < 11 && igual; i++)
    {
        if (cpf[i] != cpf[0])
            igual = false;
    }
    
    if (igual || cpf == "12345678909")
        return false;

    int[] numeros = new int[11];

    for (int i = 0; i < 11; i++)
        numeros[i] = int.Parse(cpf[i].ToString());

    int soma = 0;
    for (int i = 0; i < 9; i++)
        soma += (10 - i) * numeros[i];

    int resultado = soma % 11;

    if (resultado == 1 || resultado == 0)
    {
        if (numeros[9] != 0)
            return false;
    }
    else if (numeros[9] != 11 - resultado)
        return false;

    soma = 0;
    for (int i = 0; i < 10; i++)
        soma += (11 - i) * numeros[i];

    resultado = soma % 11;

    if (resultado == 1 || resultado == 0)
    {
        if (numeros[10] != 0)
            return false;
    }
    else 
    {
        if (numeros[10] != 11 - resultado)
        return false;
    }
    
    return true;
}

public static bool ValidaCNPJ(string vrCNPJ)
{
    int[] digitos, soma, resultado;
    int nrDig;
    string ftmt;
    bool[] CNPJOk;
    string CNPJ = vrCNPJ.Replace(".", "");

    CNPJ = CNPJ.Replace("/", "");
    CNPJ = CNPJ.Replace("-", "");

    ftmt = "6543298765432";
    digitos = new int[14];
    soma = new int[2];
    soma[0] = 0;
    soma[1] = 0;
    resultado = new int[2];
    resultado[0] = 0;
    resultado[1] = 0;
    CNPJOk = new bool[2];
    CNPJOk[0] = false;
    CNPJOk[1] = false;

    try
    {
        for (nrDig = 0; nrDig < 14; nrDig++)
        {
            digitos[nrDig] = int.Parse(
                             CNPJ.Substring(nrDig, 1));

            if (nrDig <= 11)
            {
                soma[0] += (digitos[nrDig] *
                            int.Parse(ftmt.Substring(
                            nrDig + 1, 1)));
            }
            
            if (nrDig <= 12)
            {
                soma[1] += (digitos[nrDig] *
                            int.Parse(ftmt.Substring(
                            nrDig, 1)));
            }
        }

        for (nrDig = 0; nrDig < 2; nrDig++)
        {
            resultado[nrDig] = (soma[nrDig] % 11);

            if ((resultado[nrDig] == 0) || (
                 resultado[nrDig] == 1)) 
            {
                CNPJOk[nrDig] = (digitos[12 + nrDig] == 0);
            }
            
            else
                CNPJOk[nrDig] = (digitos[12 + nrDig] == (11 - resultado[nrDig]));
        }

        return (CNPJOk[0] && CNPJOk[1]);
    } 
    catch (Exception ex)
    {
        return false;
    }
}
