/* Copyright (C) 2024 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

namespace IBApi
{
  public class ErrorMessage
  {
    public ErrorMessage(int requestId, long errorTime, int errorCode, string message, string advancedOrderRejectJson)
    {
      AdvancedOrderRejectJson = advancedOrderRejectJson;
      Message = message;
      RequestId = requestId;
      ErrorTime = errorTime;
      ErrorCode = errorCode;
    }

    public string AdvancedOrderRejectJson { get; set; }

    public string Message { get; set; }

    public long ErrorTime { get; set; }

    public int ErrorCode { get; set; }

    public int RequestId { get; set; }

    public override string ToString()
    {
      string errorTimeStr = Util.UnixMilliSecondsToString(ErrorTime, "yyyyMMdd-HH:mm:ss");
      string ret = "Error. Request: " + RequestId + ", Time: " + errorTimeStr + ", Code: " + ErrorCode + " - " + Message;
      if (!Util.StringIsEmpty(AdvancedOrderRejectJson))
      {
        ret += (", AdvancedOrderRejectJson: " + AdvancedOrderRejectJson);
      }
      return ret;
    }

  }
}
