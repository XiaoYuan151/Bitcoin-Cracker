using NBitcoin;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Bitcoin_Cracker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string cracking = String.Empty;
                // string cracking = "19QciEHbGVNY4hrhfKXmcBBCrJSBZ6TaVt";
                int threads = 0;
                // int threads = 16;
                foreach (string arg in args)
                {
                    if (arg.StartsWith("/address=") || arg.StartsWith("-address="))
                    {
                        cracking = arg.Substring("/address=".Length);
                    }
                    if (arg.StartsWith("/threads=") || arg.StartsWith("-threads="))
                    {
                        threads = int.Parse(arg.Substring("/threads=".Length));
                    }
                }
                if (cracking.StartsWith("1") && threads > 0)
                {
                    Console.WriteLine("Starting the Bitcoin address cracker...");
                    Task<decimal> @decimal = Task.Run(() => GetBalanceBlockstreamAsync(cracking));
                    @decimal.Wait();
                    decimal balance = @decimal.Result;
                    Console.WriteLine($"Total balance: {balance} BTC");
                    for (int i = 0; i < threads; i++)
                    {
                        Thread thread = new Thread(() => CrackAddress(cracking, i));
                        thread.Start();
                    }
                    while (true) { }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        static void CrackAddress(string cracking, int threading)
        {
            Network network = Network.Main;
            while (true)
            {
                Key key = new Key();
                string privHex = key.ToHex();
                PubKey pubKey = key.PubKey;
                string pubHex = pubKey.ToHex();
                BitcoinAddress address = pubKey.GetAddress(ScriptPubKeyType.Legacy, network);
                Console.WriteLine($"[{threading}] Private Key: {privHex}");
                Console.WriteLine($"[{threading}] P2PKH Address: {address.ToString()}");
                if (address.ToString() == cracking)
                {
                    File.WriteAllText("FoundKey.txt", privHex);
                    Console.WriteLine($"[{threading}] Match found! Saved into \"FoundKey.txt\".");
                    Console.ReadLine();
                    break;
                }
            }
        }
        static async Task<decimal> GetBalanceBlockstreamAsync(string address)
        {
            HttpClient httpClient = new HttpClient();
            string url = $"https://blockstream.info/api/address/{address}";
            HttpResponseMessage responseMessage = await httpClient.GetAsync(url);
            responseMessage.EnsureSuccessStatusCode();
            Stream stream = await responseMessage.Content.ReadAsStreamAsync();
            JsonDocument document = await JsonDocument.ParseAsync(stream);
            JsonElement element = document.RootElement;
            JsonElement chainStats = element.GetProperty("chain_stats");
            long funded = chainStats.GetProperty("funded_txo_sum").GetInt64();
            long spent = chainStats.GetProperty("spent_txo_sum").GetInt64();
            long satoshis = funded - spent;
            return satoshis / 100_000_000m;
        }
    }
}
