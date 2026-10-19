namespace BeloMercado.Application.Common.Validation;

public static class Ean13Validator
{
    /// <summary>
    /// Validates an EAN-13 barcode: 13 digits, last digit matches the check
    /// digit computed from the first 12 (1x odd positions + 3x even
    /// positions, mod 10 complement).
    /// </summary>
    public static bool IsValid(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 13)
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            if (!char.IsDigit(code[i]))
            {
                return false;
            }
            var d = code[i] - '0';
            sum += i % 2 == 0 ? d : d * 3;
        }

        if (!char.IsDigit(code[12]))
        {
            return false;
        }
        var check = (10 - (sum % 10)) % 10;
        return check == code[12] - '0';
    }
}
