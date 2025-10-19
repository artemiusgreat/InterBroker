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
        LastTradeDate = "20251015"
      };

      broker.Connect();

      var contracts = await broker.GetContracts(contract);
      var bars = await broker.GetBars(contract, DateTime.Now, "1 D", "1 min", "MIDPOINT");
      var prices = await broker.GetTicks(contract, DateTime.Now.AddDays(-5), DateTime.Now, "BID_ASK", 100);
      var options = await broker.GetContracts(optionContract);
      var orders = await broker.GetOrders();
      var positions = await broker.GetPositions("AccountNumber");

      var order = new Order
      {
        Action = "BUY",
        OrderType = "LMT",
        TotalQuantity = 1,
        LmtPrice = prices.Last().PriceAsk,
      };

      var orderResponse = await broker.SendOrder(
        contract,
        order,
        order.LmtPrice - 50,
        order.LmtPrice + 50);

      var orderStatus = await broker.ClearOrder(orderResponse.Last().OrderId);
      var subscriptionId = await broker.SubscribeToTicks(price => Console.WriteLine($"Price: {JsonSerializer.Serialize(price)}"), contract, "BID_ASK");

      Console.ReadKey();

      broker.Unsubscribe(subscriptionId);
      broker.Disconnect();
    }
  }
}
