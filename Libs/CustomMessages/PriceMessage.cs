/* Copyright (C) 2019 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

namespace IBApi.Messages
{
  public class PriceMessage
  {
    public double? Bid { get; set; }
    public double? Ask { get; set; }
    public double? BidSize { get; set; }
    public double? AskSize { get; set; }
    public double? Last { get; set; }
    public long? Time { get; set; }
  }
}
