from clr import AddReference
AddReference("System")
AddReference("System.Core")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")
#AddReference("QuantConnect.Indicators")

from System import *
from System.Collections.Generic import List
# from System.Drawing import Color
from QuantConnect import *
from QuantConnect.Algorithm import *
#from QuantConnect.Indicators import *
from datetime import *
from decimal import *
import pandas as pd
from pandas.tseries.offsets import *

# Cieslak, Morse & Vissing-Jorgensen (2014)
# http://pages.stern.nyu.edu/~adamodar/New_Home_Page/datafile/histretSP.html
class FOMCAlgorithm(QCAlgorithm):
    
    def Initialize(self):
        self.trade_symbol = "SPY"
        self.AddEquity(self.trade_symbol)
        self.SetCash(100000)
        self.SetCalendar(date(1998,1,1), date(2018,1,1))
        self.SetBenchmark(self.trade_symbol)

        self.stopLoss = 0
        
    def SetCalendar(self, start_date, end_date):
        self.SetStartDate(start_date)
        self.SetEndDate(end_date)
        url = "https://www.dropbox.com/s/njjthytp1q0oqd7/fomc_calendar.csv?dl=1"
        self.fomc_calendar = pd.read_csv(url, index_col=0, parse_dates=[0], infer_datetime_format=True)
    
    def OnData(self, data):
        # self.hold_fomc_week() 
        # self.on_fomc_week()
        self.hybrid_fomc_week2() # best
        pass
    
    def hold_fomc_week(self):
        if not (self.Time.hour <= 9 and self.Time.minute <= 31): return
        fomc_week = self.fomc_calendar.loc[self.Time.date()]['fomc_week']
        is_even_fomc_week = fomc_week % 2 == 0
        if self.Portfolio[self.trade_symbol].Invested:
            if not is_even_fomc_week:
                self.Liquidate()
        elif is_even_fomc_week:
            self.SetHoldings(self.trade_symbol, 1)
    
    def on_fomc_week(self):
        if not (self.Time.hour >= 15 and self.Time.minute >= 59): return
        next_fomc_week = self.fomc_calendar.loc[self.Time.date() + BDay(1)]['fomc_week']
        is_even_fomc_week = next_fomc_week % 2 == 0
        if self.Portfolio[self.trade_symbol].Invested:
            if not is_even_fomc_week:
                self.Liquidate()
                # tag = "Exit-Week: {0} Day: {1}".format(self.fomc_calendar.loc[self.Time.date()]['fomc_week'], self.fomc_calendar.loc[self.Time.date()]['fomc_day'])
                # self.MarketOrder(self.trade_symbol, -self.Portfolio[self.trade_symbol].Quantity, False, tag)
        elif is_even_fomc_week:
            self.SetHoldings(self.trade_symbol, 1)
            # tag = "Entry-Week: {0} Day: {1}".format(self.fomc_calendar.loc[self.Time.date()]['fomc_week'], self.fomc_calendar.loc[self.Time.date()]['fomc_day'])
            # max_shares = self.Portfolio.TotalPortfolioValue // self.Securities[self.trade_symbol].Price
            # self.MarketOrder(self.trade_symbol, max_shares, False, tag)
    
    def hybrid_fomc_week(self):
        if (self.Time.hour >= 15 and self.Time.minute >= 59 and
            self.Portfolio[self.trade_symbol].Invested):
            next_fomc_week = self.fomc_calendar.loc[self.Time.date() + BDay(1)]['fomc_week']
            is_next_week_even = next_fomc_week % 2 == 0
            if not is_next_week_even:
                self.Liquidate()
        if (self.Time.hour <= 9 and self.Time.minute <= 31 and
            not self.Portfolio[self.trade_symbol].Invested):
            fomc_week = self.fomc_calendar.loc[self.Time.date()]['fomc_week']
            is_even_fomc_week = fomc_week % 2 == 0
            if is_even_fomc_week:
                self.SetHoldings(self.trade_symbol, 1)
    
    def hybrid_fomc_week2(self):
        if (self.Time.hour <= 9 and self.Time.minute <= 31 and
            self.Portfolio[self.trade_symbol].Invested):
            fomc_week = self.fomc_calendar.loc[self.Time.date()]['fomc_week']
            is_even_fomc_week = fomc_week % 2 == 0
            if not is_even_fomc_week: # or self.Securities[self.trade_symbol].Price < self.stopLoss:                
                self.Liquidate()
        if (self.Time.hour >= 15 and self.Time.minute >= 59 and
            not self.Portfolio[self.trade_symbol].Invested):
            next_fomc_week = self.fomc_calendar.loc[self.Time.date() + BDay(1)]['fomc_week']
            is_next_week_even = next_fomc_week % 2 == 0
            if is_next_week_even:
                self.SetHoldings(self.trade_symbol, 1)
    
    def OnEndOfDay(self):
        self.stopLoss = self.Securities[self.trade_symbol].Price * 0.98
        #self.Log(str(self.Securities[self.trade_symbol].Price))