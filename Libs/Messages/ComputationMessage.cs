namespace InteractiveBrokers.Messages
{
  public class ComputationMessage
  {
    public double? IV { get; set; }

    public double? Delta { get; set; }

    public double? Gamma { get; set; }

    public double? Vega { get; set; }

    public double? Theta { get; set; }

    public double? OptPrice { get; set; }

    public double? UndPrice { get; set; }
  }
}
