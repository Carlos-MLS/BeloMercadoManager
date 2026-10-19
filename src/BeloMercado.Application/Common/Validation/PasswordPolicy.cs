namespace BeloMercado.Application.Common.Validation;

public static class PasswordPolicy
{
    public const int MinLength = 8;

    /// <summary>
    /// UC17 password complexity: >=8 chars, includes uppercase, lowercase,
    /// digit and symbol (4 of 4 categories).
    /// </summary>
    public static bool IsValid(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            return false;
        }

        bool hasUpper = false, hasLower = false, hasDigit = false, hasSymbol = false;
        foreach (var c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else hasSymbol = true; // anything else counts as symbol
        }
        return hasUpper && hasLower && hasDigit && hasSymbol;
    }
}
