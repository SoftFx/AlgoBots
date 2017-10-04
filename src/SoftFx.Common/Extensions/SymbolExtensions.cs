using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace SoftFx.Common.Extensions
{
    public static class SymbolExtensions
    {
        /// <summary>
        /// Determines whether symbol can be used for trading
        /// </summary>
        /// <returns>true if symbol can be used for trading, false otherwise</returns>
        public static bool CanTrade(this Symbol symbol)
        {
            return !symbol.IsNull && symbol.IsTradeAllowed && !double.IsNaN(symbol.Ask) && !double.IsNaN(symbol.Bid);
        }

        /// <summary>
        /// Calculates number of digits in fractional part of symbol step
        /// </summary>
        /// <returns>number of digits in fractional part of symbol step, -1 if <code>symbol.IsNull == true</code></returns>
        public static int StepDigits(this Symbol symbol)
        {
            return symbol.IsNull ? -1 : symbol.TradeVolumeStep.Digits();
        }

        /// <summary>
        /// Rounds price to symbol digits + extraDigits
        /// </summary>
        /// <returns>Rounded price, NaN if <code>symbol.IsNull == true</code></returns>
        public static double RoundPrice(this Symbol symbol, double price, int extraDigits = 0)
        {
            return symbol.IsNull ? double.NaN : price.Round(symbol.Digits + extraDigits);
        }

        /// <summary>
        /// Ceils or Floors price to symbol digits + extraDigits according to side
        /// </summary>
        /// <returns>Rounded price, NaN if <code>symbol.IsNull == true</code></returns>
        public static double RoundPrice(this Symbol symbol, double price, OrderSide side, int extraDigits = 0)
        {
            return side == OrderSide.Sell ? symbol.FloorPrice(price, extraDigits) : symbol.CeilPrice(price, extraDigits);
        }

        /// <summary>
        /// Floors price to symbol digits + extraDigits
        /// </summary>
        /// <returns>Floored price, NaN if <code>symbol.IsNull == true</code></returns>
        public static double FloorPrice(this Symbol symbol, double price, int extraDigits = 0)
        {
            return symbol.IsNull ? double.NaN : price.Floor(symbol.Digits + extraDigits);
        }

        /// <summary>
        /// Ceils price to symbol digits + extraDigits
        /// </summary>
        /// <returns>Ceiled price, NaN if <code>symbol.IsNull == true</code></returns>
        public static double CeilPrice(this Symbol symbol, double price, int extraDigits = 0)
        {
            return symbol.IsNull ? double.NaN : price.Ceil(symbol.Digits + extraDigits);
        }

        /// <summary>
        /// Rounds volume to symbol step
        /// </summary>
        /// <returns>Rounded volume, NaN if <code>symbol.IsNull == true</code></returns>
        public static double RoundVolume(this Symbol symbol, double volume)
        {
            return symbol.IsNull ? double.NaN : volume.Round(symbol.TradeVolumeStep);
        }

        /// <summary>
        /// Floors volume to symbol step
        /// </summary>
        /// <returns>Floored volume, NaN if <code>symbol.IsNull == true</code></returns>
        public static double FloorVolume(this Symbol symbol, double volume)
        {
            return symbol.IsNull ? double.NaN : volume.Floor(symbol.TradeVolumeStep);
        }

        /// <summary>
        /// Ceils volume to symbol step
        /// </summary>
        /// <returns>Ceiled volume, NaN if <code>symbol.IsNull == true</code></returns>
        public static double CeilVolume(this Symbol symbol, double volume)
        {
            return symbol.IsNull ? double.NaN : volume.Ceil(symbol.TradeVolumeStep);
        }

        /// <summary>
        /// Converts price to string with fixed number of fractional digits taken from symbol digits
        /// </summary>
        /// <returns>Rounded price string, NaN if <code>symbol.IsNull == true</code></returns>
        public static string ToPriceString(this Symbol symbol, double price)
        {
            return symbol.IsNull ? "NaN" : price.ToString(symbol.Digits);
        }

        /// <summary>
        /// Converts volume to string with fixed number of fractional digits calculated from symbol step
        /// </summary>
        /// <returns>Rounded volume string, NaN if <code>symbol.IsNull == true</code></returns>
        public static string ToVolumeString(this Symbol symbol, double volume)
        {
            return symbol.IsNull ? "NaN" : volume.ToString(symbol.TradeVolumeStep);
        }

        /// <summary>
        /// Calculates commission. If calculation method is unknown returns NaN
        /// </summary>
        /// <returns>Commission if it is calculated properly, NaN otherwise</returns>
        public static double CalculateCommission(this Symbol symbol, AccountTypes accountType, bool usePendingOrders)
        {
            if (symbol.IsNull)
            {
                return double.NaN;
            }

            if (symbol.CommissionChargeMethod != CommissionChargeMethod.OneWay &&
                symbol.CommissionChargeMethod != CommissionChargeMethod.RoundTurn)
            {
                return double.NaN;
            }
            if (symbol.CommissionChargeType != CommissionChargeType.PerLot &&
                symbol.CommissionChargeType != CommissionChargeType.PerTrade)
            {
                return double.NaN;
            }
            if (accountType == AccountTypes.Cash && symbol.CommissionType != CommissionType.Percent)
            {
                return double.NaN;
            }

            var commissionValue = usePendingOrders ? symbol.LimitsCommission : symbol.Commission;

            switch (symbol.CommissionType)
            {
                case CommissionType.Percent:
                    return commissionValue / 100.0;
                case CommissionType.Absolute:
                    return commissionValue / symbol.ContractSize;
                case CommissionType.PerBond:
                    return commissionValue * symbol.Point;
                default:
                    return double.NaN;
            }
        }
    }
}
