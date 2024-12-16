// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using Utility.Shared.AES;

Console.WriteLine("Hello, World!");

const string secretKey = "042569c1-9dc8-4731-9363-18c1f268a1cf-225dbb41-ccaf-40bf-9db1-439e0c12f9e2-602b429b-7f66-44d3-85c6-0cb15beebe65-29318e16-416c-4636-818a-f7e69a38a8fa";

AesHelper aesHelper = new AesHelper(secretKey: secretKey);

var request = new
{
    FirstName = "John",
    LastName = "Doe",
    Email = "jhon@example.com",
    Password="123456789",
    Mobile = "1234567890",
    OrgId= "9922f65e-a7a5-4b9f-b064-b147a70454d9"

};

var json = JsonConvert.SerializeObject(request);

var encrypted = await aesHelper.EncryptAsync(json);
Console.WriteLine($"Encrypted:{encrypted}");

var decrypted = await aesHelper.DecryptAsync(encrypted);
Console.WriteLine($"Decrypted:{decrypted}");