using IBApi;
using IBApi.Enums;
using IBApi.Messages;
using IBApi.Queries;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Demo
{
  public class Program
  {
    static async Task Main(string[] args)
    {
      var broker = new InterBroker
      {
        Port = 7497,
        Timeout = TimeSpan.FromSeconds(5)
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

      var id = await broker.Connect();

      // Requests

      var account = "";
      var cleaner = CancellationToken.None;
      var contracts = await broker.GetContracts(contract, cleaner);
      var bars = await broker.GetBars(new HistoricalBarsQuery { Contract = contract, MaxDate = DateTime.Now, Duration = "1 D", BarType = "1 min", DataType = "MIDPOINT" }, cleaner);
      var prices = await broker.GetTicks(new HistoricalTicksQuery { Contract = contract, MinDate = DateTime.Now.AddDays(-5), MaxDate = DateTime.Now, DataType = "BID_ASK", Count = 10 }, cleaner);
      var options = await broker.GetContracts(optionContract, cleaner);
      var summary = await broker.GetAccountSummary(cleaner);
      var orders = await broker.GetOrders(cleaner);
      var positions = await broker.GetPositions(account, cleaner);
      var executions = await broker.GetTransactions(new ExecutionFilter(), cleaner);

      // Subscriptions

      var dataMessage = new PriceStreamMessage
      {
        DataTypes = [SubscriptionEnum.Price],
        Contract = contracts.Last().Contract,
        Account = account
      };

      broker.SubscribeToPositions(account, o => Console.WriteLine("\n\n Position: " + JsonSerializer.Serialize(o)));
      broker.SubscribeToAccounts(account, o => Console.WriteLine("Account: " + JsonSerializer.Serialize(o)));
      broker.SubscribeToPositions(account, o => Console.WriteLine("Position: " + JsonSerializer.Serialize(o)));
      broker.SubscribeToOrders(o => Console.WriteLine("\n\n Order: " + JsonSerializer.Serialize(o)));

      var counter = 0;
      var stamp = DateTime.Now;

      broker.SubscribeToTicks(dataMessage, async o =>
      {
        Console.WriteLine(stamp + " : " + (counter++));

        orders = await broker.GetOrders(cleaner);
        positions = [.. (await broker.GetPositions(account, cleaner)).Where(o => o.Position is not 0)];

        Console.WriteLine($"Orders: {orders.Length}, Positions: {positions.Length}");

        if (orders.Length is not 0)
        {
          //return;
        }

        var order = new Order
        {
          Action = "BUY",
          OrderType = "MKT",
          TotalQuantity = 1,
        };

        var orderResponse = broker.SendOrder(
          contracts.Last().Contract,
          order,
          order.LmtPrice - 50,
          order.LmtPrice + 50);

        Console.WriteLine($"Order => {order.OrderId}");

        broker.ClearOrder(orderResponse.Item1.OrderId);
      });

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

      var comboResponse = broker.SendOrder(
        contract,
        comboOrder,
        comboOrder.LmtPrice - 0.05,
        comboOrder.LmtPrice + 0.05);

      Console.ReadKey();

      broker.Disconnect();
    }
  }
}
