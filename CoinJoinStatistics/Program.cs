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
            int firstBlock = 0;
            if (File.Exists(coinjoinsFilePath))
            {
                string lastLine = File.ReadAllLines(coinjoinsFilePath).LastOrDefault();
                if (lastLine != null)
                {
                    firstBlock = int.Parse(lastLine.Split(':')[1]) + 1;
                }
            }

            var rpc = new RPCClient(
                        credentials: new RPCCredentialString
                        {
                            UserPassword = new NetworkCredential(args[0], args[1])
                        },
                        network: Network.Main);

            int lastBlock = rpc.GetBlockCount();

            for (int i = firstBlock; i < lastBlock; i++)
            {
                var block = rpc.GetBlock(i);

                var builder = new StringBuilder($"");
                foreach (var tx in block.Transactions)
                {
                    IEnumerable<(Money value, int count)> coinjoin = tx.GetIndistinguishableOutputs().Where(x => x.count > 1);
                    if (coinjoin.Any())
                    {
                        string dateString = block.Header.BlockTime.ToString("yyyy-MM-dd");

                        foreach (var (value, count) in coinjoin)
                        {
                            builder.Append($"{dateString}:{i}:{tx.GetHash()}:{count}:{value}\n");
                        }
                    }
                }
                if (builder.Length != 0)
                {
                    var content = builder.ToString();
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
