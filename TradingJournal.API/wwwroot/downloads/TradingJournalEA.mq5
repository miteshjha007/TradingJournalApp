//+------------------------------------------------------------------+
//|                                             TradingJournalEA.mq5 |
//|                        Trading Journal MT5 Auto-Import EA        |
//|                                                                  |
//| This EA automatically sends closed trade information to your    |
//| Trading Journal when positions are closed in MetaTrader 5.      |
//+------------------------------------------------------------------+

#property copyright "Trading Journal"
#property version   "1.00"
#property description "Auto-import trades to Trading Journal on close"
#property strict

// --- Input Parameters ---
input string WebhookUrl = "https://trading-journal-api-mcc2.onrender.com/api/import/mt5-webhook";
input string WebhookToken = "PASTE_YOUR_TOKEN_HERE";
input int MagicFilter = 0;  // 0 = all trades, otherwise only trades with this magic number
input bool EnableLogging = true;  // Enable journal logging
input int MaxRetryAttempts = 3;  // Maximum retry attempts on failure
input int RetryDelayMs = 1000;  // Delay between retries in milliseconds

// --- Global Variables ---
datetime lastProcessedTime = 0;
string lastTicketHash = "";

//+------------------------------------------------------------------+
//| Expert initialization function                                    |
//+------------------------------------------------------------------+
int OnInit()
{
   if(EnableLogging)
   {
      Print("=== TradingJournalEA Started ===");
      Print("Webhook URL: ", WebhookUrl);
      Print("Token: ", StringSubstr(WebhookToken, 0, 8), "...");
      Print("Magic Filter: ", MagicFilter);
   }

   if(WebhookToken == "PASTE_YOUR_TOKEN_HERE" || WebhookToken == "")
   {
      Print("ERROR: Please configure your WebhookToken in the EA settings!");
      return INIT_PARAMETERS_INCORRECT;
   }

   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   if(EnableLogging)
   {
      Print("=== TradingJournalEA Stopped ===");
   }
}

//+------------------------------------------------------------------+
//| Trade transaction handler                                         |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction& transaction,
                         const MqlTradeRequest& request,
                         const MqlTradeResult& result)
{
   // Only process closed deals
   if(transaction.type != TRADE_TRANSACTION_DEAL)
      return;

   // Only process DEAL_ENTRY_OUT (position closed)
   if(transaction.entry != DEAL_ENTRY_OUT)
      return;

   // Apply magic filter if configured
   if(MagicFilter > 0 && transaction.magic != MagicFilter)
      return;

   // Get deal ticket
   ulong ticket = transaction.deal;
   if(ticket == 0)
      return;

   // Prevent duplicate processing
   string ticketHash = IntegerToString(ticket) + IntegerToString(transaction.time);
   if(ticketHash == lastTicketHash)
      return;

   lastTicketHash = ticketHash;

   // Get deal details
   if(!ProcessClosedDeal(ticket))
   {
      if(EnableLogging)
      {
         Print("Failed to process deal ticket: ", ticket);
      }
   }
}

