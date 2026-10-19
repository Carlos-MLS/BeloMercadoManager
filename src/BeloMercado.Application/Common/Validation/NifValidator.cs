namespace BeloMercado.Application.Common.Validation;

public static class NifValidator
{
    /// <summary>
    /// Validates a Portuguese NIF: 9 digits, last digit matches the
    /// mod-11 check digit computed from the first 8.
    /// </summary>
    public static bool IsValid(string nif)
    {
        if (string.IsNullOrWhiteSpace(nif) || nif.Length != 9)
        {
            return false;
        }

        for (var i = 0; i < 9; i++)
        {
            if (!char.IsDigit(nif[i]))
            {
                return false;
            }
        }

        var sum = 0;
        for (var i = 0; i < 8; i++)
        {
            sum += (nif[i] - '0') * (9 - i);
        }
        var remainder = sum % 11;
        var expected = remainder is 0 or 1 ? 0 : 11 - remainder;
        return expected == nif[8] - '0';
    }
}
