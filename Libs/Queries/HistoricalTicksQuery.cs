using System;

namespace IBApi.Queries
{
  public class HistoricalTicksQuery
  {
    public Contract Contract { get; set; }
    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }
    public string DataType { get; set; }
    public int Count { get; set; } = 1;
    public int Session { get; set; } = 0;
  }
}
