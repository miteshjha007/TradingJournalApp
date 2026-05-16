//+------------------------------------------------------------------+
//|                                             TradingJournalEA.mq5 |
//|                        Trading Journal MT5 Auto-Import EA        |
//+------------------------------------------------------------------+
#property copyright "Trading Journal"
#property version   "1.10"
#property description "Auto-import trades to Trading Journal on close"
#property strict

// --- Input Parameters ---
input string WebhookUrl = "https://trading-journal-api-mcc2.onrender.com/api/import/mt5-webhook";
input string WebhookToken = "PASTE_YOUR_TOKEN_HERE";
input int MagicFilter = 0;
input bool EnableLogging = true;

// --- Global Variables ---
datetime lastCheckTime;

//+------------------------------------------------------------------+
//| Expert initialization function                                    |
//+------------------------------------------------------------------+
int OnInit()
{
   lastCheckTime = TimeCurrent();
   if(EnableLogging) Print("Trading Journal EA Initialized");
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Main Trade Event                                                 |
//+------------------------------------------------------------------+
void OnTrade()
{
   // Check history for the last 1 minute
   datetime startTime = TimeCurrent() - 60;
   if(!HistorySelect(startTime, TimeCurrent())) return;

   int total = HistoryDealsTotal();
   for(int i = total - 1; i >= 0; i--)
   {
      ulong ticket = HistoryDealGetTicket(i);
      if(ticket <= 0) continue;

      // Only process deals that close a position (OUT)
      if(HistoryDealGetInteger(ticket, DEAL_ENTRY) != DEAL_ENTRY_OUT) continue;
      
      // Apply magic filter
      long magic = HistoryDealGetInteger(ticket, DEAL_MAGIC);
      if(MagicFilter > 0 && magic != MagicFilter) continue;

      // Check if this is a new deal
      datetime dealTime = (datetime)HistoryDealGetInteger(ticket, DEAL_TIME);
      if(dealTime > lastCheckTime)
      {
         ProcessDeal(ticket);
         lastCheckTime = dealTime;
      }
   }
}

//+------------------------------------------------------------------+
//| Process and Send Deal Data                                       |
//+------------------------------------------------------------------+
void ProcessDeal(ulong ticket)
{
   string symbol = HistoryDealGetString(ticket, DEAL_SYMBOL);
   double volume = HistoryDealGetDouble(ticket, DEAL_VOLUME);
   double priceClose = HistoryDealGetDouble(ticket, DEAL_PRICE);
   double profit = HistoryDealGetDouble(ticket, DEAL_PROFIT);
   datetime timeClose = (datetime)HistoryDealGetInteger(ticket, DEAL_TIME);
   string comment = HistoryDealGetString(ticket, DEAL_COMMENT);
   long magic = HistoryDealGetInteger(ticket, DEAL_MAGIC);
   ENUM_DEAL_TYPE type = (ENUM_DEAL_TYPE)HistoryDealGetInteger(ticket, DEAL_TYPE);

   // Detect Buy/Sell
   string orderType = (type == DEAL_TYPE_SELL) ? "buy" : "sell"; 

   string json = "{";
   json += "\"symbol\":\"" + symbol + "\",";
   json += "\"orderType\":\"" + orderType + "\",";
   json += "\"lots\":" + DoubleToString(volume, 2) + ",";
   json += "\"closePrice\":" + DoubleToString(priceClose, 5) + ",";
   json += "\"profit\":" + DoubleToString(profit, 2) + ",";
   json += "\"closeTime\":\"" + TimeToString(timeClose, TIME_DATE|TIME_SECONDS) + "\",";
   json += "\"comment\":\"" + comment + "\",";
   json += "\"ticketNumber\":" + IntegerToString(ticket) + ",";
   json += "\"magicNumber\":\"" + IntegerToString(magic) + "\"";
   json += "}";

   SendRequest(json);
}

//+------------------------------------------------------------------+
//| HTTP Request to Webhook                                          |
//+------------------------------------------------------------------+
void SendRequest(string json)
{
   char postData[], result[];
   string resultHeaders;
   string headers = "Content-Type: application/json\r\nX-Webhook-Token: " + WebhookToken + "\r\n";
   
   StringToCharArray(json, postData, 0, StringLen(json));
   int res = WebRequest("POST", WebhookUrl, headers, 5000, postData, result, resultHeaders);

   if(res == 200 && EnableLogging) Print("Trade synced to Journal!");
   else if(EnableLogging) Print("Error syncing trade: ", res);
}