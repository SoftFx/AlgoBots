MovingAverageBot
===

## Idea
The Moving Average bot uses one Moving Average indicator to trade. If a candle crosses the Moving Average from below, the EA will enter a long position. 
Vice Versa for the short position. If you enter a Padding in Pips, the bot will only open a buy position if the price is X pips above the Moving Average Line.

## Formulas

The bot opens a trade when the price crosses the MA. The volume of the lot count as:

    volume = FreeMargin * MaximumRisk / p
*Where __p__ is the price for 1.0 volume*  

If the number of unsuccessful trades is more than 1 in a row, the volume of the lot will be reduced by the formula:

    volume = volume - i / DecreaseFactor
*Where __i = 2 .. n__ and __n__ is the number of unsuccessful trades  in a row*  

## Parameters

### Maximum Risk
Responsible for Money Management. Here, in percentage, it is determined by how much of your account the Moving Average bot will trade. The parameter must be between 0 and 1.

### Decrease Factor
This parameter is responsible for limiting losses. The parameter must be more than 0.

### Moving Period
Sets the period MA. The parameter must be more than 0.

### Moving Shift
Sets the shift MA
