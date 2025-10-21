# IBKR TWS API wrapper

Minimalistic async wrapper around IBKR TWS API for Interactive Brokers.

# Status 

![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/Indemos/Terminal/dotnet.yml?event=push)
![GitHub](https://img.shields.io/github/license/Indemos/Terminal)
![GitHub](https://img.shields.io/badge/system-Windows%20%7C%20Linux%20%7C%20Mac-blue)

# Nuget 

`dotnet add package InterBroker --version 0.0.1`

# Usage 

```C#

var broker = new InterBroker
{
  Port = 7497
};

var contract = new Contract
{
  Symbol = "ES",
  LocalSymbol = "ESZ5",
  SecType = "FUT",
  Exchange = "CME",
  Currency = "USD",
  LastTradeDateOrContractMonth = "202512"
};

var optionContract = new Contract
{
  Symbol = "AAPL",
  SecType = "OPT",
  Exchange = "SMART",
  Currency = "USD",
  LastTradeDateOrContractMonth = "20251017"
};

broker.Connect();

// Requests

var account = "<AccountNumber>";
var cleaner = CancellationToken.None;
var contracts = await broker.GetContracts(cleaner, contract);
var bars = await broker.GetBars(cleaner, contract, DateTime.Now, "1 D", "1 min", "MIDPOINT");
var prices = await broker.GetTicks(cleaner, contract, DateTime.Now.AddDays(-5), DateTime.Now, "BID_ASK", 100);
var options = await broker.GetContracts(cleaner, optionContract);
var summary = await broker.GetAccountSummary(cleaner);
var orders = await broker.GetOrders(cleaner);
var positions = await broker.GetPositions(cleaner, account);

// Subscriptions

var dataMessage = new DataStreamMessage
{
  DataTypes = [SubscriptionEnum.Price],
  Contract = contract
};

var priceSub = broker.SubscribeToTicks(dataMessage, o => Console.WriteLine("Price: " + JsonSerializer.Serialize(o)));
var accountSub = broker.SubscribeToAccounts(account, o => Console.WriteLine("Account: " + JsonSerializer.Serialize(o)));
var positionSub = broker.SubscribeToPositions(account, o => Console.WriteLine("Position: " + JsonSerializer.Serialize(o)));

//broker.SubscribeToOrders(o => Console.WriteLine("Order: " + JsonSerializer.Serialize(o)));

// Orders

var order = new Order
{
  Action = "BUY",
  OrderType = "LMT",
  TotalQuantity = 1,
  LmtPrice = prices.Last().Last.Value,
};

var orderResponse = await broker.SendOrder(
  cleaner,
  contracts.Last().Contract,
  order,
  order.LmtPrice - 50,
  order.LmtPrice + 50);

var orderStatus = await broker.ClearOrder(cleaner, orderResponse.Last().OrderId);

Console.ReadKey();

broker.Unsubscribe(priceSub);
broker.UnsubscribeFromUpdates(accountSub);
broker.UnsubscribeFromUpdates(positionSub);
broker.Disconnect();

```