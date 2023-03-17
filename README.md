# AlgoBots
This repository contains public indicators and bots for SoftFx Bot API.

* [ImportAccountStateBot](https://github.com/SoftFx/AlgoBots/tree/develop/ImportAccountStateBot)
This bot helps to integrate bots written on another programming languange to SoftFx Environment. Just write required trade state to csv file from python, r, mathlab and this bot will adjust orders and positions to required state.

* [100YearPortfolio](https://github.com/SoftFx/AlgoBots/tree/develop/100YearPortfolio)
Based on the stock distribution in the Portfolio sheet, this bot tries to open and change positions according to desired distribution.

* [MACDsampleBot](https://github.com/SoftFx/AlgoBots/tree/develop/MACDsampleBot)
The MACD expert advisor is designed to use moving average convergence and divergence in order to enter a trade.
This will open up a trade once the signal line breaks through the MACD histogram and both are below (or above) the zero lines.

* [MovingAverageBot](https://github.com/SoftFx/AlgoBots/tree/develop/MovingAverageBot)
The Moving Average bot uses one Moving Average indicator to trade. If a candle crosses the Moving Average from below, the EA will enter a long position. Vice Versa for the short position. If you enter a Padding in Pips, the bot will only open a buy position if the price is X pips above the Moving Average Line.

* [TPtoAllNewPositionsInPercents](https://github.com/SoftFx/AlgoBots/tree/develop/TPtoAllNewPositionsInPercents)
The bot emulates the TakeProfit for positions on Net account. Every time interval it checks the amount of positions and updates the limit orders set so that the Amount of position opened by a symbol is equivalent the amount of limit orders.
