using IBApi;
using InteractiveBrokers;
using System.Text.Json;

namespace Demo
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
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
        LastTradeDateOrContractMonth = "20251017"
      };

      broker.Connect();

      var cleaner = CancellationToken.None;
      var contracts = await broker.GetContracts(cleaner, contract);
      var bars = await broker.GetBars(cleaner, contract, DateTime.Now, "1 D", "1 min", "MIDPOINT");
      var prices = await broker.GetTicks(cleaner, contract, DateTime.Now.AddDays(-5), DateTime.Now, "BID_ASK", 100);
      var options = await broker.GetContracts(cleaner, optionContract);
      var orders = await broker.GetOrders(cleaner);
      var positions = await broker.GetPositions(cleaner, "AccountNumber");

      var order = new Order
      {
        Action = "BUY",
        OrderType = "LMT",
        TotalQuantity = 1,
        LmtPrice = prices.Last().PriceAsk,
      };

      var orderResponse = await broker.SendOrder(
        cleaner,
        contracts.Last().Contract,
        order,
        order.LmtPrice - 50,
        order.LmtPrice + 50);

      broker.OnPrice += price => Console.WriteLine(JsonSerializer.Serialize(price));

      var orderStatus = await broker.ClearOrder(cleaner, orderResponse.Last().OrderId);
      var subscriptionId = await broker.SubscribeToTicks(contract, "BID_ASK");

      Console.ReadKey();

      broker.Unsubscribe(subscriptionId);
      broker.Disconnect();
    }
  }
}
