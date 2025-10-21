/* Copyright (C) 2019 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

using System;

namespace IBApi.Messages
{
  public class ComputationMessage : ICloneable
  {
    public int? TickAttrib { get; set; }

    public double? ImpliedVolatility { get; set; }

    public double? Delta { get; set; }

    public double? OptPrice { get; set; }

    public double? PvDividend { get; set; }

    public double? Gamma { get; set; }

    public double? Vega { get; set; }

    public double? Theta { get; set; }

    public double? UndPrice { get; set; }

    public object Clone()
    {
      return MemberwiseClone();
    }
  }
}
