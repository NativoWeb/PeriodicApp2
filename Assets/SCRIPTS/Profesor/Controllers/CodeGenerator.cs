using System;

public static class CodeGenerator
{
    private static Random random = new Random();

    public static string GenerateCode(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] codeArray = new char[length];

        for (int i = 0; i < length; i++)
        {
            codeArray[i] = chars[random.Next(chars.Length)];
        }

        return new string(codeArray);
    }
}
