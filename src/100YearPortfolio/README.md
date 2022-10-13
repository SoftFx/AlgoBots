100YearPortfolioBot
===

## Idea

Based on the stock distribution in the *Portfolio* sheet, this bot tries to open and change positions according to desired distribution.

## Description

Every N minutes a bot opens/closes Limit orders so that total amount of money invested by Symbol (orders + positions) is equal to the percentage of the account balance in *Portfolio*.
Is the equity loss is critical the bot will be stopped.

## Required Links

**For bot running you have to create Google Sheet with Configuration.**

Template for Google Sheet Configuration is here (ссылка)

Guide how to create Google Sheet and setup it is here (ссылка)


## Connect to Configuration sheet

First of all you need to create a connection to Configuration before running a bot.

Setup window looks like this:

![Setup Window](screens/SetupWindow.png)

### Parameters

#### **IsDebug**
Defines whether the bot will do specific actions(for ex. additional logging) which are not required during normal usage. Bool parameter. Possible values: true or false.

#### **SheetLink**
This is link to your Configuration sheet. String parameter.

#### **CredsFile**
Path to credentials file on your computer. It is nessesary for update *Status* page on Configuration sheet in real time mode. (for more information read here (ссылка)). String parameter.


## Settings page
The main settings responsible for a bot logic.

![Settings Page](screens/SettingsPage.png)

### Parameters

#### **Once Per N min**
This is a time after whitch a bot recalculates orders. Integer parameter. Should be greater than 0.

#### **Status Update Timeout (sec)**
This is a time after whitch a bot refreshes *Status*. Integer parameter. Should be greater than 0.

#### **Balance Type**
Specifies what is the balance for percentage calculation. Enum parameter. Possible values: Balance or Equity.

#### **Equity Min Lvl**
Protection against critical loss of money. Specifies the maximum possible change in equity as a percentage of a last starting point. Double percent parameter (can be written as 10 or 10%).

#### **Equity Update Time (sec)**
Protection against critical loss of money. Specifies a period of time which the starting point of equity will be updated. Integer parameter. Should be greater than 1.

#### **Default Max Lots Sum**
Default maximum volume amount that can be opened by Symbol. Double value. Should be greater than 0.

## Portfolio page

![Portfolio Page](screens/PortfolioPage.png)

## **Important Notes**

## Algorithm

## Status page


### RefreshTimeout
Timeout between updating the account state. RefreshTimeout measured in milliseconds. Integer parameter. Value should be greater than 0.

### Mode
Trade opening mode. Enum parameter. Possible values: Market, TrailingLimit, TrailingLimitPercent. Mode affects order types and trailing prices.

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
