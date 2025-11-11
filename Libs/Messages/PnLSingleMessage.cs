/* Copyright (C) 2019 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

namespace IBApi.Messages
{
  public class PnLSingleMessage
  {
    public int ReqId { get; set; }
    public decimal Pos { get; set; }
    public double DailyPnL { get; set; }
    public double Value { get; set; }
    public double UnrealizedPnL { get; set; }
    public double RealizedPnL { get; set; }

    public PnLSingleMessage() { }

    public PnLSingleMessage(int reqId, decimal pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
    {
      ReqId = reqId;
      Pos = pos;
      DailyPnL = dailyPnL;
      Value = value;
      UnrealizedPnL = unrealizedPnL;
      RealizedPnL = realizedPnL;
    }
  }
}
