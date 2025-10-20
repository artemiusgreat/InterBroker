using IBApi.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IBApi
{
  public class IBClient : EWrapper
  {
    public Task<Contract> ResolveContractAsync(int conId, string refExch)
    {
      var reqId = new Random(DateTime.Now.Millisecond).Next();
      var resolveResult = new TaskCompletionSource<Contract>();
      var resolveContract_Error = new Action<int, long, int, string, string, Exception>((id, errorTime, code, msg, advancedOrderRejectJson, ex) =>
      {
        if (reqId != id)
          return;

        resolveResult.SetResult(null);
      });
      var resolveContract = new Action<ContractDetailsMessage>(msg =>
      {
        if (msg.RequestId == reqId)
          resolveResult.SetResult(msg.ContractDetails.Contract);
      });
      var contractDetailsEnd = new Action<int>(id =>
      {
        if (reqId == id && !resolveResult.Task.IsCompleted)
          resolveResult.SetResult(null);
      });

      var tmpError = Error;
      var tmpContractDetails = ContractDetails;
      var tmpContractDetailsEnd = ContractDetailsEnd;

      Error = resolveContract_Error;
      ContractDetails = resolveContract;
      ContractDetailsEnd = contractDetailsEnd;

      resolveResult.Task.ContinueWith(t =>
      {
        Error = tmpError;
        ContractDetails = tmpContractDetails;
        ContractDetailsEnd = tmpContractDetailsEnd;
      });

      ClientSocket.reqContractDetails(reqId, new Contract
      { ConId = conId, Exchange = refExch });

      return resolveResult.Task;
    }

    public Task<Contract[]> ResolveContractAsync(string secType, string symbol, string currency, string exchange)
    {
      var reqId = new Random(DateTime.Now.Millisecond).Next();
      var res = new TaskCompletionSource<Contract[]>();
      var contractList = new List<Contract>();
      var resolveContract_Error = new Action<int, long, int, string, string, Exception>((id, errorTime, code, msg, advancedOrderRejectJson, ex) =>
      {
        if (reqId != id)
          return;

        res.SetResult(new Contract[0]);
      });
      var contractDetails = new Action<ContractDetailsMessage>(msg =>
      {
        if (reqId != msg.RequestId)
          return;

        contractList.Add(msg.ContractDetails.Contract);
      });
      var contractDetailsEnd = new Action<int>(id =>
      {
        if (reqId == id)
          res.SetResult(contractList.ToArray());
      });

      var tmpError = Error;
      var tmpContractDetails = ContractDetails;
      var tmpContractDetailsEnd = ContractDetailsEnd;

      Error = resolveContract_Error;
      ContractDetails = contractDetails;
      ContractDetailsEnd = contractDetailsEnd;

      res.Task.ContinueWith(t =>
      {
        Error = tmpError;
        ContractDetails = tmpContractDetails;
        ContractDetailsEnd = tmpContractDetailsEnd;
      });

      ClientSocket.reqContractDetails(reqId, new Contract
      { SecType = secType, Symbol = symbol, Currency = currency, Exchange = exchange });

      return res.Task;
    }

    public int ClientId { get; set; }

    SynchronizationContext sc;

    public IBClient(EReaderSignal signal)
    {
      ClientSocket = new EClientSocket(this, signal);
      sc = SynchronizationContext.Current;
    }

    public EClientSocket ClientSocket { get; private set; }

    public int NextOrderId { get; set; }

    public event Action<int, long, int, string, string, Exception> Error;

    void EWrapper.error(Exception e)
    {
      var tmp = Error;

      if (tmp != null)
        Run(t => tmp(0, Util.CurrentTimeMillis(), 0, null, null, e), null);
    }

    void EWrapper.error(string str)
    {
      var tmp = Error;

      if (tmp != null)
        Run(t => tmp(0, Util.CurrentTimeMillis(), 0, str, null, null), null);
    }

    void EWrapper.error(int id, long errorTime, int errorCode, string errorMsg, string advancedOrderRejectJson)
    {
      var tmp = Error;

      if (tmp != null)
        Run(t => tmp(id, errorTime, errorCode, errorMsg, advancedOrderRejectJson, null), null);
    }

    public event Action ConnectionClosed;

    void EWrapper.connectionClosed()
    {
      var tmp = ConnectionClosed;

      if (tmp != null)
        Run(t => tmp(), null);
    }

    public event Action<long> CurrentTime;

    void EWrapper.currentTime(long time)
    {
      var tmp = CurrentTime;

      if (tmp != null)
        Run(t => tmp(time), null);
    }

    public event Action<TickPriceMessage> TickPrice;

    void EWrapper.tickPrice(int tickerId, int field, double price, TickAttrib attribs)
    {
      var tmp = TickPrice;

      if (tmp != null)
        Run(t => tmp(new TickPriceMessage(tickerId, field, price, attribs)), null);
    }

    public event Action<TickSizeMessage> TickSize;

    void EWrapper.tickSize(int tickerId, int field, decimal size)
    {
      var tmp = TickSize;

      if (tmp != null)
        Run(t => tmp(new TickSizeMessage(tickerId, field, size)), null);
    }

    public event Action<int, int, string> TickString;

    void EWrapper.tickString(int tickerId, int tickType, string value)
    {
      var tmp = TickString;

      if (tmp != null)
        Run(t => tmp(tickerId, tickType, value), null);
    }

    public event Action<TickGenericMessage> TickGeneric;

    void EWrapper.tickGeneric(int tickerId, int field, double value)
    {
      var tmp = TickGeneric;

      if (tmp != null)
        Run(t => tmp(new TickGenericMessage(tickerId, field, value)), null);
    }

    public event Action<int, int, double, string, double, int, string, double, double> TickEFP;

    void EWrapper.tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
    {
      var tmp = TickEFP;

      if (tmp != null)
        Run(t => tmp(tickerId, tickType, basisPoints, formattedBasisPoints, impliedFuture, holdDays, futureLastTradeDate, dividendImpact, dividendsToLastTradeDate), null);
    }

    public event Action<int> TickSnapshotEnd;

    void EWrapper.tickSnapshotEnd(int tickerId)
    {
      var tmp = TickSnapshotEnd;

      if (tmp != null)
        Run(t => tmp(tickerId), null);
    }

    public event Action<ConnectionStatusMessage> NextValidId;

    void EWrapper.nextValidId(int orderId)
    {
      var tmp = NextValidId;

      if (tmp != null)
        Run(t => tmp(new ConnectionStatusMessage(true)), null);

      NextOrderId = orderId;
    }

    public event Action<int, DeltaNeutralContract> DeltaNeutralValidation;

    void EWrapper.deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract)
    {
      var tmp = DeltaNeutralValidation;

      if (tmp != null)
        Run(t => tmp(reqId, deltaNeutralContract), null);
    }

    public event Action<ManagedAccountsMessage> ManagedAccounts;

    void EWrapper.managedAccounts(string accountsList)
    {
      var tmp = ManagedAccounts;

      if (tmp != null)
        Run(t => tmp(new ManagedAccountsMessage(accountsList)), null);
    }

    public event Action<TickOptionMessage> TickOptionCommunication;

    void EWrapper.tickOptionComputation(int tickerId, int field, int tickAttrib, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
    {
      var tmp = TickOptionCommunication;

      if (tmp != null)
        Run(t => tmp(new TickOptionMessage(tickerId, field, tickAttrib, impliedVolatility, delta, optPrice, pvDividend, gamma, vega, theta, undPrice)), null);
    }

    public event Action<AccountSummaryMessage> AccountSummary;

    void EWrapper.accountSummary(int reqId, string account, string tag, string value, string currency)
    {
      var tmp = AccountSummary;

      if (tmp != null)
        Run(t => tmp(new AccountSummaryMessage(reqId, account, tag, value, currency)), null);
    }

    public event Action<AccountSummaryEndMessage> AccountSummaryEnd;

    void EWrapper.accountSummaryEnd(int reqId)
    {
      var tmp = AccountSummaryEnd;

      if (tmp != null)
        Run(t => tmp(new AccountSummaryEndMessage(reqId)), null);
    }

    public event Action<AccountValueMessage> UpdateAccountValue;

    void EWrapper.updateAccountValue(string key, string value, string currency, string accountName)
    {
      var tmp = UpdateAccountValue;

      if (tmp != null)
        Run(t => tmp(new AccountValueMessage(key, value, currency, accountName)), null);
    }

    public event Action<UpdatePortfolioMessage> UpdatePortfolio;

    void EWrapper.updatePortfolio(Contract contract, decimal position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName)
    {
      var tmp = UpdatePortfolio;

      if (tmp != null)
        Run(t => tmp(new UpdatePortfolioMessage(contract, position, marketPrice, marketValue, averageCost, unrealizedPNL, realizedPNL, accountName)), null);
    }

    public event Action<UpdateAccountTimeMessage> UpdateAccountTime;

    void EWrapper.updateAccountTime(string timestamp)
    {
      var tmp = UpdateAccountTime;

      if (tmp != null)
        Run(t => tmp(new UpdateAccountTimeMessage(timestamp)), null);
    }

    public event Action<AccountDownloadEndMessage> AccountDownloadEnd;

    void EWrapper.accountDownloadEnd(string account)
    {
      var tmp = AccountDownloadEnd;

      if (tmp != null)
        Run(t => tmp(new AccountDownloadEndMessage(account)), null);
    }

    public event Action<OrderStatusMessage> OrderStatus;

    void EWrapper.orderStatus(int orderId, string status, decimal filled, decimal remaining, double avgFillPrice, long permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
    {
      var tmp = OrderStatus;

      if (tmp != null)
        Run(t => tmp(new OrderStatusMessage(orderId, status, filled, remaining, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld, mktCapPrice)), null);
    }

    public event Action<OpenOrderMessage> OpenOrder;

    void EWrapper.openOrder(int orderId, Contract contract, Order order, OrderState orderState)
    {
      var tmp = OpenOrder;

      if (tmp != null)
        Run(t => tmp(new OpenOrderMessage(orderId, contract, order, orderState)), null);
    }

    public event Action OpenOrderEnd;

    void EWrapper.openOrderEnd()
    {
      var tmp = OpenOrderEnd;

      if (tmp != null)
        Run(t => tmp(), null);
    }

    public event Action<ContractDetailsMessage> ContractDetails;

    void EWrapper.contractDetails(int reqId, ContractDetails contractDetails)
    {
      var tmp = ContractDetails;

      if (tmp != null)
        Run(t => tmp(new ContractDetailsMessage(reqId, contractDetails)), null);
    }

    public event Action<int> ContractDetailsEnd;

    void EWrapper.contractDetailsEnd(int reqId)
    {
      var tmp = ContractDetailsEnd;

      if (tmp != null)
        Run(t => tmp(reqId), null);
    }

    public event Action<ExecutionMessage> ExecDetails;

    void EWrapper.execDetails(int reqId, Contract contract, Execution execution)
    {
      var tmp = ExecDetails;

      if (tmp != null)
        Run(t => tmp(new ExecutionMessage(reqId, contract, execution)), null);
    }

    public event Action<int> ExecDetailsEnd;

    void EWrapper.execDetailsEnd(int reqId)
    {
      var tmp = ExecDetailsEnd;

      if (tmp != null)
        Run(t => tmp(reqId), null);
    }

    public event Action<CommissionAndFeesReport> CommissionAndFeesReport;

    void EWrapper.commissionAndFeesReport(CommissionAndFeesReport commissionAndFeesReport)
    {
      var tmp = CommissionAndFeesReport;

      if (tmp != null)
        Run(t => tmp(commissionAndFeesReport), null);
    }

    public event Action<FundamentalsMessage> FundamentalData;

    void EWrapper.fundamentalData(int reqId, string data)
    {
      var tmp = FundamentalData;

      if (tmp != null)
        Run(t => tmp(new FundamentalsMessage(data)), null);
    }

    public event Action<HistoricalDataMessage> HistoricalData;

    void EWrapper.historicalData(int reqId, Bar bar)
    {
      var tmp = HistoricalData;

      if (tmp != null)
        Run(t => tmp(new HistoricalDataMessage(reqId, bar)), null);
    }

    public event Action<HistoricalDataEndMessage> HistoricalDataEnd;

    void EWrapper.historicalDataEnd(int reqId, string startDate, string endDate)
    {
      var tmp = HistoricalDataEnd;

      if (tmp != null)
        Run(t => tmp(new HistoricalDataEndMessage(reqId, startDate, endDate)), null);
    }

    public event Action<MarketDataTypeMessage> MarketDataType;

    void EWrapper.marketDataType(int reqId, int marketDataType)
    {
      var tmp = MarketDataType;

      if (tmp != null)
        Run(t => tmp(new MarketDataTypeMessage(reqId, marketDataType)), null);
    }

    public event Action<DeepBookMessage> UpdateMktDepth;

    void EWrapper.updateMktDepth(int tickerId, int position, int operation, int side, double price, decimal size)
    {
      var tmp = UpdateMktDepth;

      if (tmp != null)
        Run(t => tmp(new DeepBookMessage(tickerId, position, operation, side, price, size, "", false)), null);
    }

    public event Action<DeepBookMessage> UpdateMktDepthL2;

    void EWrapper.updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, decimal size, bool isSmartDepth)
    {
      var tmp = UpdateMktDepthL2;

      if (tmp != null)
        Run(t => tmp(new DeepBookMessage(tickerId, position, operation, side, price, size, marketMaker, isSmartDepth)), null);
    }

    public event Action<int, int, string, string> UpdateNewsBulletin;

    void EWrapper.updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
    {
      var tmp = UpdateNewsBulletin;

      if (tmp != null)
        Run(t => tmp(msgId, msgType, message, origExchange), null);
    }

    public event Action<PositionMessage> Position;

    void EWrapper.position(string account, Contract contract, decimal pos, double avgCost)
    {
      var tmp = Position;

      if (tmp != null)
        Run(t => tmp(new PositionMessage(account, contract, pos, avgCost)), null);
    }

    public event Action PositionEnd;

    void EWrapper.positionEnd()
    {
      var tmp = PositionEnd;

      if (tmp != null)
        Run(t => tmp(), null);
    }

    public event Action<RealTimeBarMessage> RealtimeBar;

    void EWrapper.realtimeBar(int reqId, long time, double open, double high, double low, double close, decimal volume, decimal WAP, int count)
    {
      var tmp = RealtimeBar;

      if (tmp != null)
        Run(t => tmp(new RealTimeBarMessage(reqId, time, open, high, low, close, volume, WAP, count)), null);
    }

    public event Action<string> ScannerParameters;

    void EWrapper.scannerParameters(string xml)
    {
      var tmp = ScannerParameters;

      if (tmp != null)
        Run(t => tmp(xml), null);
    }

    public event Action<ScannerMessage> ScannerData;

    void EWrapper.scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
    {
      var tmp = ScannerData;

      if (tmp != null)
        Run(t => tmp(new ScannerMessage(reqId, rank, contractDetails, distance, benchmark, projection, legsStr)), null);
    }

    public event Action<int> ScannerDataEnd;

    void EWrapper.scannerDataEnd(int reqId)
    {
      var tmp = ScannerDataEnd;

      if (tmp != null)
        Run(t => tmp(reqId), null);
    }

    public event Action<AdvisorDataMessage> ReceiveFA;

    void EWrapper.receiveFA(int faDataType, string faXmlData)
    {
      var tmp = ReceiveFA;

      if (tmp != null)
        Run(t => tmp(new AdvisorDataMessage(faDataType, faXmlData)), null);
    }

    public event Action<BondContractDetailsMessage> BondContractDetails;

    void EWrapper.bondContractDetails(int requestId, ContractDetails contractDetails)
    {
      var tmp = BondContractDetails;

      if (tmp != null)
        Run(t => tmp(new BondContractDetailsMessage(requestId, contractDetails)), null);
    }

    public event Action<string> VerifyMessageAPI;

    void EWrapper.verifyMessageAPI(string apiData)
    {
      var tmp = VerifyMessageAPI;

      if (tmp != null)
        Run(t => tmp(apiData), null);
    }
    public event Action<bool, string> VerifyCompleted;

    void EWrapper.verifyCompleted(bool isSuccessful, string errorText)
    {
      var tmp = VerifyCompleted;

      if (tmp != null)
        Run(t => tmp(isSuccessful, errorText), null);
    }

    public event Action<string, string> VerifyAndAuthMessageAPI;

    void EWrapper.verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
    {
      var tmp = VerifyAndAuthMessageAPI;

      if (tmp != null)
        Run(t => tmp(apiData, xyzChallenge), null);
    }

    public event Action<bool, string> VerifyAndAuthCompleted;

    void EWrapper.verifyAndAuthCompleted(bool isSuccessful, string errorText)
    {
      var tmp = VerifyAndAuthCompleted;

      if (tmp != null)
        Run(t => tmp(isSuccessful, errorText), null);
    }

    public event Action<int, string> DisplayGroupList;

    void EWrapper.displayGroupList(int reqId, string groups)
    {
      var tmp = DisplayGroupList;

      if (tmp != null)
        Run(t => tmp(reqId, groups), null);
    }

    public event Action<int, string> DisplayGroupUpdated;

    void EWrapper.displayGroupUpdated(int reqId, string contractInfo)
    {
      var tmp = DisplayGroupUpdated;

      if (tmp != null)
        Run(t => tmp(reqId, contractInfo), null);
    }


    void EWrapper.connectAck()
    {
      if (ClientSocket.AsyncEConnect)
        ClientSocket.startApi();
    }

    public event Action<PositionMultiMessage> PositionMulti;

    void EWrapper.positionMulti(int reqId, string account, string modelCode, Contract contract, decimal pos, double avgCost)
    {
      var tmp = PositionMulti;

      if (tmp != null)
        Run(t => tmp(new PositionMultiMessage(reqId, account, modelCode, contract, pos, avgCost)), null);
    }

    public event Action<int> PositionMultiEnd;

    void EWrapper.positionMultiEnd(int reqId)
    {
      var tmp = PositionMultiEnd;

      if (tmp != null)
        Run(t => tmp(reqId), null);
    }

    public event Action<AccountUpdateMultiMessage> AccountUpdateMulti;

    void EWrapper.accountUpdateMulti(int reqId, string account, string modelCode, string key, string value, string currency)
    {
      var tmp = AccountUpdateMulti;

      if (tmp != null)
        Run(t => tmp(new AccountUpdateMultiMessage(reqId, account, modelCode, key, value, currency)), null);
    }

    public event Action<int> AccountUpdateMultiEnd;

    void EWrapper.accountUpdateMultiEnd(int reqId)
    {
      var tmp = AccountUpdateMultiEnd;

      if (tmp != null)
        Run(t => tmp(reqId), null);
    }

    public event Action<SecurityDefinitionOptionParameterMessage> SecurityDefinitionOptionParameter;

    void EWrapper.securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes)
    {
      var tmp = SecurityDefinitionOptionParameter;

      if (tmp != null)
        Run(t => tmp(new SecurityDefinitionOptionParameterMessage(reqId, exchange, underlyingConId, tradingClass, multiplier, expirations, strikes)), null);
    }

    public event Action<int> SecurityDefinitionOptionParameterEnd;

    void EWrapper.securityDefinitionOptionParameterEnd(int reqId)
    {
      var tmp = SecurityDefinitionOptionParameterEnd;

      if (tmp != null)
        Run(t => tmp(reqId), null);
    }

    public event Action<SoftDollarTiersMessage> SoftDollarTiers;

    void EWrapper.softDollarTiers(int reqId, SoftDollarTier[] tiers)
    {
      var tmp = SoftDollarTiers;

      if (tmp != null)
        Run(t => tmp(new SoftDollarTiersMessage(reqId, tiers)), null);
    }

    public event Action<FamilyCode[]> FamilyCodes;

    void EWrapper.familyCodes(FamilyCode[] familyCodes)
    {
      var tmp = FamilyCodes;

      if (tmp != null)
        Run(t => tmp(familyCodes), null);
    }

    public event Action<SymbolSamplesMessage> SymbolSamples;

    void EWrapper.symbolSamples(int reqId, ContractDescription[] contractDescriptions)
    {
      var tmp = SymbolSamples;

      if (tmp != null)
        Run(t => tmp(new SymbolSamplesMessage(reqId, contractDescriptions)), null);
    }


    public event Action<DepthMktDataDescription[]> MktDepthExchanges;

    void EWrapper.mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
    {
      var tmp = MktDepthExchanges;

      if (tmp != null)
        Run(t => tmp(depthMktDataDescriptions), null);
    }

    public event Action<TickNewsMessage> TickNews;

    void EWrapper.tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
    {
      var tmp = TickNews;

      if (tmp != null)
        Run(t => tmp(new TickNewsMessage(tickerId, timeStamp, providerCode, articleId, headline, extraData)), null);
    }

    public event Action<int, Dictionary<int, KeyValuePair<string, char>>> SmartComponents;

    void EWrapper.smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
    {
      var tmp = SmartComponents;

      if (tmp != null)
        Run(t => tmp(reqId, theMap), null);
    }

    public event Action<TickReqParamsMessage> TickReqParams;

    void EWrapper.tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
    {
      var tmp = TickReqParams;

      if (tmp != null)
        Run(t => tmp(new TickReqParamsMessage(tickerId, minTick, bboExchange, snapshotPermissions)), null);
    }

    public event Action<NewsProvider[]> NewsProviders;

    void EWrapper.newsProviders(NewsProvider[] newsProviders)
    {
      var tmp = NewsProviders;

      if (tmp != null)
        Run(t => tmp(newsProviders), null);
    }

    public event Action<NewsArticleMessage> NewsArticle;

    void EWrapper.newsArticle(int requestId, int articleType, string articleText)
    {
      var tmp = NewsArticle;

      if (tmp != null)
        Run(t => tmp(new NewsArticleMessage(requestId, articleType, articleText)), null);
    }

    public event Action<HistoricalNewsMessage> HistoricalNews;

    void EWrapper.historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
    {
      var tmp = HistoricalNews;

      if (tmp != null)
        Run(t => tmp(new HistoricalNewsMessage(requestId, time, providerCode, articleId, headline)), null);
    }

    public event Action<HistoricalNewsEndMessage> HistoricalNewsEnd;

    void EWrapper.historicalNewsEnd(int requestId, bool hasMore)
    {
      var tmp = HistoricalNewsEnd;

      if (tmp != null)
        Run(t => tmp(new HistoricalNewsEndMessage(requestId, hasMore)), null);
    }

    public event Action<HeadTimestampMessage> HeadTimestamp;

    void EWrapper.headTimestamp(int reqId, string headTimestamp)
    {
      var tmp = HeadTimestamp;

      if (tmp != null)
        Run(t => tmp(new HeadTimestampMessage(reqId, headTimestamp)), null);
    }

    public event Action<HistogramDataMessage> HistogramData;

    void EWrapper.histogramData(int reqId, HistogramEntry[] data)
    {
      var tmp = HistogramData;

      if (tmp != null)
        Run(t => tmp(new HistogramDataMessage(reqId, data)), null);
    }

    public event Action<HistoricalDataMessage> HistoricalDataUpdate;

    void EWrapper.historicalDataUpdate(int reqId, Bar bar)
    {
      var tmp = HistoricalDataUpdate;

      if (tmp != null)
        Run(t => tmp(new HistoricalDataMessage(reqId, bar)), null);
    }

    public event Action<int, int, string> RerouteMktDataReq;

    void EWrapper.rerouteMktDataReq(int reqId, int conId, string exchange)
    {
      var tmp = RerouteMktDataReq;

      if (tmp != null)
        Run(t => tmp(reqId, conId, exchange), null);
    }

    public event Action<int, int, string> RerouteMktDepthReq;

    void EWrapper.rerouteMktDepthReq(int reqId, int conId, string exchange)
    {
      var tmp = RerouteMktDepthReq;

      if (tmp != null)
        Run(t => tmp(reqId, conId, exchange), null);
    }

    public event Action<MarketRuleMessage> MarketRule;

    void EWrapper.marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
    {
      var tmp = MarketRule;

      if (tmp != null)
        Run(t => tmp(new MarketRuleMessage(marketRuleId, priceIncrements)), null);
    }

    public event Action<PnLMessage> pnl;

    void EWrapper.pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
    {
      var tmp = pnl;

      if (tmp != null)
        Run(t => tmp(new PnLMessage(reqId, dailyPnL, unrealizedPnL, realizedPnL)), null);
    }

    public event Action<PnLSingleMessage> pnlSingle;

    void EWrapper.pnlSingle(int reqId, decimal pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
    {
      var tmp = pnlSingle;

      if (tmp != null)
        Run(t => tmp(new PnLSingleMessage(reqId, pos, dailyPnL, unrealizedPnL, realizedPnL, value)), null);
    }

    public event Action<HistoricalTickMessage> historicalTick;
    public event Action<int> historicalTickEnd;

    void EWrapper.historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
    {
      var tmp = historicalTick;
      var tmpEnd = historicalTickEnd;

      if (tmp != null)
      {
        ticks.ToList().ForEach(tick => Run(t => tmp(new HistoricalTickMessage(reqId, tick.Time, tick.Price, tick.Size)), null));
        Run(o => tmpEnd(reqId), null);
      }
    }

    public event Action<HistoricalTickBidAskMessage> historicalTickBidAsk;

    void EWrapper.historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
    {
      var tmp = historicalTickBidAsk;
      var tmpEnd = historicalTickEnd;

      if (tmp != null)
      {
        ticks.ToList().ForEach(tick => Run(t =>
            tmp(new HistoricalTickBidAskMessage(reqId, tick.Time, tick.TickAttribBidAsk, tick.PriceBid, tick.PriceAsk, tick.SizeBid, tick.SizeAsk)), null));
        Run(o => tmpEnd(reqId), null);
      }
    }

    public event Action<HistoricalTickLastMessage> historicalTickLast;

    void EWrapper.historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
    {
      var tmp = historicalTickLast;
      var tmpEnd = historicalTickEnd;

      if (tmp != null)
      {
        ticks.ToList().ForEach(tick => Run(t =>
            tmp(new HistoricalTickLastMessage(reqId, tick.Time, tick.TickAttribLast, tick.Price, tick.Size, tick.Exchange, tick.SpecialConditions)), null));
        Run(o => tmpEnd(reqId), null);
      }
    }

    public event Action<TickByTickAllLastMessage> tickByTickAllLast;

    void EWrapper.tickByTickAllLast(int reqId, int tickType, long time, double price, decimal size, TickAttribLast tickAttribLast, string exchange, string specialConditions)
    {
      var tmp = tickByTickAllLast;

      if (tmp != null)
        Run(t => tmp(new TickByTickAllLastMessage(reqId, tickType, time, price, size, tickAttribLast, exchange, specialConditions)), null);
    }

    public event Action<TickByTickBidAskMessage> tickByTickBidAsk;

    void EWrapper.tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, decimal bidSize, decimal askSize, TickAttribBidAsk tickAttribBidAsk)
    {
      var tmp = tickByTickBidAsk;

      if (tmp != null)
        Run(t => tmp(new TickByTickBidAskMessage(reqId, time, bidPrice, askPrice, bidSize, askSize, tickAttribBidAsk)), null);
    }

    public event Action<TickByTickMidPointMessage> tickByTickMidPoint;

    void EWrapper.tickByTickMidPoint(int reqId, long time, double midPoint)
    {
      var tmp = tickByTickMidPoint;

      if (tmp != null)
        Run(t => tmp(new TickByTickMidPointMessage(reqId, time, midPoint)), null);
    }

    public event Action<OrderBoundMessage> OrderBound;

    void EWrapper.orderBound(long permId, int clientId, int orderId)
    {
      var tmp = OrderBound;

      if (tmp != null)
        Run(t => tmp(new OrderBoundMessage(permId, clientId, orderId)), null);
    }

    public event Action<CompletedOrderMessage> CompletedOrder;

    void EWrapper.completedOrder(Contract contract, Order order, OrderState orderState)
    {
      var tmp = CompletedOrder;

      if (tmp != null)
        Run(t => tmp(new CompletedOrderMessage(contract, order, orderState)), null);
    }

    public event Action CompletedOrdersEnd;

    void EWrapper.completedOrdersEnd()
    {
      var tmp = CompletedOrdersEnd;

      if (tmp != null)
        Run(t => tmp(), null);
    }

    public event Action<int, string> ReplaceFAEnd;

    void EWrapper.replaceFAEnd(int reqId, string text)
    {
      var tmp = ReplaceFAEnd;

      if (tmp != null)
        Run(t => tmp(reqId, text), null);
    }

    public event Action<int, string> WshMetaData;

    public void wshMetaData(int reqId, string dataJson)
    {
      var tmp = WshMetaData;

      if (tmp != null)
        Run(t => tmp(reqId, dataJson), null);
    }

    public event Action<int, string> WshEventData;

    public void wshEventData(int reqId, string dataJson)
    {
      var tmp = WshEventData;

      if (tmp != null)
        Run(t => tmp(reqId, dataJson), null);
    }

    public event Action<HistoricalScheduleMessage> HistoricalSchedule;

    public void historicalSchedule(int reqId, string startDateTime, string endDateTime, string timeZone, HistoricalSession[] sessions)
    {
      var tmp = HistoricalSchedule;

      if (tmp != null)
        Run(t => tmp(new HistoricalScheduleMessage(reqId, startDateTime, endDateTime, timeZone, sessions)), null);
    }

    public event Action<string> UserInfo;
    void EWrapper.userInfo(int reqId, string whiteBrandingId)
    {
      var tmp = UserInfo;
      if (tmp != null)
        Run(t => tmp(whiteBrandingId), null);
    }

    public event Action<long> CurrentTimeInMillis;
    void EWrapper.currentTimeInMillis(long timeInMillis)
    {
      var tmp = CurrentTimeInMillis;
      if (tmp != null)
        Run(t => tmp(timeInMillis), null);
    }

    /**
     * Protobuf
     */
    public event Action<IBApi.protobuf.OrderStatus> OrderStatusProtoBuf;
    void EWrapper.orderStatusProtoBuf(IBApi.protobuf.OrderStatus orderStatusProto)
    {
      var tmp = OrderStatusProtoBuf;
      if (tmp != null)
        Run(t => tmp(orderStatusProto), null);
    }

    public event Action<IBApi.protobuf.OpenOrder> OpenOrderProtoBuf;
    void EWrapper.openOrderProtoBuf(IBApi.protobuf.OpenOrder openOrderProto)
    {
      var tmp = OpenOrderProtoBuf;
      if (tmp != null)
        Run(t => tmp(openOrderProto), null);
    }

    public event Action<IBApi.protobuf.OpenOrdersEnd> OpenOrdersEndProtoBuf;
    void EWrapper.openOrdersEndProtoBuf(IBApi.protobuf.OpenOrdersEnd openOrdersEndProto)
    {
      var tmp = OpenOrdersEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(openOrdersEndProto), null);
    }

    public event Action<IBApi.protobuf.ErrorMessage> ErrorMessageProtoBuf;
    void EWrapper.errorProtoBuf(IBApi.protobuf.ErrorMessage errorMessageProto)
    {
      var tmp = ErrorMessageProtoBuf;
      if (tmp != null)
        Run(t => tmp(errorMessageProto), null);
    }

    public event Action<IBApi.protobuf.ExecutionDetails> ExecutionDetailsProtoBuf;
    void EWrapper.execDetailsProtoBuf(IBApi.protobuf.ExecutionDetails executionDetailsProto)
    {
      var tmp = ExecutionDetailsProtoBuf;
      if (tmp != null)
        Run(t => tmp(executionDetailsProto), null);
    }

    public event Action<IBApi.protobuf.ExecutionDetailsEnd> ExecutionDetailsEndProtoBuf;
    void EWrapper.execDetailsEndProtoBuf(IBApi.protobuf.ExecutionDetailsEnd executionDetailsEndProto)
    {
      var tmp = ExecutionDetailsEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(executionDetailsEndProto), null);
    }

    public event Action<IBApi.protobuf.CompletedOrder> CompletedOrderProtoBuf;
    void EWrapper.completedOrderProtoBuf(IBApi.protobuf.CompletedOrder completedOrderProto)
    {
      var tmp = CompletedOrderProtoBuf;
      if (tmp != null)
        Run(t => tmp(completedOrderProto), null);
    }

    public event Action<IBApi.protobuf.CompletedOrdersEnd> CompletedOrdersEndProtoBuf;
    void EWrapper.completedOrdersEndProtoBuf(IBApi.protobuf.CompletedOrdersEnd completedOrdersEndProto)
    {
      var tmp = CompletedOrdersEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(completedOrdersEndProto), null);
    }

    public event Action<IBApi.protobuf.OrderBound> OrderBoundProtoBuf;
    void EWrapper.orderBoundProtoBuf(IBApi.protobuf.OrderBound orderBoundProto)
    {
      var tmp = OrderBoundProtoBuf;
      if (tmp != null)
        Run(t => tmp(orderBoundProto), null);
    }

    public event Action<IBApi.protobuf.ContractData> ContractDataProtoBuf;
    void EWrapper.contractDataProtoBuf(IBApi.protobuf.ContractData contractDataProto)
    {
      var tmp = ContractDataProtoBuf;
      if (tmp != null)
        Run(t => tmp(contractDataProto), null);
    }

    public event Action<IBApi.protobuf.ContractData> BondContractDataProtoBuf;
    void EWrapper.bondContractDataProtoBuf(IBApi.protobuf.ContractData contractDataProto)
    {
      var tmp = BondContractDataProtoBuf;
      if (tmp != null)
        Run(t => tmp(contractDataProto), null);
    }

    public event Action<IBApi.protobuf.ContractDataEnd> ContractDataEndProtoBuf;
    void EWrapper.contractDataEndProtoBuf(IBApi.protobuf.ContractDataEnd contractDataEndProto)
    {
      var tmp = ContractDataEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(contractDataEndProto), null);
    }

    public event Action<IBApi.protobuf.TickPrice> TickPriceProtoBuf;
    void EWrapper.tickPriceProtoBuf(IBApi.protobuf.TickPrice tickPriceProto)
    {
      var tmp = TickPriceProtoBuf;
      if (tmp != null)
        Run(t => tmp(tickPriceProto), null);
    }

    public event Action<IBApi.protobuf.TickSize> TickSizeProtoBuf;
    void EWrapper.tickSizeProtoBuf(IBApi.protobuf.TickSize tickSizeProto)
    {
      var tmp = TickSizeProtoBuf;
      if (tmp != null)
        Run(t => tmp(tickSizeProto), null);
    }

    public event Action<IBApi.protobuf.TickOptionComputation> TickOptionComputationProtoBuf;
    void EWrapper.tickOptionComputationProtoBuf(IBApi.protobuf.TickOptionComputation tickOptionComputationProto)
    {
      var tmp = TickOptionComputationProtoBuf;
      if (tmp != null)
        Run(t => tmp(tickOptionComputationProto), null);
    }

    public event Action<IBApi.protobuf.TickGeneric> TickGenericProtoBuf;
    void EWrapper.tickGenericProtoBuf(IBApi.protobuf.TickGeneric tickGenericProto)
    {
      var tmp = TickGenericProtoBuf;
      if (tmp != null)
        Run(t => tmp(tickGenericProto), null);
    }

    public event Action<IBApi.protobuf.TickString> TickStringProtoBuf;
    void EWrapper.tickStringProtoBuf(IBApi.protobuf.TickString tickStringProto)
    {
      var tmp = TickStringProtoBuf;
      if (tmp != null)
        Run(t => tmp(tickStringProto), null);
    }

    public event Action<IBApi.protobuf.TickSnapshotEnd> TickSnapshotEndProtoBuf;
    void EWrapper.tickSnapshotEndProtoBuf(IBApi.protobuf.TickSnapshotEnd tickSnapshotEndProto)
    {
      var tmp = TickSnapshotEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(tickSnapshotEndProto), null);
    }

    public event Action<IBApi.protobuf.MarketDepth> UpdateMarketDepthProtoBuf;
    void EWrapper.updateMarketDepthProtoBuf(IBApi.protobuf.MarketDepth marketDepthProto)
    {
      var tmp = UpdateMarketDepthProtoBuf;
      if (tmp != null)
        Run(t => tmp(marketDepthProto), null);
    }

    public event Action<IBApi.protobuf.MarketDepthL2> UpdateMarketDepthL2ProtoBuf;
    void EWrapper.updateMarketDepthL2ProtoBuf(IBApi.protobuf.MarketDepthL2 marketDepthL2Proto)
    {
      var tmp = UpdateMarketDepthL2ProtoBuf;
      if (tmp != null)
        Run(t => tmp(marketDepthL2Proto), null);
    }

    public event Action<IBApi.protobuf.MarketDataType> MarketDataTypeProtoBuf;
    void EWrapper.marketDataTypeProtoBuf(IBApi.protobuf.MarketDataType marketDataTypeProto)
    {
      var tmp = MarketDataTypeProtoBuf;
      if (tmp != null)
        Run(t => tmp(marketDataTypeProto), null);
    }

    public event Action<IBApi.protobuf.TickReqParams> TickReqParamsProtoBuf;
    void EWrapper.tickReqParamsProtoBuf(IBApi.protobuf.TickReqParams tickReqParamsProto)
    {
      var tmp = TickReqParamsProtoBuf;
      if (tmp != null)
        Run(t => tmp(tickReqParamsProto), null);
    }

    public event Action<IBApi.protobuf.AccountValue> UpdateAccountValueProtoBuf;
    void EWrapper.updateAccountValueProtoBuf(IBApi.protobuf.AccountValue accountValueProto)
    {
      var tmp = UpdateAccountValueProtoBuf;
      if (tmp != null)
        Run(t => tmp(accountValueProto), null);
    }

    public event Action<IBApi.protobuf.PortfolioValue> UpdatePortfolioProtoBuf;
    void EWrapper.updatePortfolioProtoBuf(IBApi.protobuf.PortfolioValue portfolioValueProto)
    {
      var tmp = UpdatePortfolioProtoBuf;
      if (tmp != null)
        Run(t => tmp(portfolioValueProto), null);
    }

    public event Action<IBApi.protobuf.AccountUpdateTime> UpdateAccountTimeProtoBuf;
    void EWrapper.updateAccountTimeProtoBuf(IBApi.protobuf.AccountUpdateTime accountUpdateTimeProto)
    {
      var tmp = UpdateAccountTimeProtoBuf;
      if (tmp != null)
        Run(t => tmp(accountUpdateTimeProto), null);
    }

    public event Action<IBApi.protobuf.AccountDataEnd> AccountDataEndProtoBuf;
    void EWrapper.accountDataEndProtoBuf(IBApi.protobuf.AccountDataEnd accountDataEndProto)
    {
      var tmp = AccountDataEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(accountDataEndProto), null);
    }

    public event Action<IBApi.protobuf.ManagedAccounts> ManagedAccountsProtoBuf;
    void EWrapper.managedAccountsProtoBuf(IBApi.protobuf.ManagedAccounts managedAccountsProto)
    {
      var tmp = ManagedAccountsProtoBuf;
      if (tmp != null)
        Run(t => tmp(managedAccountsProto), null);
    }

    public event Action<IBApi.protobuf.Position> PositionProtoBuf;
    void EWrapper.positionProtoBuf(IBApi.protobuf.Position positionProto)
    {
      var tmp = PositionProtoBuf;
      if (tmp != null)
        Run(t => tmp(positionProto), null);
    }

    public event Action<IBApi.protobuf.PositionEnd> PositionEndProtoBuf;
    void EWrapper.positionEndProtoBuf(IBApi.protobuf.PositionEnd positionEndProto)
    {
      var tmp = PositionEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(positionEndProto), null);
    }

    public event Action<IBApi.protobuf.AccountSummary> AccountSummaryProtoBuf;
    void EWrapper.accountSummaryProtoBuf(IBApi.protobuf.AccountSummary accountSummaryProto)
    {
      var tmp = AccountSummaryProtoBuf;
      if (tmp != null)
        Run(t => tmp(accountSummaryProto), null);
    }

    public event Action<IBApi.protobuf.AccountSummaryEnd> AccountSummaryEndProtoBuf;
    void EWrapper.accountSummaryEndProtoBuf(IBApi.protobuf.AccountSummaryEnd accountSummaryEndProto)
    {
      var tmp = AccountSummaryEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(accountSummaryEndProto), null);
    }

    public event Action<IBApi.protobuf.PositionMulti> PositionMultiProtoBuf;
    void EWrapper.positionMultiProtoBuf(IBApi.protobuf.PositionMulti positionMultiProto)
    {
      var tmp = PositionMultiProtoBuf;
      if (tmp != null)
        Run(t => tmp(positionMultiProto), null);
    }

    public event Action<IBApi.protobuf.PositionMultiEnd> PositionMultiEndProtoBuf;
    void EWrapper.positionMultiEndProtoBuf(IBApi.protobuf.PositionMultiEnd positionMultiEndProto)
    {
      var tmp = PositionMultiEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(positionMultiEndProto), null);
    }

    public event Action<IBApi.protobuf.AccountUpdateMulti> AccountUpdateMultiProtoBuf;
    void EWrapper.accountUpdateMultiProtoBuf(IBApi.protobuf.AccountUpdateMulti accountUpdateMultiProto)
    {
      var tmp = AccountUpdateMultiProtoBuf;
      if (tmp != null)
        Run(t => tmp(accountUpdateMultiProto), null);
    }

    public event Action<IBApi.protobuf.AccountUpdateMultiEnd> AccountUpdateMultiEndProtoBuf;
    void EWrapper.accountUpdateMultiEndProtoBuf(IBApi.protobuf.AccountUpdateMultiEnd accountUpdateMultiEndProto)
    {
      var tmp = AccountUpdateMultiEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(accountUpdateMultiEndProto), null);
    }

    public event Action<IBApi.protobuf.HistoricalData> HistoricalDataProtoBuf;
    void EWrapper.historicalDataProtoBuf(IBApi.protobuf.HistoricalData historicalDataProto)
    {
      var tmp = HistoricalDataProtoBuf;
      if (tmp != null)
        Run(t => tmp(historicalDataProto), null);
    }

    public event Action<IBApi.protobuf.HistoricalDataUpdate> HistoricalDataUpdateProtoBuf;
    void EWrapper.historicalDataUpdateProtoBuf(IBApi.protobuf.HistoricalDataUpdate historicalDataUpdateProto)
    {
      var tmp = HistoricalDataUpdateProtoBuf;
      if (tmp != null)
        Run(t => tmp(historicalDataUpdateProto), null);
    }

    public event Action<IBApi.protobuf.HistoricalDataEnd> HistoricalDataEndProtoBuf;
    void EWrapper.historicalDataEndProtoBuf(IBApi.protobuf.HistoricalDataEnd historicalDataEndProto)
    {
      var tmp = HistoricalDataEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(historicalDataEndProto), null);
    }

    public event Action<IBApi.protobuf.RealTimeBarTick> RealTimeBarTickProtoBuf;
    void EWrapper.realTimeBarTickProtoBuf(IBApi.protobuf.RealTimeBarTick realTimeBarTickProto)
    {
      var tmp = RealTimeBarTickProtoBuf;
      if (tmp != null)
        Run(t => tmp(realTimeBarTickProto), null);
    }

    public event Action<IBApi.protobuf.HeadTimestamp> HeadTimestampProtoBuf;
    void EWrapper.headTimestampProtoBuf(IBApi.protobuf.HeadTimestamp headTimestampProto)
    {
      var tmp = HeadTimestampProtoBuf;
      if (tmp != null)
        Run(t => tmp(headTimestampProto), null);
    }

    public event Action<IBApi.protobuf.HistogramData> HistogramDataProtoBuf;
    void EWrapper.histogramDataProtoBuf(IBApi.protobuf.HistogramData histogramDataProto)
    {
      var tmp = HistogramDataProtoBuf;
      if (tmp != null)
        Run(t => tmp(histogramDataProto), null);
    }

    public event Action<IBApi.protobuf.HistoricalTicks> HistoricalTicksProtoBuf;
    void EWrapper.historicalTicksProtoBuf(IBApi.protobuf.HistoricalTicks historicalTicksProto)
    {
      var tmp = HistoricalTicksProtoBuf;
      if (tmp != null)
        Run(t => tmp(historicalTicksProto), null);
    }

    public event Action<IBApi.protobuf.HistoricalTicksBidAsk> HistoricalTicksBidAskProtoBuf;
    void EWrapper.historicalTicksBidAskProtoBuf(IBApi.protobuf.HistoricalTicksBidAsk historicalTicksBidAskProto)
    {
      var tmp = HistoricalTicksBidAskProtoBuf;
      if (tmp != null)
        Run(t => tmp(historicalTicksBidAskProto), null);
    }

    public event Action<IBApi.protobuf.HistoricalTicksLast> HistoricalTicksLastProtoBuf;
    void EWrapper.historicalTicksLastProtoBuf(IBApi.protobuf.HistoricalTicksLast historicalTicksLastProto)
    {
      var tmp = HistoricalTicksLastProtoBuf;
      if (tmp != null)
        Run(t => tmp(historicalTicksLastProto), null);
    }

    public event Action<IBApi.protobuf.TickByTickData> TickByTickDataProtoBuf;
    void EWrapper.tickByTickDataProtoBuf(IBApi.protobuf.TickByTickData tickByTickDataProto)
    {
      var tmp = TickByTickDataProtoBuf;
      if (tmp != null)
        Run(t => tmp(tickByTickDataProto), null);
    }

    public event Action<IBApi.protobuf.NewsBulletin> UpdateNewsBulletinProtoBuf;
    void EWrapper.updateNewsBulletinProtoBuf(IBApi.protobuf.NewsBulletin newsBulletinProto)
    {
      var tmp = UpdateNewsBulletinProtoBuf;
      if (tmp != null)
        Run(t => tmp(newsBulletinProto), null);
    }

    public event Action<IBApi.protobuf.NewsArticle> NewsArticleProtoBuf;
    void EWrapper.newsArticleProtoBuf(IBApi.protobuf.NewsArticle newsArticleProto)
    {
      var tmp = NewsArticleProtoBuf;
      if (tmp != null)
        Run(t => tmp(newsArticleProto), null);
    }

    public event Action<IBApi.protobuf.NewsProviders> NewsProvidersProtoBuf;
    void EWrapper.newsProvidersProtoBuf(IBApi.protobuf.NewsProviders newsProvidersProto)
    {
      var tmp = NewsProvidersProtoBuf;
      if (tmp != null)
        Run(t => tmp(newsProvidersProto), null);
    }

    public event Action<IBApi.protobuf.HistoricalNews> HistoricalNewsProtoBuf;
    void EWrapper.historicalNewsProtoBuf(IBApi.protobuf.HistoricalNews historicalNewsProto)
    {
      var tmp = HistoricalNewsProtoBuf;
      if (tmp != null)
        Run(t => tmp(historicalNewsProto), null);
    }

    public event Action<IBApi.protobuf.HistoricalNewsEnd> HistoricalNewsEndProtoBuf;
    void EWrapper.historicalNewsEndProtoBuf(IBApi.protobuf.HistoricalNewsEnd historicalNewsEndProto)
    {
      var tmp = HistoricalNewsEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(historicalNewsEndProto), null);
    }

    public event Action<IBApi.protobuf.WshMetaData> WshMetaDataProtoBuf;
    void EWrapper.wshMetaDataProtoBuf(IBApi.protobuf.WshMetaData wshMetaDataProto)
    {
      var tmp = WshMetaDataProtoBuf;
      if (tmp != null)
        Run(t => tmp(wshMetaDataProto), null);
    }

    public event Action<IBApi.protobuf.WshEventData> WshEventDataProtoBuf;
    void EWrapper.wshEventDataProtoBuf(IBApi.protobuf.WshEventData wshEventDataProto)
    {
      var tmp = WshEventDataProtoBuf;
      if (tmp != null)
        Run(t => tmp(wshEventDataProto), null);
    }

    public event Action<IBApi.protobuf.TickNews> TickNewsProtoBuf;
    void EWrapper.tickNewsProtoBuf(IBApi.protobuf.TickNews tickNewsProto)
    {
      var tmp = TickNewsProtoBuf;
      if (tmp != null)
        Run(t => tmp(tickNewsProto), null);
    }

    public event Action<IBApi.protobuf.ScannerParameters> ScannerParametersProtoBuf;
    void EWrapper.scannerParametersProtoBuf(IBApi.protobuf.ScannerParameters scannerParametersProto)
    {
      var tmp = ScannerParametersProtoBuf;
      if (tmp != null)
        Run(t => tmp(scannerParametersProto), null);
    }

    public event Action<IBApi.protobuf.ScannerData> ScannerDataProtoBuf;
    void EWrapper.scannerDataProtoBuf(IBApi.protobuf.ScannerData scannerDataProto)
    {
      var tmp = ScannerDataProtoBuf;
      if (tmp != null)
        Run(t => tmp(scannerDataProto), null);
    }

    public event Action<IBApi.protobuf.FundamentalsData> FundamentalsDataProtoBuf;
    void EWrapper.fundamentalsDataProtoBuf(IBApi.protobuf.FundamentalsData fundamentalsDataProto)
    {
      var tmp = FundamentalsDataProtoBuf;
      if (tmp != null)
        Run(t => tmp(fundamentalsDataProto), null);
    }

    public event Action<IBApi.protobuf.PnL> PnLProtoBuf;
    void EWrapper.pnlProtoBuf(IBApi.protobuf.PnL pnlProto)
    {
      var tmp = PnLProtoBuf;
      if (tmp != null)
        Run(t => tmp(pnlProto), null);
    }

    public event Action<IBApi.protobuf.PnLSingle> PnLSingleProtoBuf;
    void EWrapper.pnlSingleProtoBuf(IBApi.protobuf.PnLSingle pnlSingleProto)
    {
      var tmp = PnLSingleProtoBuf;
      if (tmp != null)
        Run(t => tmp(pnlSingleProto), null);
    }

    public event Action<IBApi.protobuf.ReceiveFA> ReceiveFAProtoBuf;
    void EWrapper.receiveFAProtoBuf(IBApi.protobuf.ReceiveFA receiveFAProto)
    {
      var tmp = ReceiveFAProtoBuf;
      if (tmp != null)
        Run(t => tmp(receiveFAProto), null);
    }

    public event Action<IBApi.protobuf.ReplaceFAEnd> ReplaceFAEndProtoBuf;
    void EWrapper.replaceFAEndProtoBuf(IBApi.protobuf.ReplaceFAEnd replaceFAEndProto)
    {
      var tmp = ReplaceFAEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(replaceFAEndProto), null);
    }

    public event Action<IBApi.protobuf.CommissionAndFeesReport> CommissionAndFeesReportProtoBuf;
    void EWrapper.commissionAndFeesReportProtoBuf(IBApi.protobuf.CommissionAndFeesReport commissionAndFeesReportProto)
    {
      var tmp = CommissionAndFeesReportProtoBuf;
      if (tmp != null)
        Run(t => tmp(commissionAndFeesReportProto), null);
    }

    public event Action<IBApi.protobuf.HistoricalSchedule> HistoricalScheduleProtoBuf;
    void EWrapper.historicalScheduleProtoBuf(IBApi.protobuf.HistoricalSchedule historicalScheduleProto)
    {
      var tmp = HistoricalScheduleProtoBuf;
      if (tmp != null)
        Run(t => tmp(historicalScheduleProto), null);
    }

    public event Action<IBApi.protobuf.RerouteMarketDataRequest> RerouteMarketDataRequestProtoBuf;
    void EWrapper.rerouteMarketDataRequestProtoBuf(IBApi.protobuf.RerouteMarketDataRequest rerouteMarketDataRequestProto)
    {
      var tmp = RerouteMarketDataRequestProtoBuf;
      if (tmp != null)
        Run(t => tmp(rerouteMarketDataRequestProto), null);
    }

    public event Action<IBApi.protobuf.RerouteMarketDepthRequest> RerouteMarketDepthRequestProtoBuf;
    void EWrapper.rerouteMarketDepthRequestProtoBuf(IBApi.protobuf.RerouteMarketDepthRequest rerouteMarketDepthRequestProto)
    {
      var tmp = RerouteMarketDepthRequestProtoBuf;
      if (tmp != null)
        Run(t => tmp(rerouteMarketDepthRequestProto), null);
    }

    public event Action<IBApi.protobuf.SecDefOptParameter> SecDefOptParameterProtoBuf;
    void EWrapper.secDefOptParameterProtoBuf(IBApi.protobuf.SecDefOptParameter secDefOptParameterProto)
    {
      var tmp = SecDefOptParameterProtoBuf;
      if (tmp != null)
        Run(t => tmp(secDefOptParameterProto), null);
    }

    public event Action<IBApi.protobuf.SecDefOptParameterEnd> SecDefOptParameterEndProtoBuf;
    void EWrapper.secDefOptParameterEndProtoBuf(IBApi.protobuf.SecDefOptParameterEnd secDefOptParameterEndProto)
    {
      var tmp = SecDefOptParameterEndProtoBuf;
      if (tmp != null)
        Run(t => tmp(secDefOptParameterEndProto), null);
    }

    public event Action<IBApi.protobuf.SoftDollarTiers> SoftDollarTiersProtoBuf;
    void EWrapper.softDollarTiersProtoBuf(IBApi.protobuf.SoftDollarTiers softDollarTiersProto)
    {
      var tmp = SoftDollarTiersProtoBuf;
      if (tmp != null)
        Run(t => tmp(softDollarTiersProto), null);
    }

    public event Action<IBApi.protobuf.FamilyCodes> FamilyCodesProtoBuf;
    void EWrapper.familyCodesProtoBuf(IBApi.protobuf.FamilyCodes familyCodesProto)
    {
      var tmp = FamilyCodesProtoBuf;
      if (tmp != null)
        Run(t => tmp(familyCodesProto), null);
    }

    public event Action<IBApi.protobuf.SymbolSamples> SymbolSamplesProtoBuf;
    void EWrapper.symbolSamplesProtoBuf(IBApi.protobuf.SymbolSamples symbolSamplesProto)
    {
      var tmp = SymbolSamplesProtoBuf;
      if (tmp != null)
        Run(t => tmp(symbolSamplesProto), null);
    }

    public event Action<IBApi.protobuf.SmartComponents> SmartComponentsProtoBuf;
    void EWrapper.smartComponentsProtoBuf(IBApi.protobuf.SmartComponents smartComponentsProto)
    {
      var tmp = SmartComponentsProtoBuf;
      if (tmp != null)
        Run(t => tmp(smartComponentsProto), null);
    }

    public event Action<IBApi.protobuf.MarketRule> MarketRuleProtoBuf;
    void EWrapper.marketRuleProtoBuf(IBApi.protobuf.MarketRule marketRuleProto)
    {
      var tmp = MarketRuleProtoBuf;
      if (tmp != null)
        Run(t => tmp(marketRuleProto), null);
    }

    public event Action<IBApi.protobuf.UserInfo> UserInfoProtoBuf;
    void EWrapper.userInfoProtoBuf(IBApi.protobuf.UserInfo userInfoProto)
    {
      var tmp = UserInfoProtoBuf;
      if (tmp != null)
        Run(t => tmp(userInfoProto), null);
    }

    public event Action<IBApi.protobuf.NextValidId> NextValidIdProtoBuf;
    void EWrapper.nextValidIdProtoBuf(IBApi.protobuf.NextValidId nextValidIdProto)
    {
      var tmp = NextValidIdProtoBuf;
      if (tmp != null)
        Run(t => tmp(nextValidIdProto), null);
    }

    public event Action<IBApi.protobuf.CurrentTime> CurrentTimeProtoBuf;
    void EWrapper.currentTimeProtoBuf(IBApi.protobuf.CurrentTime currentTimeProto)
    {
      var tmp = CurrentTimeProtoBuf;
      if (tmp != null)
        Run(t => tmp(currentTimeProto), null);
    }

    public event Action<IBApi.protobuf.CurrentTimeInMillis> CurrentTimeInMillisProtoBuf;
    void EWrapper.currentTimeInMillisProtoBuf(IBApi.protobuf.CurrentTimeInMillis currentTimeInMillisProto)
    {
      var tmp = CurrentTimeInMillisProtoBuf;
      if (tmp != null)
        Run(t => tmp(currentTimeInMillisProto), null);
    }

    public event Action<IBApi.protobuf.VerifyMessageApi> VerifyMessageApiProtoBuf;
    void EWrapper.verifyMessageApiProtoBuf(IBApi.protobuf.VerifyMessageApi verifyMessageApiProto)
    {
      var tmp = VerifyMessageApiProtoBuf;
      if (tmp != null)
        Run(t => tmp(verifyMessageApiProto), null);
    }

    public event Action<IBApi.protobuf.VerifyCompleted> VerifyCompletedProtoBuf;
    void EWrapper.verifyCompletedProtoBuf(IBApi.protobuf.VerifyCompleted verifyCompletedProto)
    {
      var tmp = VerifyCompletedProtoBuf;
      if (tmp != null)
        Run(t => tmp(verifyCompletedProto), null);
    }

    public event Action<IBApi.protobuf.DisplayGroupList> DisplayGroupListProtoBuf;
    void EWrapper.displayGroupListProtoBuf(IBApi.protobuf.DisplayGroupList displayGroupListProto)
    {
      var tmp = DisplayGroupListProtoBuf;
      if (tmp != null)
        Run(t => tmp(displayGroupListProto), null);
    }

    public event Action<IBApi.protobuf.DisplayGroupUpdated> DisplayGroupUpdatedProtoBuf;
    void EWrapper.displayGroupUpdatedProtoBuf(IBApi.protobuf.DisplayGroupUpdated displayGroupUpdatedProto)
    {
      var tmp = DisplayGroupUpdatedProtoBuf;
      if (tmp != null)
        Run(t => tmp(displayGroupUpdatedProto), null);
    }

    public event Action<IBApi.protobuf.MarketDepthExchanges> MarketDepthExchangesProtoBuf;
    void EWrapper.marketDepthExchangesProtoBuf(IBApi.protobuf.MarketDepthExchanges marketDepthExchangesProto)
    {
      var tmp = MarketDepthExchangesProtoBuf;
      if (tmp != null)
        Run(t => tmp(marketDepthExchangesProto), null);
    }

    // Custom implementation

    private object sync = new object();

    protected void Run(Action<object> cb, object state)
    {
      lock (sync)
      {
        cb(null);
      }
    }
  }
}
