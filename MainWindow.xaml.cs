using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Panuon.UI.Silver;
using System.IO.Ports;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace SerialPortWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : WindowX
    {
        public SerialPort SerialPort = new SerialPort();
        DispatcherTimer Timer = new DispatcherTimer();
        
        Boolean ReceiveHex = false;
        Boolean ReceiveStatus = true;
        Boolean IsAutoSend = false;
        Boolean IsNewLine = false;
        Boolean SendHexStatus = false;
        public MainWindow()
        {
            InitializeComponent();
            SerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            Timer.Tick += new EventHandler(Timer_Tick);
        }
        /// <summary>
        /// //读取下数据并显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (ReceiveStatus)//开启接收
            {
                Thread.Sleep(20);
                byte[] recBuffer = new byte[SerialPort.BytesToRead];//接收数据缓存
                SerialPort.Read(recBuffer, 0, recBuffer.Length);//读取数据
                //中断线程不能对WPF进行操作
                Dispatcher.Invoke(
                new Action(
                     delegate
                     {
                         if (ReceiveHex)//接收模式为ASCII文本模式
                            {
                             string recData = Encoding.ASCII.GetString(recBuffer);

                             ReceiveDataShow(recData);
                            }
                         else
                         {
                             StringBuilder recBuffer16 = new StringBuilder();//定义16进制接收缓存
                             for (int i = 0; i < recBuffer.Length; i++)
                                {
                                     recBuffer16.AppendFormat("{0:X2}" + " ", recBuffer[i]);
                                 }
                             ReceiveDataShow(recBuffer16.ToString());
                         }
                     }
                )
                );
            }
            else//暂停接收
            {
                SerialPort.DiscardInBuffer();//清接收缓存
            }
        }
        /// <summary>
        /// 串口查找点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchCOM_Click(object sender, RoutedEventArgs e)
        {
            SerialPortCom.Items.Clear();
            string[] Ports = SerialPort.GetPortNames();
            if (Ports.Length == 0)
            {

               MessageBoxX.Show("未能查找到Port口,请检查USB", "Tips", Application.Current.MainWindow);
            }
            else
            {
                for (int index = 0; index < Ports.Length; index++)
                  {
                    SerialPortCom.Items.Add(Ports[index]);
                }
                SerialPortCom.SelectedIndex = 0;
            }


        }
        /// <summary>
        /// 设置串口的参数
        /// </summary>
        /// <param name="strComPort">串口类</param>
        /// <param name="strComName">COM口</param>
        /// <param name="strBaudRate">波特率</param>
        /// <param name="strDataBits">数据位</param>
        /// <param name="strParity">校验位</param>
        /// <param name="strStopBits">停止位</param>
        public void SerialPort_Config(SerialPort strComPort, string strComName, string strBaudRate, string strDataBits, string strParity, string strStopBits)
        {
            //strComPort.PortName = "COM18";
            strComPort.PortName = strComName;
            //strComPort.BaudRate = (SerialPortBaudRates)Enum.Parse(typeof(SerialPortBaudRates), strBaudRate);
            strComPort.BaudRate = Convert.ToInt32(strBaudRate);
            strComPort.Parity = (Parity)Enum.Parse(typeof(Parity), strParity);
            //strComPort.DataBits = (SerialPortDatabits)Enum.Parse(typeof(SerialPortDatabits), strDataBits);
            strComPort.DataBits = Convert.ToInt32(strDataBits);
            strComPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), strStopBits);

            //设置串口数据编码格式
            strComPort.Encoding = System.Text.Encoding.GetEncoding(28591);
            //strComPort.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
        }
        /// <summary>
        /// 串口发送函数
        /// </summary>
        /// <param name="msg"></param>
        private bool SerialPortSendCommand(string msg,bool NewLine_Status=false)
        {
            if (SerialPort.IsOpen)
            {
                if (NewLine_Status == true)
                {
                    SerialPort.Write(msg+ NewLine.Text);
                    //DUTSerialPort.Write(msg);
                    return true;
                }
                else
                {
                    SerialPort.Write(msg);
                    //DUTSerialPort.Write(msg);
                     return true;
                }

            }
            else
            {
                MessageBoxX.Show("串口未打开", "Tips", Application.Current.MainWindow);
                return false;
            }
        }
        /// <summary>
        /// 串口打开和关闭按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPortOpen_Click(object sender, RoutedEventArgs e)
        {
            if(SerialPortOpen_txt.Text== "关闭串口")
            {
                SerialPort.Close();
                SerialPortOpen.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c2c2c2"));
                SerialPortOpen_txt.Text = "打开串口";
                SerialPortOpen_txt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#272727"));
                SerialPortCom.IsEnabled = true;
                SerialPortBaudRate.IsEnabled = true;
                SerialPortDataBits.IsEnabled = true;
                SerialPortParity.IsEnabled = true;
                SerialPortStopBits.IsEnabled = true;
                SearchCOM.IsEnabled = true;
            }
            else
            {
                     if (SerialPortCom.Text == null|| SerialPortCom.Text=="")
            {
                MessageBoxX.Show("未能查找到Port口,请先查找", "Tips", Application.Current.MainWindow);
            }
                     else
            {
                    try
                    {
                        SerialPort_Config(SerialPort, SerialPortCom.Text, SerialPortBaudRate.Text, SerialPortDataBits.Text, SerialPortParity.Text, SerialPortStopBits.Text);
                        SerialPort.Open();
                        SerialPortOpen.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008001"));
                        SerialPortOpen_txt.Text = "关闭串口";
                        SerialPortOpen_txt.Foreground = new SolidColorBrush(Colors.White);
                        SerialPortCom.IsEnabled = false;
                        SerialPortBaudRate.IsEnabled = false;
                        SerialPortDataBits.IsEnabled = false;
                        SerialPortParity.IsEnabled = false;
                        SerialPortStopBits.IsEnabled = false;
                        SearchCOM.IsEnabled = false;
                    }
                    catch
                    {
                        MessageBoxX.Show("Port口打开失败，请检查串口或参数", "Tips", Application.Current.MainWindow);
                    }
                

            }
            }
        }
        /// <summary>
        /// 发送按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
          private void Send_Click(object sender, RoutedEventArgs e)
        {
           String Send_Data = Data_Send.Text;
            if (IsAutoSend)
            {
                Timer.IsEnabled = true;
                Timer.Start();
            }
            else
            {
                bool SendResult= SerialPortSendCommand(Send_Data, IsNewLine);
                  if (SendResult)
                  {
                         SendDataShow(Send_Data);
                  }
            }

            
        }
        #region 数据接收和数据发送显示不同的颜色
        /// <summary>
        /// 
        /// </summary>
        /// <param name="txt"></param>
        private void SendDataShow(string txt)
        {

            Run run = new Run() { Text = ">> " + txt + "\r\n", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008001")) };
            Paragraph.Inlines.Add(run);
         //   Data_Record.ScrollToEnd();

        }
        private void ReceiveDataShow(string txt)
        {
            Run run = new Run() { Text = "<< " + txt + "\r\n", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f1f1f"))};
            Paragraph.Inlines.Add(run);
            Data_Record.ScrollToEnd();
        }
        #endregion
        #region  TextBox:Data_Send 聚焦和失去焦点时改变外框颜色
        private void Data_Send_LostFocus(object sender, RoutedEventArgs e)
        {
            Data_Send.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#919191"));
         }
        private void Data_Send_GotFocus(object sender, RoutedEventArgs e)
        {
            Data_Send.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078c7"));
        }
        #endregion
        #region 当窗口关闭时，关闭串口
        /// <summary>
        /// 当窗口关闭时，关闭串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            string strContent = this.SerialPortOpen_txt.Text.ToString();
            if (strContent == "关闭串口")
            {
                SerialPort.Close();
            }

        }

        #endregion

        public string ConvertStringToHex(string strASCII)
        {
            string[] hexValuesSplit = strASCII.Split(' ');
            string charValue = null;
            foreach (String hex in hexValuesSplit)
            {
                int value = Convert.ToInt32(hex, 16);
                charValue += (char)value;
            }
            return charValue;
        }

        public string ConvertHexToString(string HexValue, string separator = null)
        {
            char[] hexValuesSplit = HexValue.ToCharArray();
            string charValue = null;
            foreach (char hex in hexValuesSplit)
            {
                charValue += ((hex & 0xf0) / 16).ToString("x");
                charValue += (hex & 0x0f).ToString("x");
                charValue += ' ';
            }
            return charValue;
        }



        public void Save_Data_Record( )
        {
            SaveFileDialog SaveFileDialog = new SaveFileDialog();
            SaveFileDialog.Title = "请选择文件";
            SaveFileDialog.FileName = "New Log.txt";
            SaveFileDialog.Filter = "文本文件(*.txt)|*.txt|全部文件(*.*)|*.*";
            SaveFileDialog.FilterIndex = 1;
            SaveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            SaveFileDialog.RestoreDirectory = true;
            Nullable<bool> Result = SaveFileDialog.ShowDialog();
            if (Result == true)
            {
                string FileName = SaveFileDialog.FileName;
                using (StreamWriter sw = new StreamWriter(FileName, true))
                {
                    TextRange textRange = new TextRange(Data_Record.Document.ContentStart, Data_Record.Document.ContentEnd);
                    sw.WriteLine(textRange.Text);
                }
            }
        }
        private void Clear_DataRecord_Click(object sender, RoutedEventArgs e)
        {
            Paragraph.Inlines.Clear();

        }

        private void Save_DataRecord_Click(object sender, RoutedEventArgs e)
        {
            Save_Data_Record();
        }

        private void Receive_Hex_Checked(object sender, RoutedEventArgs e)
        {
            ReceiveHex = true;
        }
        private void Receive_Hex_UnChecked(object sender, RoutedEventArgs e)
        {
            ReceiveHex = false;
        }
        
        private void Receive_Stop_Checked(object sender, RoutedEventArgs e)
        {
            ReceiveStatus = false;
        }
        private void Receive_Stop_UnChecked(object sender, RoutedEventArgs e)
        {
            ReceiveStatus = true;
            
        }

        private void SendHex_Checked(object sender, RoutedEventArgs e)
        {
            
            InputLanguageManager.SetInputLanguage(Data_Send, CultureInfo.CreateSpecificCulture("en"));
            SendHexStatus = true;
        }
        private void SendHex_UnChecked(object sender, RoutedEventArgs e)
        {
           
            SendHexStatus = false;
        }
        private void Data_Send_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void Data_Send_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void Data_Send_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string newText = e.Text;
            if (IsHex(newText))
            {
                ReceiveDataShow("hex");
                e.Handled = false;
            }
            else
            {
                ReceiveDataShow("unwwhex");
                e.Handled = true;
            }
        }
        
        public bool IsHex(string str)
        {
            const string PATTERN = @"[A-Fa-f0-9]+$";
            return System.Text.RegularExpressions.Regex.IsMatch(str, PATTERN);
        }
        public bool IsNumAndLetter(string str)
        {
            const string PATTERN = @"^[\u4e00-\u9fa5]{0,}$";
            return System.Text.RegularExpressions.Regex.IsMatch(str, PATTERN);
        }

        private void Send_Timing_Checked(object sender, RoutedEventArgs e)
        {
            IsAutoSend = true;
            SendTiming_Num.IsEnabled = false;
            int Time_Sec = Convert.ToInt32(SendTiming_Num.Text);
            Timer.Interval = new TimeSpan(0, 0, Time_Sec);//设置的间隔
            
        }
        private void Timer_Tick (object sender, EventArgs e) {
          //  bool SendResult = SerialPortSendCommand(Send_Data, IsNewLine);
           // if (SendResult)
           // {
           //     SendDataShow(Send_Data);
         //   }
            ReceiveDataShow("我是时钟"+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        private void Send_Timing_Unchecked(object sender, RoutedEventArgs e)
        {
            IsAutoSend = false;
            SendTiming_Num.IsEnabled = true;

            if (Timer != null && Timer.IsEnabled == true)
            {
                Timer.Stop();
            }
        }

        private void SendTiming_Num_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {    
            e.Handled =!Regex.IsMatch(e.Text, "^([0-9]{1,})$");
        }

        private void NewLine_CB_Checked(object sender, RoutedEventArgs e)
        {
            NewLine.IsEnabled = false;
            IsNewLine = true;
        }
        private void NewLine_CB_Unchecked(object sender, RoutedEventArgs e)
        {
            NewLine.IsEnabled = true;
            IsNewLine = false;
        }
        
    }
}
