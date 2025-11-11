using IBApi.Enums;
using System.Collections.Generic;

namespace IBApi.Messages
{
  public class PriceStreamMessage
  {
    public bool Snapshot { get; set; } = false;
    public bool RegSnapshot { get; set; } = false;
    public string Account { get; set; }
    public Contract Contract { get; set; }
    public List<SubscriptionEnum> DataTypes { get; set; }
    public List<TagValue> Tags { get; set; } = new List<TagValue>();
  }
}
