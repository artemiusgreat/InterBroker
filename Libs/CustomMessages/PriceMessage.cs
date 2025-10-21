using System;

namespace IBApi.Messages
{
  public class PriceMessage : ICloneable
  {
    public double? Bid { get; set; }
    public double? Ask { get; set; }
    public double? BidSize { get; set; }
    public double? AskSize { get; set; }
    public double? Last { get; set; }
    public long? Time { get; set; }

    public object Clone()
    {
      return MemberwiseClone();
    }
  }
}
