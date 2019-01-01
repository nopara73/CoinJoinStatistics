using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace CoinJoinStatistics
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var coinjoinsFilePath = "CoinJoins.txt";

            var rpc = new RPCClient(
                        credentials: new RPCCredentialString
                        {
                            UserPassword = new NetworkCredential(args[0], args[1])
                        },
                        network: Network.Main);

            var lastBlock = rpc.GetBlockCount();
            for (int i = 0; i < lastBlock; i++)
            {
                var block = rpc.GetBlock(i);

                var builder = new StringBuilder($"");
                foreach (var tx in block.Transactions)
                {
                    IEnumerable<(Money value, int count)> coinjoin = tx.GetIndistinguishableOutputs().Where(x => x.count > 1);
                    if (coinjoin.Any())
                    {
                        var levelStrings = new List<string>();
                        Money txVolume = Money.Zero;
                        foreach (var (value, count) in coinjoin)
                        {
                            Money volume = count * value;
                            txVolume += volume;
                            levelStrings.Add($"{count} * {value.ToString(false, false)}BTC = {volume.ToString(false, false)}BTC\n");
                        }

                        builder.Append($"{tx.GetHash()} {txVolume.ToString(false, false)}BTC\n");
                        foreach (var lvlString in levelStrings)
                        {
                            builder.Append(lvlString);
                        }
                    }
                }
                if (builder.Length != 0)
                {
                    var content = $"{block.Header.BlockTime.ToString("yyyy-MM-dd")}{i} BLOCK\n{builder.ToString()}\n\n";
                    Console.Write(content);
                    File.AppendAllText(coinjoinsFilePath, content);
                }
            }
        }

        public static IEnumerable<(Money value, int count)> GetIndistinguishableOutputs(this Transaction me)
        {
            return me.Outputs.GroupBy(x => x.Value)
               .ToDictionary(x => x.Key, y => y.Count())
               .Select(x => (x.Key, x.Value));
        }
    }
}
