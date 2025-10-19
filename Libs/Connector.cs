using IBApi;
using InteractiveBrokers.Enums;
using InteractiveBrokers.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static InteractiveBrokers.IBClient;

namespace InteractiveBrokers
{
  public class InterBroker
  {
    public virtual IBClient Instance { get; set; }

    public virtual int Port { get; set; } = 7497;
    public virtual int Range { get; set; } = short.MaxValue;
    public virtual int Id => Instance.NextOrderId + Range++;

    public virtual string Host { get; set; } = "localhost";

    public virtual TimeSpan Span { get; set; } = TimeSpan.Zero;
    public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public Action<PriceMessage> OnPrice { get; set; } = o => { };
    public Action<ComputationMessage> OnComputation { get; set; } = o => { };

    /// <summary>
    /// Connect
    /// </summary>
    /// <param name="id"></param>
    public virtual void Connect(int id = 0)
    {
      var signal = new EReaderMonitorSignal();

      Instance = new IBClient(signal);
      Instance.ClientSocket.eConnect(Host, Port, id);

      var reader = new EReader(Instance.ClientSocket, signal);
      var process = new Thread(() =>
      {
        while (Instance.ClientSocket.IsConnected())
        {
          signal.waitForSignal();
          reader.processMsgs();
        }
      });

      process.Start();
      reader.Start();
    }

    /// <summary>
    /// Save state and dispose
    /// </summary>
    public virtual void Disconnect()
    {
      Instance?.ClientSocket?.eDisconnect();
    }

    /// <summary>
    /// Unsubscribe from data streams
    /// </summary>
    /// <param name="id"></param>
    public virtual void Unsubscribe(int id)
    {
      Instance.ClientSocket.cancelMktData(id);
    }

    /// <summary>
    /// Get contract definition
    /// </summary>
    /// <param name="cleaner"></param>
    /// <param name="contract"></param>
    public virtual async Task<List<ContractDetails>> GetContracts(CancellationToken cleaner, Contract contract)
    {
      var nextId = Id;
      var response = new List<ContractDetails>();
      var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      void subscribe(ContractDetailsMessage message)
      {
        if (Equals(nextId, message.RequestId))
        {
          response.Add(message.ContractDetails);
        }
      }

      void unsubscribe(int id)
      {
        Instance.ContractDetails -= subscribe;
        Instance.ContractDetailsEnd -= unsubscribe;
        source.TrySetResult(true);
      }

      Instance.ContractDetails += subscribe;
      Instance.ContractDetailsEnd += unsubscribe;
      Instance.ClientSocket.reqContractDetails(nextId, contract);

      await await Task.WhenAny(source.Task, Task.Delay(Timeout, cleaner).ContinueWith(o => unsubscribe(nextId)));
      await Task.Delay(Span);

      return response;
    }

    /// <summary>
    /// Get historical ticks
    /// </summary>
    /// <param name="cleaner"></param>
    /// <param name="contract"></param>
    /// <param name="minDate"></param>
    /// <param name="maxDate"></param>
    /// <param name="dataType"></param>
    /// <param name="count"></param>
    /// <param name="session"></param>
    public virtual async Task<IList<HistoricalTickBidAsk>> GetTicks(CancellationToken cleaner, Contract contract, DateTime minDate, DateTime maxDate, string dataType, int count = 1, int session = 0)
    {
      var nextId = Id;
      var items = new HistoricalTickBidAsk[0];
      var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      void subscribe(HistoricalTicksMessage message)
      {
        if (Equals(nextId, message.ReqId))
        {
          items = message.Items;
          unsubscribe();
        }
      }

      void unsubscribe()
      {
        Instance.historicalTicksList -= subscribe;
        source.TrySetResult(true);
      }

      var minDateStr = minDate.ToString($"yyyyMMdd-HH:mm:ss");
      var maxDateStr = maxDate.ToString($"yyyyMMdd-HH:mm:ss");

      Instance.historicalTicksList += subscribe;
      Instance.ClientSocket.reqHistoricalTicks(nextId, contract, minDateStr, maxDateStr, count, dataType, session, false, null);

      await await Task.WhenAny(source.Task, Task.Delay(Timeout, cleaner).ContinueWith(o => unsubscribe()));
      await Task.Delay(Span);

      return items;
    }

