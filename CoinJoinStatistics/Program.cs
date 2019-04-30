using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CoinJoinStatistics
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var coinjoinsFilePath = "CoinJoins.txt";
            if (args.Length == 0 && File.Exists(coinjoinsFilePath))
            {
                DoMonthlyStatAsync(coinjoinsFilePath).GetAwaiter().GetResult();
                return;
            }

            DoTxStatAsync(args, coinjoinsFilePath).GetAwaiter().GetResult();
        }

        private static async Task DoTxStatAsync(string[] args, string coinjoinsFilePath)
        {
            int firstBlock = 0;
            if (File.Exists(coinjoinsFilePath))
            {
                string lastLine = (await File.ReadAllLinesAsync(coinjoinsFilePath)).LastOrDefault();
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

            int lastBlock = await rpc.GetBlockCountAsync();

            for (int i = firstBlock; i < lastBlock; i++)
            {
                var block = await rpc.GetBlockAsync(i);

                var builder = new StringBuilder($"");
                foreach (var tx in block.Transactions)
                {
                    IEnumerable<(Money value, int count)> coinjoin = tx.GetIndistinguishableOutputs().Where(x => x.count > 1).OrderByDescending(x => x.count);
                    int transactionOutputCount = tx.Inputs.Count;
                    int coinjoinOutputCount = coinjoin.Sum(x => x.count);
                    int nonCoinjoinOutputCount = transactionOutputCount - coinjoinOutputCount;
                    if (coinjoin.Any() && coinjoin.First().count <= tx.Inputs.Count && coinjoinOutputCount >= nonCoinjoinOutputCount)
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
                    await File.AppendAllTextAsync(coinjoinsFilePath, content);
                }
            }
        }

        private static async Task DoMonthlyStatAsync(string coinjoinsFilePath)
        {
            var monthlyStatFilePath = "MonthlyStat.txt";
            var lastDate = DateTimeOffset.MinValue.Date;
            Money volume = Money.Zero;
            foreach (var cj in await File.ReadAllLinesAsync(coinjoinsFilePath))
            {
                var parts = cj.Split(':');
                var currentDate = DateTimeOffset.Parse(parts[0]).Date;
                var cnt = int.Parse(parts[3]);
                var val = Money.Parse(parts[4]);
                Money currentVolume = cnt * val;

                volume += currentVolume;
                if (currentDate.Date.Month != lastDate.Date.Month)
                {
                    var content = $"{lastDate.ToString("yyyy-MM")}:{volume.ToString(false, false)}\n";
                    Console.Write(content);
                    await File.AppendAllTextAsync(monthlyStatFilePath, content);
                    lastDate = currentDate;
                    volume = Money.Zero;
                }
            }

            return;
        }

        public static IEnumerable<(Money value, int count)> GetIndistinguishableOutputs(this Transaction me)
        {
            return me.Outputs.GroupBy(x => x.Value)
               .ToDictionary(x => x.Key, y => y.Count())
               .Select(x => (x.Key, x.Value));
        }
    }
}
