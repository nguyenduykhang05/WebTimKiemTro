using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add(""User-Agent"", ""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"");
string url = ""https://gateway.chotot.com/v1/public/ad-listing?cg=1020&limit=1"";
var response = await httpClient.GetAsync(url);
if (!response.IsSuccessStatusCode) {
    Console.WriteLine(""Failed HTTP"");
    return;
}
string jsonStr = await response.Content.ReadAsStringAsync();
using var document = JsonDocument.Parse(jsonStr);
var root = document.RootElement;
if (root.TryGetProperty(""ads"", out var ads)) {
    foreach (var ad in ads.EnumerateArray()) {
        try {
            string subject = ad.GetProperty(""subject"").GetString();
            Console.WriteLine($""Title: {subject}"");
            long listId = ad.GetProperty(""list_id"").GetInt64();
            Console.WriteLine($""ListID: {listId}"");
            if (ad.TryGetProperty(""price"", out var priceElement))
                Console.WriteLine($""Price: {priceElement.GetDouble()}"");
            
            if (ad.TryGetProperty(""images"", out var imagesElement)) {
                Console.WriteLine($""Images type: {imagesElement.ValueKind}"");
            }
        } catch(Exception e) {
            Console.WriteLine($""Error: {e.Message}"");
        }
    }
}
