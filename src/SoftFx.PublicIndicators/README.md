LPRatesIndicator
===========

## Idea
Indicator gets and draws on a chart asks/bids from different liquidity providers such as Tidex, Livecoin and others.

## List of supported liquidity prodiders

[SoftFX](https://www.xbtce.com/)

[Tidex](https://tidex.com/)

[Livecoin](https://www.livecoin.net/)

[Okex](https://www.okex.com/)

[Binance](https://www.binance.com/)

[Bitfinex](https://www.bitfinex.com/)

[HitBTC](https://hitbtc.com/)

[Kraken](https://www.kraken.com)

[Kucoin](https://www.kucoin.com)

[Huobi](https://www.huobi.pro)

## Public API examples

### Get ticker for BTCUSD symbol
```
https://cryptottlivewebapi.xbtce.net:8443/api/v1/public/tick/BTCUSD
https://api.tidex.com/api/3/ticker/btc_usdt
https://api.livecoin.net//exchange/ticker?currencyPair=BTC/USD
https://www.okex.com/api/v1/ticker.do?symbol=btc_usdt
https://api.binance.com/api/v3/ticker/bookTicker?symbol=BTCUSDT
https://api.bitfinex.com/v2/ticker/tBTCUSD
https://api.hitbtc.com/api/2/public/ticker/BTCUSD
https://api.kraken.com/0/public/Ticker?pair=XBTUSD
https://api.kucoin.com/v1/BTC-USDT/open/tick
https://api.huobi.pro/market/detail/merged?symbol=btcusdt
```

## Parameters

### Save to DB
If the parameter is checked, all prices from lps will be saved in database for the definite period of time.

### Restore from DB
If the parameter is checked and the database contains prices for the needed period of time, best asks and bids will be restored and drawn on the chart.

### Symbol sections
Each sybmol section contains name of symbol which substitutes in a suitable ticker url. 
If the section is empty, the indicator will not send a request to the corresponding server. 
Symbol must have a specific format for each server (ex. btc_usdt for Tidex, BTC/USD for Livecoin).