//+------------------------------------------------------------------+
//| Process closed deal                                               |
//+------------------------------------------------------------------+
bool ProcessClosedDeal(ulong ticket)
{
   // Request deal history
   if(!HistorySelectByPosition(0))
   {
      if(EnableLogging)
         Print("Failed to select history");
      return false;
   }

   // Find the deal
   ulong dealTicket = 0;
   string symbol = "";
   ENUM_DEAL_TYPE dealType = DEAL_TYPE_UNKNOWN;
   double volume = 0;
   double priceOpen = 0;
   double priceClose = 0;
   double profit = 0;
   double stopLoss = 0;
   double takeProfit = 0;
   datetime timeOpen = 0;
   datetime timeClose = 0;
   string comment = "";

   // Search through history for our deal
   for(int i = HistoryDealsTotal() - 1; i >= 0; i--)
   {
      ulong deal = HistoryDealGetTicket(i);
      if(deal == 0)
         continue;

      if(HistoryDealGetInteger(deal, DEAL_TICKET) == ticket)
      {
         dealTicket = ticket;
         symbol = HistoryDealGetString(deal, DEAL_SYMBOL);
         dealType = (ENUM_DEAL_TYPE)HistoryDealGetInteger(deal, DEAL_TYPE);
         volume = HistoryDealGetDouble(deal, DEAL_VOLUME);
         priceOpen = HistoryDealGetDouble(deal, DEAL_PRICE_OPEN);
         priceClose = HistoryDealGetDouble(deal, DEAL_PRICE_CLOSE);
         profit = HistoryDealGetDouble(deal, DEAL_PROFIT);
         timeOpen = (datetime)HistoryDealGetInteger(deal, DEAL_TIME);
         timeClose = (datetime)HistoryDealGetInteger(deal, DEAL_TIME_UPDATE);
         comment = HistoryDealGetString(deal, DEAL_COMMENT);

         // Get SL/TP from position if available
         ulong positionId = HistoryDealGetInteger(deal, DEAL_POSITION_ID);
         if(positionId > 0)
         {
            if(!PositionSelectByTicket(positionId))
            {
               // Try by symbol as fallback
               if(PositionSelect(symbol))
               {
                  stopLoss = PositionGetDouble(POSITION_SL);
                  takeProfit = PositionGetDouble(POSITION_TP);
               }
            }
            else
            {
               stopLoss = PositionGetDouble(POSITION_SL);
               takeProfit = PositionGetDouble(POSITION_TP);
            }
         }
         break;
      }
   }

   if(dealTicket == 0 || symbol == "")
   {
      if(EnableLogging)
         Print("Deal not found in history: ", ticket);
      return false;
   }

   // Determine order type
   string orderType = "buy";
   if(dealType == DEAL_TYPE_SELL)
      orderType = "sell";
   else if(dealType == DEAL_TYPE_BUY)
      orderType = "buy";

   // Format datetime as ISO 8601
   string openTimeStr = TimeToString(timeOpen, TIME_DATE|TIME_MINUTES|TIME_SECONDS);
   string closeTimeStr = TimeToString(timeClose, TIME_DATE|TIME_MINUTES|TIME_SECONDS);

   // Replace space with T for ISO format
   StringReplace(openTimeStr, " ", "T");
   StringReplace(closeTimeStr, " ", "T");

   // Get magic number
   long magic = 0;
   for(int i = HistoryDealsTotal() - 1; i >= 0; i--)
   {
      ulong deal = HistoryDealGetTicket(i);
      if(deal != 0 && HistoryDealGetInteger(deal, DEAL_TICKET) == ticket)
      {
         magic = HistoryDealGetInteger(deal, DEAL_MAGIC);
         break;
      }
   }

   // Build JSON payload
   string json = "{";
   json += "\"symbol\":\"" + symbol + "\",";
   json += "\"orderType\":\"" + orderType + "\",";
   json += "\"lots\":" + DoubleToString(volume, 2) + ",";
   json += "\"openPrice\":" + DoubleToString(priceOpen, 5) + ",";
   json += "\"closePrice\":" + DoubleToString(priceClose, 5) + ",";
   json += "\"stopLoss\":" + DoubleToString(stopLoss, 5) + ",";
   json += "\"takeProfit\":" + DoubleToString(takeProfit, 5) + ",";
   json += "\"profit\":" + DoubleToString(profit, 2) + ",";
   json += "\"openTime\":\"" + openTimeStr + "\",";
   json += "\"closeTime\":\"" + closeTimeStr + "\",";
   json += "\"comment\":\"" + comment + "\",";
   json += "\"ticketNumber\":" + IntegerToString(ticket) + ",";
   json += "\"magicNumber\":\"" + IntegerToString(magic) + "\"";
   json += "}";

   if(EnableLogging)
   {
      Print("Sending trade to Trading Journal: ", symbol, " ", orderType, " ", volume, " lots");
      Print("JSON: ", json);
   }

   // Send to webhook with retry logic
   return SendToWebhook(json, 1);
}

//+------------------------------------------------------------------+
//| Send JSON to webhook with retry logic                            |
//+------------------------------------------------------------------+
bool SendToWebhook(string json, int attempt)
{
   string headers = "Content-Type: application/json\r\nX-Webhook-Token: " + WebhookToken + "\r\n";
   char postData[];
   char result[];

   StringToCharArray(json, postData, 0, StringLen(json));

   int res = WebRequest("POST", WebhookUrl, headers, 5000, postData, result, "");

   if(res == 200)
   {
      string response = CharArrayToString(result);
      if(EnableLogging)
      {
         Print("Trade sent successfully! Response: ", response);
      }
      return true;
   }
   else
   {
      if(attempt < MaxRetryAttempts)
      {
         if(EnableLogging)
         {
            Print("Webhook failed (attempt ", attempt, "/", MaxRetryAttempts, "). Retrying in ", RetryDelayMs, "ms...");
         }
         Sleep(RetryDelayMs);
         return SendToWebhook(json, attempt + 1);
      }
      else
      {
         if(EnableLogging)
         {
            Print("ERROR: Webhook failed after ", MaxRetryAttempts, " attempts. HTTP Code: ", res);
            Print("Response: ", CharArrayToString(result));
         }
         return false;
      }
   }
}

//+------------------------------------------------------------------+
//| Alternative: OnTrade handler for simpler detection               |
//+------------------------------------------------------------------+
/*
This is an alternative event handler. If OnTradeTransaction doesn't work
as expected in your MT5 build, you can use OnTrade instead.

To use this, comment out OnTradeTransaction above and uncomment below:
*/
/*
void OnTrade()
{
   static datetime lastTradeTime = 0;
   datetime currentTime = TimeCurrent();

   // Rate limiting - only check once per second
   if(currentTime <= lastTradeTime)
      return;

   lastTradeTime = currentTime;

   // Check all closed positions
   for(int i = 0; i < PositionsTotal(); i++)
   {
      // This is a simplified approach - for production, use position IDs
   }
}
*/

//+------------------------------------------------------------------+
//| Helper function to get position SL/TP                           |
//+------------------------------------------------------------------+
double GetPositionSL(string symbol)
{
   if(PositionSelect(symbol))
   {
      return PositionGetDouble(POSITION_SL);
   }
   return 0;
}

double GetPositionTP(string symbol)
{
   if(PositionSelect(symbol))
   {
      return PositionGetDouble(POSITION_TP);
   }
   return 0;
}

//+------------------------------------------------------------------+
//| End of EA                                                        |
//+------------------------------------------------------------------+