using System;

namespace InteractiveBrokers.Messages
{
  public class PriceMessage
  {
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Bid
    /// </summary>
    public double? Bid { get; set; }

    /// <summary>
    /// Ask
    /// </summary>
    public double? Ask { get; set; }

    /// <summary>
    /// Volume of the bid 
    /// </summary>
    public double? BidSize { get; set; }

    /// <summary>
    /// Volume of the ask
    /// </summary>
    public double? AskSize { get; set; }

    /// <summary>
    /// Last price or value
    /// </summary>
    public double? Last { get; set; }

    /// <summary>
    /// Instrument volume
    /// </summary>
    public double? Volume { get; set; }

    /// <summary>
    /// Time stamp
    /// </summary>
    public long? Time { get; set; }

    /// <summary>
    /// Aggregation period for the quotes
    /// </summary>
    public TimeSpan? TimeFrame { get; set; }

    /// <summary>
    /// Reference to the complex data point
    /// </summary>
    public BarMessage Bar { get; set; }
  }
}
