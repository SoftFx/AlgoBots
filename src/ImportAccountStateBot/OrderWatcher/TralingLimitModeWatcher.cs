using SoftFx.Common.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace ImportAccountStateBot
{
    public sealed class TralingLimitModeWatcher : OrderBaseWatcher
    {
        private readonly TrailingLimitPercentModeConfig _config;
        private readonly double _tralingPercentCoef;


        public TralingLimitModeWatcher(string symbol, ImportAccountStateBot bot) : base(symbol, bot)
        {
            _config = bot.Config.TrailingLimitPercentMode;
            _tralingPercentCoef = _config.Percent * 0.01 * _symbol.Point;
        }


        protected override bool TryBuildOpenRequest(TransactionToken token, out OpenOrderRequest.Template template)
        {
            var traling = CalculateTraling();

            _bot.PrintDebug($"{_symbol.Name} traling = {traling}");

            template = BuildBaseOpenTemplate(token).WithPrice(PriceWithPips(token.Side, traling))
                                                   .WithComment($"{traling:F10}")
                                                   .WithType(OrderType.Limit);

            return traling.Gte(0.0);
        }

        public override async Task CorrectAllOrders()
        {
            var correctList = new List<Task>(1 << 3);

            foreach (var order in Orders)
                correctList.Add(CorrectOrder(order));

            await Task.WhenAll(correctList);
        }

        private async Task CorrectOrder(Order order)
        {
            if (!double.TryParse(order.Comment, out var traling))
                traling = 0.0;

            var tralingPrice = PriceWithPips(order.Side, traling);

            if (!order.Price.E(tralingPrice))
            {
                var request = ModifyOrderRequest.Template
                                                .Create()
                                                .WithOrderId(order.Id)
                                                .WithPrice(tralingPrice)
                                                .MakeRequest();

                await _bot.ModifyOrderAsync(request);
            }
        }

        private double CalculateTraling()
        {
            var traling = 0.0;

            switch (_bot.Config.Mode)
            {
                case ImportMode.TrailingLimit:
                    traling = (_symbol.Ask - _symbol.Bid) * 0.5;
                    break;
                case ImportMode.TrailingLimitPercent:
                    traling = (_symbol.Ask + _symbol.Bid) * 0.5 * _tralingPercentCoef;
                    break;
            }

            return traling;
        }

        private double PriceWithPips(OrderSide side, double traling)
        {
            if (side.IsBuy())
                return _symbol.LastQuote.Ask - traling;
            else
                return _symbol.LastQuote.Bid + traling;
        }
    }
}
