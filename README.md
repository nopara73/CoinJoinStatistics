# CoinJoinStatistics

## Get The Requirements

0. Get Bitcoin Core: https://bitcoincore.org/
1. Get Git: https://git-scm.com/downloads
2. Get .NET Core 2.2 SDK: https://www.microsoft.com/net/download (Note, you can disable .NET's telemetry by typing `export DOTNET_CLI_TELEMETRY_OPTOUT=1` on Linux and OSX or `set DOTNET_CLI_TELEMETRY_OPTOUT=1` on Windows.)

## Configure Bitcoin Core

```
txindex=1
server=1
rpcuser=foouser
rpcpassword=barpassword
```

## Get Wasabi

Clone & Restore & Build

```sh
git clone https://github.com/nopara73/CoinJoinStatistics.git
cd WalletWasabi/WalletWasabi.Gui
dotnet build -c Release
```

## Run

Before you run the software, run Bitcoin Core.

First run it with the following arguments, this'll gather all the coinjoin-like transactions. This may take a day or so to run.

`dotnet run -c Release -- foouser barpassword`

Next run it without arguments: `dotnet run -c Release`. This will create monthly statistics of the coinjoins.
