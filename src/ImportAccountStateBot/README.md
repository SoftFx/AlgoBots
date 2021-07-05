ImportAccountStateBot
===

## Idea

The bot tries to reach the account states recorded in the "AccountState.csv". The bot supports only Net account. Order types depend on the Mode parameter. Market orders if Mode = "Market" and Limit with Trailing price if Mode = "TrailingLimit" or "TrailingLimitPercent".

### Account state file
The file in .csv format. The bot sorts and groups all records by time and tries to open these positions by the specified time.

### AccoutState.csv sample
```
Time,Symbol,Side,Volume
2021-07-02T09:15:00Z,BTCUSD,FALSE,0.5
2021-07-02T09:15:00Z,A,TRUE
2021-07-02T09:16:00Z,BTCUSD,TRUE,1.0
2021-07-02T09:17:00Z,BTCUSD,TRUE,1.5
2021-07-02T09:18:00Z,BTCUSD,TRUE,0.5
2021-07-02T09:19:00Z,BTCUSD,FALSE,0.2
2021-07-02T09:20:00Z,BTCUSD,TRUE,1.0
```

### Format AccountState.csv file: "Time,Symbol,Side,Volume".

* Time - time when the position should be on the account
* Symbol - position symbol
* Side - position side. Bool parameter. Possible values: true - Buy, false - Sell
* Volume - position volume

## Config description (actual for version 1.0)
### Config sample
```
IsDebug = false
RefreshTimeout = 1000
Mode = "Market"
SetEmptyStateAtTheEnd = false

[CSVConfig]
DefaultVolume = 1.0
TimeFormat = "yyyy-MM-dd'T'H:mm:ss'Z'"
Separator = ","
SkipFirstLine = true

[TrailingLimitPercentMode]
Percent = 0.1
```
## Parameters

### IsDebug
Defines whether the bot will do specific actions(for ex. additional logging) which are not required during normal usage. Bool parameter. Possible values: true or false.

### RefreshTimeout
Timeout between updating the account state. RefreshTimeout measured in milliseconds. Integer parameter. Value should be greater than 0.

### Mode
Trade opening mode. Enum parameter. Possible values: Market, TrailingLimit, TrailingLimitPercent. Mode affects order types and trailing prices.

### SetEmptyStateAtTheEnd
Close all positions if all account states have been passed. Bool parameter. Possible values: true and false.

### DefaultVolume
Is used if the position volume didn't record in the .csv file. Double parameter. Should be greater than 0.

### TimeFormat
This is a time format in .csv file. String parameter.

### Separator
This is a separator in .csv file. String parameter.

### SkipFirstLine
Skips the first line in the file (for headers). Bool parameter. Possible values: true and false.

### Percent
Is used for calculating TrailingPrice if Mode = "TrailingLimitPercent". Double parameter. Should be greater than 0.
