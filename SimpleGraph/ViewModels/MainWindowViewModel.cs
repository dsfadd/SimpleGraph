using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SimpleGraph.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleGraph.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public Axis[] XAxes { get; set; }
        private ObservableCollection<double> _askValues;

        public ObservableCollection<double> AskValues
        {
            get
            {
                return _askValues;
            }
            set
            {
                lock (_lock1)
                {
                    _askValues = value;
                }
            }
        }

        public ObservableCollection<ISeries> Series { get; set; }
        public int SelectedTick { get; set; } = 5;
        public string SelectedTicker { get; set; } = Tickers[0];
        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _lock = new object();
        private readonly object _lock1 = new object();
        public static string[] Tickers { get; set; }
        private ObservableCollection<double> _bidValues;

        public ObservableCollection<double> BidValues
        {
            get
            {
                return _bidValues;
            }
            set
            {
                lock (_lock1)
                {
                    _bidValues = value;
                }
            }
        }

        static MainWindowViewModel()
        {
            Tickers = new string[]
        {
                "ETHBTC",
                "BNBBTC",
                "SNTETH",
                "GASBTC",
                "EOSETH",
                "MCOETH",
                "BTCUSDT",
                "ETHUSDT"
        };
        }

        private Axis customAxis;

        public MainWindowViewModel()
        {
            BidValues = new ObservableCollection<double>();
            AskValues = new ObservableCollection<double>();
            customAxis = new Axis();
            XAxes = new Axis[]
            {
               customAxis
            };
            customAxis.Labels = new List<string>();
            _cancellationTokenSource = new CancellationTokenSource();
            Series = new ObservableCollection<ISeries>
    {
        new LineSeries<double>
        {
             Stroke=new SolidColorPaint(SKColors.Red),
            Values = BidValues,
            Name = "Bid",
            GeometrySize=1
        },
        new LineSeries<double>
        {
          Stroke=new SolidColorPaint(SKColors.Yellow),
            Values = AskValues,
            Name = "Ask",
            GeometrySize=1
        }
    };
        }

        private void BinanceWSS_OnMessageReceived(string message, DateTime receivedTime)
 {
     if (!TickerDepthMessage.TryParse(message)) return;
         
             var parsedMessage = TickerDepthMessage.Parse(message);
             lock (_lock)
             {
                 if (AskValues.Count > SelectedTick)
                 {
                     AskValues.RemoveAt(0);
                     AskValues.Add(parsedMessage.AskPrice);

                     BidValues.RemoveAt(0);
                     BidValues.Add(parsedMessage.BidPrice);

                     customAxis.Labels.RemoveAt(0);
                     customAxis.Labels.Add(receivedTime.ToString("HH:mm:ss.fff"));
                 }
                 else
                 {
                     AskValues.Add(parsedMessage.AskPrice);
                     BidValues.Add(parsedMessage.BidPrice);
                     customAxis.Labels.Add(receivedTime.ToString("HH:mm:ss.fff"));
                 }
             }
 }

        private void ClearLineSeriesValues()
        {
            if (BidValues.Count >= 1)
            {
                AskValues.Clear();
                BidValues.Clear();
                customAxis.Labels.Clear();
            }
        }

        public void Connect()
        {
            var cancellationToken = _cancellationTokenSource.Token;

            Task.Run(async () =>
          {
              BinanceWebSocketService binanceWSS = new BinanceWebSocketService();
              try
              {
                  binanceWSS.MessageReceived += BinanceWSS_OnMessageReceived;
                  await binanceWSS.ConnectAsync();
                  await binanceWSS.SubscribeAsync(SelectedTicker);
                  await binanceWSS.ReceiveMessagesAsync(cancellationToken);
              }
              catch
              {
                  binanceWSS.Dispose();
              }
          }, CancellationToken.None);
        }

        public void Disconnect()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            ClearLineSeriesValues();
        }
    }
}
