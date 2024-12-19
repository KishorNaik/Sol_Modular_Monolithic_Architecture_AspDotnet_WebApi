// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using Utility.Shared.AES;

Console.WriteLine("Hello, World!");

const string defaultSecretKey = "042569c1-9dc8-4731-9363-18c1f268a1cf-225dbb41-ccaf-40bf-9db1-439e0c12f9e2-602b429b-7f66-44d3-85c6-0cb15beebe65-29318e16-416c-4636-818a-f7e69a38a8fa";

AesHelper aesHelper = new AesHelper(secretKey: defaultSecretKey);

//var request = new
//{
//    FirstName = "Jane",
//    LastName = "Doe",
//    Email = "jane@example.com",
//    Password = "123456789",
//    Mobile = "1234567891",
//    OrgId = "78e01393-fad6-43b9-8244-22eea6bb41ce"

//};

//var request = new
//{
//    Name = "Shree Krishna Ltd"
//};

var request = new
{
    EmailId = "jane@example.com",
    Password = "123456789"
};

var json = JsonConvert.SerializeObject(request);

var encrypted = await aesHelper.EncryptAsync(json);
Console.WriteLine($"Encrypted:{encrypted}");

var decrypted = await aesHelper.DecryptAsync(encrypted);
Console.WriteLine($"Decrypted:{decrypted}");

