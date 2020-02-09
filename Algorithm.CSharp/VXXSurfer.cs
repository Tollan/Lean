using QuantConnect.Indicators;
using QuantConnect.Orders.Fees;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    public class VXXSurfer : QCAlgorithm
    {
        //Define required variables:
        private const string symbol = "VXX";
        private const decimal shortSL = 1.03m;
        //    private const decimal longSL = 0.90m;
        //    private const decimal drawdown = 0.96m; // 0.97 exp .57

        private const int endHour = 11;

        private decimal holdings = 0;
        private decimal price = 0;
        private decimal dayOpen = 0;
        private decimal stopLoss = 0;
        //    private decimal close1 = 0;

        //Set up the indicator Classes:
        private SimpleMovingAverage close0;
        private RateOfChange roc1;
        private RateOfChange roc2;
        private RateOfChange roc3;
        private RateOfChange roc4;
        private AverageTrueRange atr1;
        private RelativeStrengthIndex rsi2;

        private ExponentialMovingAverage emaShort;

        //Initialize the data and resolution you require for your strategy:
        public override void Initialize()
        {
            SetStartDate(2017, 1, 3);  //SetStartDate(2009, 1, 30);
            SetEndDate(2018, 2, 15);   //SetEndDate(2015, 11, 30);
            SetCash(10000);
            AddSecurity(SecurityType.Equity, symbol, Resolution.Minute); // Resolution.Daily); 

            close0 = SMA(symbol, 1, Resolution.Daily);

            roc1 = ROC(symbol, 1, Resolution.Daily);
            roc2 = ROC(symbol, 2, Resolution.Daily);
            roc3 = ROC(symbol, 3, Resolution.Daily);
            roc4 = ROC(symbol, 10, Resolution.Daily);
            atr1 = ATR(symbol, 1, MovingAverageType.Simple, Resolution.Daily);
            rsi2 = RSI(symbol, 2, MovingAverageType.Simple, Resolution.Daily);
            emaShort = EMA(symbol, 10, Resolution.Daily);

            // Set commissions to $1
            Securities[symbol].FeeModel = new ConstantFeeModel(1);

        }

        public void OnData(TradeBars data)
        {
            // wait for slowest indicator to fully initialize
            if (!emaShort.IsReady) return;

            TradeBar b = null;
            data.TryGetValue(symbol, out b);
            price = b.Close;
            holdings = Portfolio[symbol].Quantity;

            // EXIT
            if (b.Time.Minute == 45
                && b.Time.Hour == endHour
                && holdings > 0
                ) // exit long positions 1-2 hours after market open
                Liquidate();

            if (holdings < 0
                && (roc2 > 0
                || price > stopLoss)
                ) // exit short positions with trailing stop or two-day uptrend
                Liquidate();

            // On market open
            if (b.Time.Hour == 9 && b.Time.Minute == 30) dayOpen = b.Open;

            // ENTRY
            if (b.Time.Hour == 9 && b.Time.Minute == 30
                // SAFETY -- only enter a new position if in cash
                //	&& Portfolio.Invested == false
                )
            {
                // check for long entry
                if ((roc1 > 0   // last trading day closed higher than the previous trading day
                    && roc2 > 0
                    && roc3 > 0)
                    && roc4 > 0
                    && dayOpen < close0 * 1.00m // today opened below last trading day's close
                    && price > emaShort
                    )
                {
                    SetHoldings(symbol, 1.0m, false, "open");
                    //	stopLoss = price*longSL;
                }
                // check for short entry
                if (roc2 < 0)   // two-day downtrend   
                {
                    SetHoldings(symbol, -1.0m, false, "open");
                    stopLoss = price * shortSL;
                }
            } // if

            // update trailing stop loss
            if (holdings < 0 && price * shortSL < stopLoss)
                stopLoss = price * shortSL;

        } // OnData
    } // QCAlgo
}
