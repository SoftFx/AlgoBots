using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTrader.Algo.Api;
using TickTrader.SeriesStorage;
using TickTrader.SeriesStorage.LevelDb;

namespace SoftFx.PublicIndicators
{
    [Indicator(DisplayName = "LP Rates Indicator", Category = CommonConstants.Category, Version = "1.0")]
    class LPRatesIndicator : Indicator
    {
        [Parameter(DisplayName = "Save to DB", DefaultValue = false)]
        public bool EnableSaving { get; set; }

        [Parameter(DisplayName = "Restore from DB", DefaultValue = false)]
        public bool EnableRestoring { get; set; }

        #region Symbols
        [Parameter(DisplayName = "SoftFX server", DefaultValue = "cryptottlivewebapi.xbtce.net:8443")]
        public string SoftFxServer { get; set; }

        [Parameter(DisplayName = "SoftFX Symbol", DefaultValue = "BTCUSD")]
        public string SoftFxSymbol { get; set; }

        [Parameter(DisplayName = "Tidex Symbol", DefaultValue = "btc_usdt")]
        public string TidexSymbol { get; set; }

        [Parameter(DisplayName = "Livecoin Symbol", DefaultValue = "BTC/USD")]
        public string LivecoinSymbol { get; set; }

        [Parameter(DisplayName = "Okex Symbol", DefaultValue = "btc_usdt")]
        public string OkexSymbol { get; set; }

        [Parameter(DisplayName = "Binance Symbol", DefaultValue = "BTCUSDT")]
        public string BinanceSymbol { get; set; }

        [Parameter(DisplayName = "Bitfinex Symbol", DefaultValue = "tBTCUSD")]
        public string BitfinexSymbol { get; set; }

        [Parameter(DisplayName = "HitBTC Symbol", DefaultValue = "BTCUSD")]
        public string HitBtcSymbol { get; set; }

        [Parameter(DisplayName = "Kraken Symbol", DefaultValue = "XBTUSD")]
        public string KrakenSymbol { get; set; }

        [Parameter(DisplayName = "Kucoin Symbol", DefaultValue = "BTC-USDT")]
        public string KucoinSymbol { get; set; }

        [Parameter(DisplayName = "Huobi Symbol", DefaultValue = "btcusdt")]
        public string HuobiSymbol { get; set; }
        #endregion

