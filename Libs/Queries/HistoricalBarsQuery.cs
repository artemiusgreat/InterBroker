using System;

namespace IBApi.Queries
{
  public class HistoricalBarsQuery
  {
    public Contract Contract { get; set; }
    public DateTime MaxDate { get; set; }
    public string Duration { get; set; }
    public string BarType { get; set; }
    public string DataType { get; set; }
    public int Session { get; set; } = 0;
  }
}
