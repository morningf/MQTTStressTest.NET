using MQTTnet.Client;
using MQTTnet;
using System.Text;
using System.Windows;
using System.Timers;
using System.Collections.Concurrent;
using MQTTnet.Server;
using System.ComponentModel;

namespace MQTTStressTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;


            testTimer = new System.Timers.Timer();
            testTimer.Elapsed += TestTimerTick;
        }

        private readonly MqttFactory mqttFactory = new MqttFactory();
        private IMqttClient? mqttClient;
        private ConcurrentDictionary<string, long> testMsgs = new ConcurrentDictionary<string, long>();

        private const string TEST_TOPIC = "test/t1";

        private readonly System.Timers.Timer testTimer;
        private int testCount = 0;
        private long totalTime = 0;
        private MQTTnet.Protocol.MqttQualityOfServiceLevel qos = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce;


        bool connected = false;
        public bool IsConnected { get { return connected; } }
        public bool IsNotConnected { get { return !connected; } }
        private void SetConnected(bool connected)
        {
            this.connected = connected;
            OnPropertyChanged("IsConnected");
            OnPropertyChanged("IsNotConnected");
        }

        bool running = false;
        public bool IsRunning { get { return running; } }
        public bool IsNotRunning { get { return !running; } }
        private void SetRunning(bool running)
        {
            this.running = running;
            OnPropertyChanged("IsRunning");
            OnPropertyChanged("IsNotRunning");
        }

        private async void TestTimerTick(object? sender, ElapsedEventArgs e)
        {
            if (mqttClient != null)
            {
                testMsgs.TryAdd(testCount.ToString(), DateTime.Now.Ticks);
                await mqttClient.PublishStringAsync(TEST_TOPIC, testCount.ToString(), qos);
                R.Slog($"publish {testCount.ToString()} with qos: {(int)qos}");
                testCount++;
            }
        }

        private async Task MessageReceivedHandler(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            if (!TEST_TOPIC.Equals(topic)) {
                return;
            }
            var segment = e.ApplicationMessage.PayloadSegment;
            string payload = string.Empty;
            if (segment.Array != null)
            {
                payload = Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
            }

            if (testMsgs.TryRemove(payload, out long value)) {
                var leftCount = testMsgs.Count;
                var allCount = testCount;
                var ms = (DateTime.Now.Ticks - value) / 10000;
                totalTime += ms;
                R.Slog($"已发送 {allCount} 未收到回复 {leftCount} 当次延时{ms}ms 平均延时 {totalTime / (allCount - leftCount)}ms");
            }
        }

        private async Task DisconnectedHandler(MqttClientDisconnectedEventArgs args)
        {
            _ = Dispatcher.BeginInvoke(new Action(() =>
            {
                R.Slog("连接断开");
                SetConnected(false);
                testTimer.Stop();
                SetRunning(false);
            }));
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (connected)
            {
                if (mqttClient != null)
                {
                    mqttClient.TryDisconnectAsync();
                }
            }
            else
            {
                R.ShowError("未连接，不需要断开");
                return;
            }
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            var address = txtAddress.Text;
            if (string.IsNullOrEmpty(address)) {
                R.ShowError("地址不能为空");
                return;
            }

            if (connected)
            {
                R.ShowError($"不需要再次连接");
                return;
            }

            if (mqttClient == null)
            {
                mqttClient = mqttFactory.CreateMqttClient();
                mqttClient.ApplicationMessageReceivedAsync += MessageReceivedHandler;
                mqttClient.DisconnectedAsync += DisconnectedHandler;
            }

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(address, 1883)
                .WithClientId("StressTest.NET")
                .WithCleanSession(false)
                .Build();

            try
            {
                var connectResult = await mqttClient.ConnectAsync(mqttClientOptions);
                if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                {
                    await mqttClient.SubscribeAsync(TEST_TOPIC);
                    R.Slog("连接成功");
                    SetConnected(true);
                }
                else
                {
                    R.ShowError("连接失败");
                }
            }
            catch
            {
                R.ShowError("连接失败");
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            int interval = 0;
            if (!int.TryParse(txtInterval.Text, out interval))
            {
                R.ShowError("间隔时间输入错误");
                return;
            }
            if (!connected) {
                R.ShowError("未连接");
                return;
            }

            switch (cbQos.SelectedIndex)
            {
                case 0:
                    qos = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce;
                    break;
                case 1:
                    qos = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce;
                    break;
                case 2:
                    qos = MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce;
                    break;
                default:
                    break;
            }

            totalTime = 0;
            testCount = 0;
            testTimer.Interval = interval;
            testTimer.Start();
            SetRunning(true);
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            testTimer.Stop();
            SetRunning(false);
        }

        private void ckLogOutput_Checked(object sender, RoutedEventArgs e)
        {
            var c = ckLogOutput.IsChecked ?? true;
            R.IsSlog = c;
        }
    }
}