    /// <summary>
    /// Get historical bars
    /// </summary>
    /// <param name="cleaner"></param>
    /// <param name="contract"></param>
    /// <param name="maxDate"></param>
    /// <param name="duration"></param>
    /// <param name="barType"></param>
    /// <param name="dataType"></param>
    /// <param name="session"></param>
    public virtual async Task<IList<HistoricalDataMessage>> GetBars(CancellationToken cleaner, Contract contract, DateTime maxDate, string duration, string barType, string dataType, int session = 0)
    {
      var nextId = Id;
      var items = new List<HistoricalDataMessage>();
      var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      void subscribe(HistoricalDataMessage message)
      {
        if (Equals(nextId, message.RequestId))
        {
          items.Add(message);
        }
      }

      void unsubscribe(HistoricalDataEndMessage message)
      {
        Instance.HistoricalData -= subscribe;
        Instance.HistoricalDataEnd -= unsubscribe;
        Instance.ClientSocket.cancelHistoricalData(nextId);
        source.TrySetResult(true);
      }

      var maxDateStr = maxDate.ToString($"yyyyMMdd-HH:mm:ss");

      Instance.HistoricalData += subscribe;
      Instance.HistoricalDataEnd += unsubscribe;
      Instance.ClientSocket.reqHistoricalData(nextId, contract, maxDateStr, duration, barType, dataType, session, 1, false, null);

      await await Task.WhenAny(source.Task, Task.Delay(Timeout, cleaner).ContinueWith(o => unsubscribe(null)));
      await Task.Delay(Span);

      return items;
    }

    /// <summary>
    /// Get orders
    /// </summary>
    /// <param name="cleaner"></param>
    public virtual async Task<OpenOrderMessage[]> GetOrders(CancellationToken cleaner)
    {
      var orders = new ConcurrentDictionary<string, OpenOrderMessage>();
      var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      void subscribe(OpenOrderMessage message)
      {
        orders[$"{message.Order.PermId}"] = message;
      }

      void unsubscribe()
      {
        Instance.OpenOrder -= subscribe;
        Instance.OpenOrderEnd -= unsubscribe;
        source.TrySetResult(true);
      }

      Instance.OpenOrder += subscribe;
      Instance.OpenOrderEnd += unsubscribe;
      Instance.ClientSocket.reqAllOpenOrders();

      await await Task.WhenAny(source.Task, Task.Delay(Timeout, cleaner).ContinueWith(o => unsubscribe()));
      await Task.Delay(Span);

      return orders.Values.ToArray();
    }

    /// <summary>
    /// Get positions 
    /// </summary>
    /// <param name="cleaner"></param>
    /// <param name="account"></param>
    public virtual async Task<PositionMultiMessage[]> GetPositions(CancellationToken cleaner, string account)
    {
      var nextId = Id;
      var positions = new ConcurrentDictionary<string, PositionMultiMessage>();
      var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      void subscribe(PositionMultiMessage message)
      {
        if (Equals(nextId, message.ReqId))
        {
          positions[$"{message.Contract.LocalSymbol}"] = message;
        }
      }

      void unsubscribe(int reqId)
      {
        if (Equals(nextId, reqId))
        {
          Instance.PositionMulti -= subscribe;
          Instance.PositionMultiEnd -= unsubscribe;
          Instance.ClientSocket.cancelPositionsMulti(nextId);
          source.TrySetResult(true);
        }
      }

      Instance.PositionMulti += subscribe;
      Instance.PositionMultiEnd += unsubscribe;
      Instance.ClientSocket.reqPositionsMulti(nextId, account, string.Empty);

      await await Task.WhenAny(source.Task, Task.Delay(Timeout, cleaner).ContinueWith(o => unsubscribe(nextId)));
      await Task.Delay(Span);

      return positions.Values.ToArray();
    }

