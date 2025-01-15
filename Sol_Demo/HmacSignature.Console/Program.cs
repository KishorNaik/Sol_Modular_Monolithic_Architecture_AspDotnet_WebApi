// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

Console.WriteLine("Hello, World!");

var userObj = new UserDto();
userObj.FirstName = "Kishor";
userObj.LastName = "Naik";

// Serialize the user object to JSON
string jsonBody = JsonConvert.SerializeObject(userObj);

// Create the payload
byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonBody);


// Secret Id
string secret = "secret";


var obj = new GeneratedSignature();
var result = obj.Handle(payloadBytes, secret);
Console.WriteLine(result);


// Demo Dto
public class UserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}


public class GeneratedSignature
{
    public string Handle(byte[] payload, string secret)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            byte[] hash = hmac.ComputeHash(payload);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}