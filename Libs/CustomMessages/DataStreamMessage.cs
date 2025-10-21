using IBApi.Enums;
using System.Collections.Generic;

namespace IBApi.Messages
{
  public class DataStreamMessage
  {
    public Contract Contract { get; set; }
    public bool Snapshot { get; set; } = false;
    public bool RegSnapshot { get; set; } = false;
    public List<SubscriptionEnum> DataTypes { get; set; }
    public List<TagValue> Tags { get; set; } = new List<TagValue>();
  }
}