    /// <summary>
    /// Subscribe to computations
    /// </summary>
    /// <param name="contract"></param>
    /// <param name="dataType"></param>
    /// <param name="snapshot"></param>
    /// <param name="regSnapshot"></param>
    public virtual async Task<int> SubscribeToComputations(Contract contract, string dataType, bool snapshot = false, bool regSnapshot = false)
    {
      var nextId = Id;
      var response = new ComputationMessage();

      double? value(double data, double min, double max, double? original)
      {
        switch (true)
        {
          case true when data < short.MinValue:
          case true when data > short.MaxValue:
          case true when data < min:
          case true when data > max: return original;
        }

        return Math.Round(data, 2);
      }

      void subscribe(TickOptionMessage message)
      {
        if (Equals(nextId, message.RequestId))
        {
          response.Delta = value(message.Delta, -1, 1, null);
          response.Gamma = value(message.Gamma, 0, short.MaxValue, null);
          response.Theta = value(message.Theta, 0, short.MaxValue, null);
          response.Vega = value(message.Vega, 0, short.MaxValue, null);

          OnComputation(response);
        }
      }

      Instance.TickOptionCommunication += subscribe;
      Instance.ClientSocket.reqMktData(nextId, contract, dataType, snapshot, regSnapshot, null);

      await Task.Delay(Span);

      return nextId;
    }

    /// <summary>
    /// Get latest quote
    /// </summary>
    /// <param name="contract"></param>
    /// <param name="dataType"></param>
    /// <param name="snapshot"></param>
    /// <param name="regSnapshot"></param>
    public virtual async Task<int> SubscribeToTicks(Contract contract, string dataType, bool snapshot = false, bool regSnapshot = false)
    {
      var nextId = Id;
      var response = new PriceMessage();

      void subscribeToPrices(TickPriceMessage message)
      {
        if (Equals(nextId, message.RequestId))
        {
          switch (message.Field)
          {
            case (int)PropertyEnum.BidSize: response.BidSize = message.Data ?? response.BidSize; break;
            case (int)PropertyEnum.AskSize: response.AskSize = message.Data ?? response.AskSize; break;
            case (int)PropertyEnum.BidPrice: response.Bid = message.Data ?? response.Bid; break;
            case (int)PropertyEnum.AskPrice: response.Ask = message.Data ?? response.Ask; break;
            case (int)PropertyEnum.LastPrice: response.Last = message.Data ?? response.Last; break;
          }

          response.Time = DateTime.Now.Ticks;
          response.Last = response.Last is null ? response.Bid ?? response.Ask : response.Last;

          if (response.Bid is null || response.Ask is null)
          {
            return;
          }

          OnPrice(response);
        }
      }

      Instance.TickPrice += subscribeToPrices;
      Instance.ClientSocket.reqMktData(nextId, contract, dataType, snapshot, regSnapshot, null);

      await Task.Delay(Span);

      return nextId;
    }

    /// <summary>
    /// Sync open balance, order, and positions 
    /// </summary>
    /// <param name="cleaner"></param>
    protected virtual async Task<Dictionary<string, string>> GetAccountSummary(CancellationToken cleaner)
    {
      var nextId = Id;
      var response = new Dictionary<string, string>();
      var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      void subscribe(AccountSummaryMessage message)
      {
        response[message.Tag] = message.Value;
      }

      void unsubscribe(AccountSummaryEndMessage message)
      {
        if (Equals(nextId, message?.RequestId))
        {
          Instance.AccountSummary -= subscribe;
          Instance.AccountSummaryEnd -= unsubscribe;
          Instance.ClientSocket.cancelAccountSummary(nextId);
          source.TrySetResult(true);
        }
      }

      Instance.AccountSummary += subscribe;
      Instance.AccountSummaryEnd += unsubscribe;
      Instance.ClientSocket.reqAccountSummary(nextId, "All", AccountSummaryTags.GetAllTags());

      await await Task.WhenAny(source.Task, Task.Delay(Timeout, cleaner).ContinueWith(o => unsubscribe(null)));
      await Task.Delay(Span);

      return response;
    }

