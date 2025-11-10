using IBApi;
using IBApi.Enums;
using IBApi.Messages;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Demo
{
  public class Program
  {
    static async Task Main(string[] args)
    {
      var broker = new InterBroker
      {
        Port = 7497,
        Timeout = TimeSpan.FromSeconds(1)
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
      var cleaner = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
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

      broker.SubscribeToAccounts(account, o => Console.WriteLine("Account: " + JsonSerializer.Serialize(o)));
      broker.SubscribeToPositions(account, o => Console.WriteLine("Position: " + JsonSerializer.Serialize(o)));
      broker.SubscribeToOrders(o => Console.WriteLine("Order: " + JsonSerializer.Serialize(o)));

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

      // Combo orders 

      contract.SecType = "BAG";
      contract.ComboLegs.Add(new()
      {
        ConId = 826895725, // E2DZ5-C6900
        Ratio = 1,
        Action = "SELL",
        Exchange = "CME",
      });

      contract.ComboLegs.Add(new()
      {
        ConId = 826895994, // E2DZ5-C6800
        Ratio = 1,
        Action = "BUY",
        Exchange = "CME",
      });

      var comboOrder = new Order
      {
        Action = "BUY",
        OrderType = "LMT",
        TotalQuantity = 1,
        LmtPrice = 0.10,
      };

      var comboResponse = await broker.SendOrder(
        cleaner,
        contract,
        comboOrder,
        comboOrder.LmtPrice - 0.05,
        comboOrder.LmtPrice + 0.05);

      Console.ReadKey();

      broker.Unsubscribe(priceSub);
      broker.UnsubscribeFromUpdates(account);
      broker.Disconnect();
    }
  }
}
