# IBKR TWS API wrapper

Minimalistic async wrapper around IBKR TWS API for Interactive Brokers.

# Status 

![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/Indemos/Terminal/dotnet.yml?event=push)
![GitHub](https://img.shields.io/github/license/Indemos/Terminal)
![GitHub](https://img.shields.io/badge/system-Windows%20%7C%20Linux%20%7C%20Mac-blue)

# Usage 

```C#

var broker = new InterBroker
{
  Port = 7497
};

var contract = new Contract
{
  Symbol = "AAPL",
  SecType = "STK",
  Exchange = "SMART",
  Currency = "USD"
};

var optionContract = new Contract
{
  Symbol = "AAPL",
  SecType = "OPT",
  Exchange = "SMART",
  Currency = "USD",
  LastTradeDate = "20251015"
};

broker.Connect();

var contracts = await broker.GetContracts(contract);
var bars = await broker.GetBars(contract, DateTime.Now, "1 D", "1 min", "MIDPOINT");
var prices = await broker.GetTicks(contract, DateTime.Now.AddDays(-5), DateTime.Now, "BID_ASK", 100);
var options = await broker.GetContracts(optionContract);
var orders = await broker.GetOrders();
var positions = await broker.GetPositions("DU9471614");

Console.ReadKey();

broker.Disconnect();

```