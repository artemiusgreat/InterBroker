using IBApi;
using InteractiveBrokers;

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

      Console.ReadKey();

      broker.Disconnect();
    }
  }
}