    /// <summary>
    /// Send order
    /// </summary>
    /// <param name="cleaner"></param>
    /// <param name="contract"></param>
    /// <param name="orderMessage"></param>
    /// <param name="stopPrice"></param>
    /// <param name="takePrice"></param>
    public virtual async Task<IList<OpenOrderMessage>> SendOrder(CancellationToken cleaner, Contract contract, Order orderMessage, double? stopPrice = null, double? takePrice = null)
    {
      var states = new List<Task>();
      var orderId = Instance.NextOrderId;
      var response = new Dictionary<int, OpenOrderMessage>();
      var orders = CreateOrder(orderMessage, stopPrice, takePrice);

      foreach (var order in orders.Where(o => o.Transmit is false))
      {
        Instance.ClientSocket.placeOrder(order.OrderId, contract, order);
      }

      foreach (var order in orders.Where(o => o.Transmit))
      {
        var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void subscribe(OpenOrderMessage message)
        {
          if (Equals(order.OrderId, message.OrderId))
          {
            response[message.OrderId] = message;
            unsubscribe();
          }
        }

        void unsubscribe()
        {
          Instance.OpenOrder -= subscribe;
          Instance.OpenOrderEnd -= unsubscribe;
          source.TrySetResult(true);
        }

        Instance.OpenOrder += subscribe;
        Instance.OpenOrderEnd += unsubscribe;
        Instance.ClientSocket.placeOrder(order.OrderId, contract, order);

        states.Add(source.Task);
      }

      await await Task.WhenAny(Task.WhenAll(states), Task.Delay(Timeout, cleaner));
      await Task.Delay(Span);

      return response.Values.ToArray();
    }

    /// <summary>
    /// Cancel order
    /// </summary>
    /// <param name="cleaner"></param>
    /// <param name="orderId"></param>
    public virtual async Task<OrderStatusMessage> ClearOrder(CancellationToken cleaner, int orderId)
    {
      var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      var response = null as OrderStatusMessage;

      void subscribe(OrderStatusMessage message)
      {
        if (Equals(orderId, message.OrderId))
        {
          response = message;
          unsubscribe();
        }
      }

      void unsubscribe()
      {
        Instance.OrderStatus -= subscribe;
        source.TrySetResult(true);
      }

      Instance.OrderStatus += subscribe;
      Instance.ClientSocket.cancelOrder(orderId, string.Empty);

      await await Task.WhenAny(source.Task, Task.Delay(Timeout, cleaner).ContinueWith(o => unsubscribe()));
      await Task.Delay(Span);

      return response;
    }

    /// <summary>
    /// Subscribe to connections
    /// </summary>
    /// <param name="action"></param>
    protected virtual void SubscribeToConnections(Action<string> action)
    {
      Instance.ConnectionClosed += () => action("No connection");
    }

    /// <summary>
    /// Subscribe to errors
    /// </summary>
    /// <param name="action"></param>
    public virtual void SubscribeToErrors(Action<int, int, string, string, Exception> action)
    {
      Instance.Error += (id, code, message, error, e) =>
      {
        switch (true)
        {
          case true when Equals(code, (int)ClientErrorEnum.NoConnection):
          case true when Equals(code, (int)ClientErrorEnum.ConnectionError): Connect(); break;
        }

        action(id, code, message, error, e);
      };
    }

    /// <summary>
    /// Subscribe orders
    /// </summary>
    /// <param name="action"></param>
    public virtual void SubscribeToOrders(Action<OpenOrderMessage> action)
    {
      Instance.OpenOrder += o => action(o);
      Instance.ClientSocket.reqAutoOpenOrders(true);
    }

    /// <summary>
    /// Bracket template
    /// </summary>
    /// <param name="order"></param>
    /// <param name="stopPrice"></param>
    /// <param name="takePrice"></param>
    protected virtual IList<Order> CreateOrder(Order order, double? stopPrice = null, double? takePrice = null)
    {
      var orders = new List<Order>();

      if (takePrice != null)
      {
        var TP = new Order
        {
          OrderType = "LMT",
          OrderId = order.OrderId + 1,
          Action = order?.Action == "BUY" ? "SELL" : "BUY",
          TotalQuantity = order.TotalQuantity,
          LmtPrice = takePrice.Value,
          ParentId = order.OrderId,
          Transmit = false
        };

        orders.Add(TP);
      }

      if (stopPrice != null)
      {
        var SL = new Order
        {
          OrderType = "STP",
          OrderId = order.OrderId + 2,
          Action = order?.Action == "BUY" ? "SELL" : "BUY",
          TotalQuantity = order.TotalQuantity,
          AuxPrice = stopPrice.Value,
          ParentId = order.OrderId,
          Transmit = false
        };

        orders.Add(SL);
      }

      order.Transmit = true;
      order.OrderId = Instance.NextOrderId;
      orders.Add(order);

      return orders;
    }
  }
}
