
using System.Text;

namespace Utility.Shared.RandomString;

public static class RandomComplexSring
{
    public static string GenerateComplexRandomString()
    {
        // Start with a base of concatenated GUIDs
        StringBuilder sb = new StringBuilder();
        sb.Append($"{Guid.NewGuid()}-{Guid.NewGuid()}-{Guid.NewGuid()}-{Guid.NewGuid()}-{Guid.NewGuid()}");

        // Add additional random alphanumeric characters
        Random random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        for (int i = 0; i < 10; i++) // Add 10 random characters
        {
            sb.Append(chars[random.Next(chars.Length)]);
        }

        return sb.ToString();
    }
}