        #region BidOutpus
        [Output(DisplayName = "SoftFx Bid", Target = OutputTargets.Overlay, DefaultColor = Colors.Black, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries SoftFxBid { get; set; }

        [Output(DisplayName = "Tidex Bid", Target = OutputTargets.Overlay, DefaultColor = Colors.SandyBrown, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries TidexBid { get; set; }

        [Output(DisplayName = "Livecoin Bid", Target = OutputTargets.Overlay, DefaultColor = Colors.DarkRed, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries LivecoinBid { get; set; }

        [Output(DisplayName = "Okex Bid", Target = OutputTargets.Overlay, DefaultColor = Colors.ForestGreen, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries OkexBid { get; set; }

        [Output(DisplayName = "Binance Bid", Target = OutputTargets.Overlay, DefaultColor = Colors.Gold, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries BinanceBid { get; set; }

        [Output(DisplayName = "Bitfinex Bid", Target = OutputTargets.Overlay, DefaultColor = Colors.Aqua, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries BitfinexBid { get; set; }

        [Output(DisplayName = "HitBTC Bid", Target = OutputTargets.Overlay, DefaultColor = Colors.DarkOliveGreen, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries HitBtcBid { get; set; }

        [Output(DisplayName = "Kraken Bid", Target = OutputTargets.Overlay, DefaultColor = Colors.Coral, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries KrakenBid { get; set; }

        [Output(DisplayName = "Kucoin Bid", Target = OutputTargets.Overlay, DefaultColor = Colors.DeepPink, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries KucoinBid { get; set; }

        [Output(DisplayName = "Huobi Bid", Target = OutputTargets.Overlay, DefaultColor = Colors.DeepSkyBlue, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries HuobiBid { get; set; }
        #endregion

        #region AskOutputs
        [Output(DisplayName = "SoftFx Ask", Target = OutputTargets.Overlay, DefaultColor = Colors.Black, DefaultLineStyle = LineStyles.LinesDots, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries SoftFxAsk { get; set; }

        [Output(DisplayName = "Tidex Ask", Target = OutputTargets.Overlay, DefaultColor = Colors.SandyBrown, DefaultLineStyle = LineStyles.LinesDots, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries TidexAsk { get; set; }

        [Output(DisplayName = "Livecoin Ask", Target = OutputTargets.Overlay, DefaultColor = Colors.DarkRed, DefaultLineStyle = LineStyles.LinesDots, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries LivecoinAsk { get; set; }

        [Output(DisplayName = "Okex Ask", Target = OutputTargets.Overlay, DefaultColor = Colors.ForestGreen, DefaultLineStyle = LineStyles.LinesDots, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries OkexAsk { get; set; }

        [Output(DisplayName = "Binance Ask", Target = OutputTargets.Overlay, DefaultColor = Colors.Gold, DefaultLineStyle = LineStyles.LinesDots, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries BinanceAsk { get; set; }

        [Output(DisplayName = "Bitfinex Ask", Target = OutputTargets.Overlay, DefaultColor = Colors.Aqua, DefaultLineStyle = LineStyles.LinesDots, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries BitfinexAsk { get; set; }

        [Output(DisplayName = "HitBTC Ask", Target = OutputTargets.Overlay, DefaultColor = Colors.DarkOliveGreen, DefaultLineStyle = LineStyles.LinesDots, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries HitBtcAsk { get; set; }

        [Output(DisplayName = "Kraken Ask", Target = OutputTargets.Overlay, DefaultColor = Colors.Coral, DefaultLineStyle = LineStyles.LinesDots, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries KrakenAsk { get; set; }

        [Output(DisplayName = "Kucoin Ask", Target = OutputTargets.Overlay, DefaultColor = Colors.DeepPink, DefaultLineStyle = LineStyles.LinesDots, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries KucoinAsk { get; set; }

        [Output(DisplayName = "Huobi Ask", Target = OutputTargets.Overlay, DefaultColor = Colors.DeepSkyBlue, DefaultLineStyle = LineStyles.LinesDots, PlotType = PlotType.DiscontinuousLine)]
        public DataSeries HuobiAsk { get; set; }
        #endregion

        private Dictionary<LiquidityProvider, string> _tickerUrl;
        private List<LiquidityProvider> _requiredLP;

        private HttpClient _softFxClient;
        private HttpClient _defaultClient;

        private readonly static IKeySerializer<DateTime> _keySerializer = new DateTimeKeySerializer();
        private readonly DateTime _startTime = DateTime.Now.ToUniversalTime();
        private string _dbPath;
        private bool _isHistoryBarsProcessed = false;

        protected override async void Calculate(bool isNewBar)
        {
            var isHistoryBar = _startTime > Bars[0].CloseTime;
            if ( isHistoryBar || isNewBar )
                return;

            if (!_isHistoryBarsProcessed && EnableRestoring)
                ProcessHistoryBars();
            else
                await ProcessCurrentBar();
            
            
        }

        #region Initialization methods
        protected override void Init()
        {
            var pathInfo = System.IO.Directory.CreateDirectory("LevelDB");
            _dbPath = pathInfo.Name + "\\" + Symbol.Name;

            InitClients();
            InitTickerUrls();
            InitRequiredLP();
            base.Init();
        }

        private void InitClients()
        {
            InitSoftFxClient();
            InitDefaultClient();
        }

        private void InitTickerUrls()
        {
            _tickerUrl = new Dictionary<LiquidityProvider, string>();

            _tickerUrl.Add(LiquidityProvider.SoftFx, $"https://{SoftFxServer}/api/v1/public/tick/{SoftFxSymbol}");
            _tickerUrl.Add(LiquidityProvider.Tidex, $"https://api.tidex.com/api/3/ticker/{TidexSymbol}");
            _tickerUrl.Add(LiquidityProvider.Livecoin, $"https://api.livecoin.net//exchange/ticker?currencyPair={LivecoinSymbol}");
            _tickerUrl.Add(LiquidityProvider.Okex, $"https://www.okex.com/api/v1/ticker.do?symbol={OkexSymbol}");
            _tickerUrl.Add(LiquidityProvider.Binance, $"https://api.binance.com/api/v3/ticker/bookTicker?symbol={BinanceSymbol}");
            _tickerUrl.Add(LiquidityProvider.Bitfinex, $"https://api.bitfinex.com/v2/ticker/{BitfinexSymbol}");
            _tickerUrl.Add(LiquidityProvider.HitBtc, $"https://api.hitbtc.com/api/2/public/ticker/{HitBtcSymbol}");
            _tickerUrl.Add(LiquidityProvider.Kraken, $"https://api.kraken.com/0/public/Ticker?pair={KrakenSymbol}");
            _tickerUrl.Add(LiquidityProvider.Kucoin, $"https://api.kucoin.com/v1/{KucoinSymbol}/open/tick");
            _tickerUrl.Add(LiquidityProvider.Huobi, $"https://api.huobi.pro/market/detail/merged?symbol={HuobiSymbol}");
        }

        private void InitRequiredLP()
        {
            _requiredLP = new List<LiquidityProvider>();

            if (!SoftFxSymbol.Equals(""))
                _requiredLP.Add(LiquidityProvider.SoftFx);

            if (!LivecoinSymbol.Equals(""))
                _requiredLP.Add(LiquidityProvider.Livecoin);

            if (!TidexSymbol.Equals(""))
                _requiredLP.Add(LiquidityProvider.Tidex);

            if (!OkexSymbol.Equals(""))
                _requiredLP.Add(LiquidityProvider.Okex);

            if (!BinanceSymbol.Equals(""))
                _requiredLP.Add(LiquidityProvider.Binance);

            if (!BitfinexSymbol.Equals(""))
                _requiredLP.Add(LiquidityProvider.Bitfinex);

            if (!HitBtcSymbol.Equals(""))
                _requiredLP.Add(LiquidityProvider.HitBtc);

            if (!KrakenSymbol.Equals(""))
                _requiredLP.Add(LiquidityProvider.Kraken);

            if (!KucoinSymbol.Equals(""))
                _requiredLP.Add(LiquidityProvider.Kucoin);

            if (!HuobiSymbol.Equals(""))
                _requiredLP.Add(LiquidityProvider.Huobi);

        }

        private void InitSoftFxClient()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            _softFxClient = new HttpClient(handler);
            _softFxClient.DefaultRequestHeaders.Accept.Clear();
            _softFxClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (SoftFxServer.Equals("tp.st.soft-fx.eu:8443"))
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        }

        private void InitDefaultClient()
        {
            _defaultClient = new HttpClient();
            _defaultClient.DefaultRequestHeaders.Add("Accept", "application/json");
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 9999;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
        }
        #endregion

        private async Task<double[]> ProcessLps(LiquidityProvider lp, int initBarsCount)
        {
            var tick = await GetTick(lp);

            var bid = GetBid(lp, tick);
            var ask = GetAsk(lp, tick);

            OnMainThread(() => { AddToIndicatorOutput(lp, bid, ask, Bars.Count - initBarsCount); });

            return new double[] { bid, ask };
        }

        private void ProcessHistoryBars()
        {
            using (var storage = new LevelDbStorage(_dbPath))
            {
                var savedLps = new List<LiquidityProvider>();
                var bidCollections = new Dictionary<LiquidityProvider, IBinaryStorageCollection<DateTime>>();
                var askCollections = new Dictionary<LiquidityProvider, IBinaryStorageCollection<DateTime>>();

                try
                {

                    foreach (var lp in _requiredLP)
                    {
                        if (storage.Collections.Contains(lp.ToString() + "Bid") && storage.Collections.Contains(lp.ToString() + "Ask"))
                        {
                            savedLps.Add(lp);
                            bidCollections[lp] = storage.GetBinaryCollection(lp.ToString() + "Bid", _keySerializer);
                            askCollections[lp] = storage.GetBinaryCollection(lp.ToString() + "Ask", _keySerializer);
                        }
                    }


                    for (int pos = Bars.Count - 1; pos > 0; pos--)
                        foreach (var lp in savedLps)
                        {
                            if (bidCollections[lp].Read(Bars[pos].OpenTime, out var x))
                                Math.Abs(-1);
                            var bid = bidCollections[lp].Read(Bars[pos].OpenTime, out var bidSeg) ? BitConverter.ToDouble(bidSeg.Array, 0) : double.NaN;
                            var ask = askCollections[lp].Read(Bars[pos].OpenTime, out var askSeg) ? BitConverter.ToDouble(askSeg.Array, 0) : double.NaN;
                            var localLp = lp;
                            var localPos = pos;
                            OnMainThreadAsync(() => AddToIndicatorOutput(localLp, bid, ask, localPos));
                        }
                }
                finally
                {
                    foreach (var lp in savedLps)
                    {
                        bidCollections[lp].Dispose();
                        askCollections[lp].Dispose();
                    }

                }
            }

            _isHistoryBarsProcessed = true;
        }

        private async Task ProcessCurrentBar()
        {
            int initBarsCount = Bars.Count;

            var lpsSnapshot = new Dictionary<string, double>();
            var lpsProcesses = new Dictionary<LiquidityProvider, Task<double[]>>();

            foreach (var lp in _requiredLP)
                lpsProcesses[lp] = ProcessLps(lp, initBarsCount);

            await Task.WhenAll(lpsProcesses.Values);

            foreach (var lp in lpsProcesses)
            {
                lpsSnapshot[lp.Key.ToString() + "Bid"] = lp.Value.Result[0];
                lpsSnapshot[lp.Key.ToString() + "Ask"] = lp.Value.Result[1];
            }

            if(EnableSaving)
                SaveToDB(lpsSnapshot, initBarsCount);
        }

        private void AddToIndicatorOutput(LiquidityProvider lp, double bid, double ask, int position = 0)
        {
            switch (lp)
            {
                case LiquidityProvider.SoftFx:
                    SoftFxBid[position] = bid;
                    SoftFxAsk[position] = ask;
                    break;
                case LiquidityProvider.Livecoin:
                    LivecoinBid[position] = bid;
                    LivecoinAsk[position] = ask;
                    break;
                case LiquidityProvider.Tidex:
                    TidexBid[position] = bid;
                    TidexAsk[position] = ask;
                    break;
                case LiquidityProvider.Okex:
                    OkexBid[position] = bid;
                    OkexAsk[position] = ask;
                    break;
                case LiquidityProvider.Binance:
                    BinanceBid[position] = bid;
                    BinanceAsk[position] = ask;
                    break;
                case LiquidityProvider.Bitfinex:
                    BitfinexBid[position] = bid;
                    BitfinexAsk[position] = ask;
                    break;
                case LiquidityProvider.HitBtc:
                    HitBtcBid[position] = bid;
                    HitBtcAsk[position] = ask;
                    break;
                case LiquidityProvider.Kraken:
                    KrakenBid[position] = bid;
                    KrakenAsk[position] = ask;
                    break;
                case LiquidityProvider.Kucoin:
                    KucoinBid[position] = bid;
                    KucoinAsk[position] = ask;
                    break;
                case LiquidityProvider.Huobi:
                    HuobiBid[position] = bid;
                    HuobiAsk[position] = ask;
                    break;

            }
        }

        private async Task<HttpResponseMessage> GetTick(LiquidityProvider lp)
        {
            _tickerUrl.TryGetValue(lp, out var url);

            try
            {
                if (lp == LiquidityProvider.SoftFx)
                    return await _softFxClient.GetAsync(url);
                else
                    return await _defaultClient.GetAsync(url);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private double GetBid(LiquidityProvider lp, HttpResponseMessage response)
        {
            try
            {
                dynamic quote = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);

                if (response.StatusCode != HttpStatusCode.OK)
                    return double.NaN;
            
                switch (lp)
                {
                    case LiquidityProvider.SoftFx:
                        return quote[0]["BestBid"]["Price"];

                    case LiquidityProvider.Livecoin:
                        return quote["best_bid"];

                    case LiquidityProvider.Tidex:
                        return quote[TidexSymbol]["buy"];

                    case LiquidityProvider.Okex:
                        string okexStr = quote["ticker"]["buy"];
                        return (double.TryParse(okexStr, out double okexBid)) ? okexBid : double.NaN;

                    case LiquidityProvider.Binance:
                        string binanceStr = quote["bidPrice"];
                        return (double.TryParse(binanceStr, out double binanceBid)) ? binanceBid : double.NaN;

                    case LiquidityProvider.Bitfinex:
                        return quote[0];

                    case LiquidityProvider.HitBtc:
                        string hitBtcStr = quote["bid"];
                        return (double.TryParse(hitBtcStr, out double hitBtcBid)) ? hitBtcBid : double.NaN;

                    case LiquidityProvider.Kraken:
                        if (((JArray)quote["error"]).Count != 0)
                            return double.NaN;
                        else
                        {
                            var result = new JObject(quote["result"]);
                            var enumerator = result.Properties().GetEnumerator();
                            enumerator.MoveNext();
                            var krakenSymbol = enumerator.Current.Name;
                            return double.Parse((string)result.GetValue(krakenSymbol).Value<JArray>("b")[0]);
                        }

                    case LiquidityProvider.Kucoin:
                        return quote["data"]["buy"];

                    case LiquidityProvider.Huobi:
                        return quote["tick"]["bid"][0];

                    default:
                        return double.NaN;
                }
            }
            catch(Exception e)
            {
                return double.NaN;
            }
        }

        private double GetAsk(LiquidityProvider lp, HttpResponseMessage response)
        {
            try
            {
                dynamic quote = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);

                if (response.StatusCode != HttpStatusCode.OK)
                    return double.NaN;

                switch (lp)
                {
                    case LiquidityProvider.SoftFx:
                        return quote[0]["BestAsk"]["Price"];

                    case LiquidityProvider.Livecoin:
                        return quote["best_ask"];

                    case LiquidityProvider.Tidex:
                        return quote[TidexSymbol]["sell"];

                    case LiquidityProvider.Okex:
                        string okexStr = quote["ticker"]["sell"];
                        return (double.TryParse(okexStr, out double okexAsk)) ? okexAsk : double.NaN;

                    case LiquidityProvider.Binance:
                        string binanceStr = quote["askPrice"];
                        return (double.TryParse(binanceStr, out double binanceAsk)) ? binanceAsk : double.NaN;

                    case LiquidityProvider.Bitfinex:
                        return quote[2];

                    case LiquidityProvider.HitBtc:
                        string hitBtcStr = quote["ask"];
                        return (double.TryParse(hitBtcStr, out double hitBtcAsk)) ? hitBtcAsk : double.NaN;

                    case LiquidityProvider.Kraken:
                        if (((JArray)quote["error"]).Count != 0)
                            return double.NaN;
                        else
                        {
                            var result = new JObject(quote["result"]);
                            var enumerator = result.Properties().GetEnumerator();
                            enumerator.MoveNext();
                            var krakenSymbol = enumerator.Current.Name;
                            return double.Parse((string)result.GetValue(krakenSymbol).Value<JArray>("a")[0]);
                        }

                    case LiquidityProvider.Kucoin:
                        return quote["data"]["sell"];

                    case LiquidityProvider.Huobi:
                        return quote["tick"]["ask"][0];

                    default:
                        return double.NaN;
                }
            }
            catch(Exception e)
            {
                return double.NaN;
            }
        }

        private void SaveToDB(Dictionary<string, double> dict, int initBarsCount)
        {
            using (var storage = new LevelDbStorage(_dbPath))
            {
                foreach (var lp in dict)
                    using (var collection = storage.GetBinaryCollection(lp.Key, _keySerializer))
                    {
                        collection.Write(Bars[Bars.Count - initBarsCount].OpenTime, GetSegment(lp.Value));
                    }
            }
        }

        public static ArraySegment<byte> GetSegment(double src)
        {
            return new ArraySegment<byte>(BitConverter.GetBytes(src));
        }
    }

    public enum LiquidityProvider { SoftFx, Livecoin, Tidex, Okex, Binance, Bitfinex, HitBtc, Kraken, Kucoin, Huobi };

    public class DateTimeKeySerializer : IKeySerializer<DateTime>
    {
        public int KeySize => 8;

        public DateTime Deserialize(IKeyReader reader)
        {
            var ticks = reader.ReadBeLong();
            return new DateTime(ticks);
        }

        public void Serialize(DateTime key, IKeyBuilder builder)
        {
            builder.WriteBe(key.Ticks);
        }
    }
}