100YearPortfolioBot
===

## Idea
Based on the stock distribution in the *Portfolio* sheet, this bot open and change positions according to desired distribution.

## Description
The bot opens/closes Limit orders so that total amount of money invested by Symbol (orders + positions) is equal to the percentage of the account balance in *Portfolio* every N minutes.
Is the equity loss is critical the bot will be stopped.

## Required Links
**For bot running you have to create Google Sheet with Configuration.**

Template for Google Sheet Configuration is here (ссылка)

Guide how to create Google Sheet and setup it is here (ссылка)

If you want to use all features you need to create test google account. Guide you can find here (ссылка).

## Connect to Configuration sheet
First of all you need to create a connection to Configuration before running a bot.

Setup window looks like this:

![Setup Window](screens/SetupWindow.png)

### Parameters

#### **SheetLink**
This is link to your Configuration sheet. String parameter.

#### **CredsFile (optional)**
Path to credentials file on your computer. It is nessesary for update *Status* page on Configuration sheet in real time mode. (for more information read here (ссылка)). String parameter.


## Settings page
The main settings responsible for a bot logic.

![Settings Page](screens/SettingsPage.png)

### Parameters

#### **Once Per N min**
A time after which a bot recalculates orders. Integer parameter. Should be greater than 0.

#### **Balance Type**
Specifies what is the balance for percentage calculation. Enum parameter. Possible values: Balance or Equity.

#### **Equity Min Lvl**
Protection against critical loss of money. Specifies the maximum possible change in equity as a percentage of a last starting point. Double percent parameter (must end with %).

#### **Equity Update Time (sec)**
Protection against critical loss of money. Specifies a period of time which the starting point of equity will be updated. Integer parameter. Should be greater than 1.

#### **Status Update Timeout (sec) (optional)**
A time after which a bot refreshes *Status*. Integer parameter. Should be greater than 0. Default value is 60 sec.



## Portfolio page

Current page consists stock distribution and has 3 columns: Name and Distribution. Also Name column can have Notes with additional settings.


### Main settings

![Portfolio Page](screens/PortfolioPage.png)

#### **Name**
A name of the symbol, which will be used to open orders. String value.

#### **Distribution**
A percentage of money from the account balance for opening orders. Double percent parameter (must end with %). If value is positive value than positions side is Buy else Sell. **Total sum absolute values of column should be less than 100%**

### Additional settings (optional, **available only with cred file**)

![Portfolio Page](screens/NoteSettings.png)

Additional parameters should be written as Note for the first cell.

#### **Symbol**
This is the symbol name on a server side. Used to open orders. If not specified the value **Name** is used by default. String value.

#### **MaxLotSize**
Upper limit for a order volume. Positive double value. Default value is  **Symbol.MaxTradeVolume**

## Filling rules
- The order of records isn't important
- Page might consists blank records for readable
- Property names aren't allowed to be change

## Algorithm

1. The bot will calculate the desired stock number for each symbol using the current **Balance Type**.
2. All previous orders will be canceled (if they exist)
3. For each symbol calculate **Delta money** = DesiredStockNumber - ActualStockNumber this is stock for desired order
4. If **Delta money** is positive value then desired order side is Buy else Sell.
5. The **Expected volume** is calculated using **Delta money**
4. Ignore symbol if **Expected volume** < **Symbol.MinTradeVolume**
5. Trying to open order with a volume = Min(**Expected volume**, **Symbol.MinTradeVolume**, **MaxLotSize**)
8. All orders are opened with Experation = **Once Per N min** + 1

## Status page (available only with cred file)
This page consists current information about account and orders.

![Status Page](screens/StatusPage.png)

The page consists of:
- Current time (UTC format)
- Read config
- Current account information (Balance and Equity)
- Information about unexpected positions and orders (if they exist)
- Information about Portfolio symbols:
    - Symbol name and alias (if exists)
    - Desired symbol percentage
    - Current delta in percentage and lots
    - Current symbol rate
- Current equity starting point and percentage change since last resave
- Time until next resave starting point of equity
- Time until next orders recalculations
