TPtoAllNewPositionsInPercents
===========

## Main Idea
The bot emulates the TakeProfit for positions on Net account. Every time interval it checks the amount of positions and updates the limit orders set so that the Amount of position opened by a symbol is equivalent the amount of limit orders.

## Bot logic
For every Net position bot opens one or several opposite side Limits so that their bulk volume is the same as Net position. 
Limit’s open price is calculated the next way:
For BuyLimit: 
price = Position.Price * (1 + _symbolTp)
For SellLimit:
price = Position.Price * (1 - Min(_symbolTp, 0.9999)) // preventing negative price if tp >= 1.0

If Position.Volume > Symbol.MaxTradeVolume then LimitOpenPrice is calculated as VWAP price based on all limits by the symbol created by the bot according to the logic
Recalculating and correction is done every RunIntervalInSeconds (3 sec by default). 
Before every recalculation bot checks current orders and cancels invalid Limits (If the isolation is on, the bot checks only its own Limits).
The next orders are detected as invalid: 
1.	If there is no a position by the symbol on the account.
2.	If a Limit order side is the same as the correspondent position side.
3.	If the value _symbolTp was changed (Order comment is checked)
4.	If Order Volume < _symbolMinVolume (defined in Bot Settings)


## Config description (actual for version 1.3)

### Config sample
```
RunIntervalInSeconds = 3
DefaultTP = 0.03
DefaultMinVolume = 0.1

[SymbolsSettings]
USDRUB = "TP=0.10; MinVolume=0.1"
GBPJPY = "MinVolume=0.05; TP=0.10"
AUDUSD = "TP=0.20; MinVolume=0.1;"
EURJPY = "TP=0.10;"
AUDNZD = "TP=0.05"
GBPAUD = "MinVolume=0.1; TP=0.15;"
GBPUSD = "TP=0.10; MinVolume=0.3"
NZDUSD = "TP= 0.10; MinVolume = 0.22"
USDCNH = "TP=0.10; MINVOLUME=0.1"
EURCHF = "tp=0.03; minvolume=0.03"
EURUSD = "MinVolume=0.1;"
HKDJPY = "MinVolume=0.2"
```

## Parameters

### RunIntervalInSeconds
Delay between chain updates.

### DefaultTP
TP coefficient value to be applied if the symbol is not found in the dictionary.

### DefaultMinVolume
MinVolume value to be applied if the symbol is not found in the dictionary.

### SymbolsSettings
This is a dictionary, where the key is the name of the symbol, and the value this is a string in the format: *TP=value; MinVolume=value*. Where TP is profit in range [0..+inf), and MinVolume the min limit volume is greater than 0.