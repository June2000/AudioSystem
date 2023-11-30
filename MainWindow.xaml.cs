using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using System.IO.Ports;
using System.Windows.Threading;

using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace AudioSystem
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool UserID { set; get; }
        public MainWindow()
        {
            InitializeComponent();
            GetSysType();

            //启动一个定时器
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
            dispatcherTimer.Start();
            
        }

        private UdpClient udpcSend ;
        private UdpClient udpcRecv;
        Thread thrRecv; //网络接收线程
        /// 开关：在监听UDP报文阶段为true，否则为false  
        /// </summary>  
        bool IsUdpcRecvStart = false;
        private static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取本机IP出错:" + ex.Message);
                return "";
            }
        }
        public static string AccessConnStr = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=MyLocalData.mdb;Jet OLEDB:Database Password=CH123321";

        private void GetAccessData(int CurrDex)
        {
            try
            {
                OleDbConnection olecon = new OleDbConnection(AccessConnStr);
                olecon.Open();
                string selstr = @"select * from userdata where ID=" + CurrDex + " ";
                OleDbCommand olecmd = new OleDbCommand(selstr, olecon);
                OleDbDataReader olereader = olecmd.ExecuteReader();     //执行查询
                if (olereader.Read())
                {
                    AKnob1.Value = (int)olereader["mic1vol"];
                    AKnob2.Value = (int)olereader["mic2vol"];
                    AKnob3.Value = (int)olereader["aux1vol"];
                    AKnob4.Value = (int)olereader["aux2vol"];
                    TSlider1.Value = (int)olereader["70hzdata"];
                    TSlider2.Value = (int)olereader["180hzdata"];
                    TSlider3.Value = (int)olereader["320hzdata"];
                    TSlider4.Value = (int)olereader["600hzdata"];
                    TSlider5.Value = (int)olereader["1khzdata"];
                    TSlider6.Value = (int)olereader["3khzdata"];
                    TSlider7.Value = (int)olereader["6khzdata"];
                    TSlider8.Value = (int)olereader["12khzdata"];
                    TSlider9.Value = (int)olereader["14khzdata"];
                    TSlider10.Value = (int)olereader["16khzdata"];
                    TSlider11.Value = (int)olereader["lopassdata"];
                    TSlider12.Value = (int)olereader["hipassdata"];
                    TimeBox.Text = (string)olereader["timedata"].ToString();
                    IDBox.Text = (string)olereader["sysid"].ToString();
                    ComboBox4.SelectedIndex = (int)olereader["noisedata"];
                    ComboBox5.SelectedIndex = (int)olereader["pressdata"];
                    KButton3.Value = (bool)olereader["afcstate"];
                    KButton4.Value = (bool)olereader["aecstate"];
                    KButton5.Value = (bool)olereader["micmute"];
                    KButton6.Value = (bool)olereader["sysmute"];
                    KButton7.Value = (bool)olereader["powerstate"];
                    KButton8.Value = (bool)olereader["lockstate"];
                    KButton9.Value = (bool)olereader["dc48state"];
                }
                olereader.Close();
                olecon.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show("数据库读取错误"+ex.Message,"错误");
            }
            
        }
        private int ComboBox6SelectedIndex = 0;
        private int NoiseSelectedIndex = 0;
        private int PressSelectedIndex = 0;
        

        private void AKnob1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x11;
                NeedSentDataDelay = 3;
            } 
        }
        private void AKnob2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x13;
                NeedSentDataDelay = 3;
            } 
        }
        private void AKnob3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x12;
                NeedSentDataDelay = 3;
            } 
        }
        private void AKnob4_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x14;
                NeedSentDataDelay = 3;
            } 
        }

        private void KButton7_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if (KButton7.Value == false) AKnob1.Value = 20;
            //else AKnob1.Value = 60;
        }

        private void ComboBox3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox3.SelectedIndex == 0)
            {
                ComboBox1.Visibility = Visibility.Visible;
                IPname.Visibility = Visibility.Collapsed;
            }
            else
            {
                ComboBox1.Visibility = Visibility.Collapsed;
                IPname.Visibility = Visibility.Visible;
            }
        }

        private void PassWord_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //修改密码
            修改密码 pass = new 修改密码();
            pass.Show();
        }

        private void ComboBox7_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (ComboBox7.SelectedIndex == 0) return; //为0为默认值 不保持和更改数据
            //读取数据库数据
            GetAccessData(ComboBox7.SelectedIndex);
            //下发
            //if(mySerialPort.IsOpen)
            {
                #region 下发数据
                byte[] sendData = new byte[23];
                sendData[0] = 0xaa;
                sendData[1] = 0x30;

                byte[] bytes = BitConverter.GetBytes(79 - AKnob1.Value);
                sendData[2] = bytes[0];
                bytes = BitConverter.GetBytes(79 - AKnob2.Value);
                sendData[3] = bytes[0];
                bytes = BitConverter.GetBytes(79 - AKnob3.Value);
                sendData[4] = bytes[0];
                bytes = BitConverter.GetBytes(79 - AKnob4.Value);
                sendData[5] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider1.Value + 12);
                sendData[6] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider2.Value + 12);
                sendData[7] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider3.Value + 12);
                sendData[8] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider4.Value + 12);
                sendData[9] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider5.Value + 12);
                sendData[10] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider6.Value + 12);
                sendData[11] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider7.Value + 12);
                sendData[12] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider8.Value + 12);
                sendData[13] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider9.Value + 12);
                sendData[14] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider10.Value + 12);
                sendData[15] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider11.Value);
                sendData[16] = bytes[0];
                bytes = BitConverter.GetBytes(TSlider12.Value);
                sendData[17] = bytes[0];
                bytes = BitConverter.GetBytes(ComboBox4.SelectedIndex);
                sendData[18] = bytes[0];
                bytes = BitConverter.GetBytes(ComboBox5.SelectedIndex);
                sendData[19] = bytes[0];

                byte bytess = Convert.ToByte(TimeBox.Text);
                sendData[20] = bytess;

                int StateByte = 0;
                if (KButton3.Value == true) StateByte += 0x01;
                if (KButton4.Value == true) StateByte += 0x02;
                if (KButton5.Value == true) StateByte += 0x04;
                if (KButton6.Value == true) StateByte += 0x08;
                if (KButton7.Value == true) StateByte += 0x10;
                if (KButton8.Value == true) StateByte += 0x20;
                if (KButton9.Value == true) StateByte += 0x40;
                bytes = BitConverter.GetBytes(StateByte);
                sendData[21] = bytes[0];
                sendData[22] = 0xff;

                if(ComboBox3.SelectedIndex==0) mySerialPort.Write(sendData, 0, sendData.Length);
                else  UDP_SentMessage(sendData);

                #endregion
            }
            

        }

        private void GetSysType()
        {
            try
            {
                OleDbConnection olecon = new OleDbConnection(AccessConnStr);
                olecon.Open();
                string selstr = @"select * from texttype where ID=1 ";
                OleDbCommand olecmd = new OleDbCommand(selstr, olecon);
                OleDbDataReader olereader = olecmd.ExecuteReader();     //执行查询
                if (olereader.Read())
                {
                    TypeText.Text = olereader["usermodel"].ToString();
                }
                olereader.Close();
                olecon.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("错误" + ex.Message);
            }
        }

        private void SaveKey_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string Updsq1 = "UPDATE userdata SET  mic1vol='" + AKnob1.Value + "',";
                Updsq1 += "mic2vol='" + AKnob2.Value + "',";
                Updsq1 += "aux1vol='" + AKnob3.Value + "',";
                Updsq1 += "aux2vol='" + AKnob4.Value + "',";
                Updsq1 += "70hzdata='" + TSlider1.Value + "',";
                Updsq1 += "180hzdata='" + TSlider2.Value + "',";
                Updsq1 += "320hzdata='" + TSlider3.Value + "',";
                Updsq1 += "600hzdata='" + TSlider4.Value + "',";
                Updsq1 += "1khzdata='" + TSlider5.Value + "',";
                Updsq1 += "3khzdata='" + TSlider6.Value + "',";
                Updsq1 += "6khzdata='" + TSlider7.Value + "',";
                Updsq1 += "12khzdata='" + TSlider8.Value + "',";
                Updsq1 += "14khzdata='" + TSlider9.Value + "',";
                Updsq1 += "16khzdata='" + TSlider10.Value + "',";
                Updsq1 += "lopassdata='" + TSlider11.Value + "',";
                Updsq1 += "hipassdata='" + TSlider12.Value + "',";
                Updsq1 += "timedata='" + Convert.ToInt32(TimeBox.Text) + "',";
                Updsq1 += "sysid='" + Convert.ToInt32(IDBox.Text) + "',";
                Updsq1 += "noisedata='" + NoiseSelectedIndex + "',";
                Updsq1 += "pressdata='" + PressSelectedIndex + "',";
                Updsq1 += "afcstate=" + KButton3.Value + ",";
                Updsq1 += "aecstate=" + KButton4.Value + ",";
                Updsq1 += "micmute=" + KButton5.Value + ",";
                Updsq1 += "sysmute=" + KButton6.Value + ",";
                Updsq1 += "powerstate=" + KButton7.Value + ",";
                Updsq1 += "lockstate=" + KButton8.Value + ",";
                Updsq1 += "dc48state=" + KButton9.Value + " ";


                Updsq1 += " WHERE [ID]=" + ComboBox6SelectedIndex + "";

                OleDbConnection olecon = new OleDbConnection(AccessConnStr);
                olecon.Open();
                OleDbCommand Updcmd = new OleDbCommand(Updsq1, olecon);
                Updcmd.ExecuteNonQuery();
                olecon.Close();
                MessageBox.Show("数据保存成功！","提示");
            
            }
            catch(Exception ex)
            {
                MessageBox.Show("数据库保持异常："+ex.Message,"错误");
            }
        }

        private void ComboBox6_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox6SelectedIndex = ComboBox6.SelectedIndex+1;
        }

        private void ComboBox4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NoiseSelectedIndex = ComboBox4.SelectedIndex;
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x04;
                NeedSentDataDelay = 2;
            }
        }

        private void ComboBox5_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PressSelectedIndex = ComboBox5.SelectedIndex;
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x05;
                NeedSentDataDelay = 2;
            }
        }

        private void DownType_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string Updsq1 = "UPDATE texttype SET  usermodel='" + TypeText.Text + "'";
        
                Updsq1 += " WHERE [ID]=1";

                OleDbConnection olecon = new OleDbConnection(AccessConnStr);
                olecon.Open();
                OleDbCommand Updcmd = new OleDbCommand(Updsq1, olecon);
                Updcmd.ExecuteNonQuery();
                olecon.Close();

                TypeText.IsReadOnly = true;

                MessageBox.Show("型号保存成功！","提示");
            
            }
            catch(Exception ex)
            {
                MessageBox.Show("数据库保持异常："+ex.Message,"错误");
            }
        }

        private void TypeText_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TypeText.IsReadOnly = false;
        }

        private void TimeBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9.-]+");

            e.Handled = re.IsMatch(e.Text);
        }

        private void IDBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9.-]+");

            e.Handled = re.IsMatch(e.Text);
        }



        SerialPort mySerialPort = new SerialPort();

        private void SystemWind_Loaded(object sender, RoutedEventArgs e)
        {
            string[] portsName = SerialPort.GetPortNames();
            Array.Sort(portsName);
            ComboBox1.ItemsSource = portsName;

            if (portsName.Length>0)
            {
                 ComboBox1.Text = Convert.ToString(ComboBox1.Items[0]);

                 mySerialPort.DataReceived += new SerialDataReceivedEventHandler(this.mySerialPort_DataReceived);
            }

            //获取保存的IP
            try
            {
                OleDbConnection olecon = new OleDbConnection(AccessConnStr);
                olecon.Open();
                string selstr = @"select * from uinfo where ID=2 ";
                OleDbCommand olecmd = new OleDbCommand(selstr, olecon);
                OleDbDataReader olereader = olecmd.ExecuteReader();     //执行查询
                if (olereader.Read())
                {
                    IPname.Text = olereader["myuser"].ToString();
                    UDPPort.Text = olereader["mypassword"].ToString();
                }
                olereader.Close();
                olecon.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("查询IP错误" + ex.Message);
            }
            
        }
        private bool LinkFlag = false;
        private void KButton1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ComboBox3.SelectedIndex == 0)
            {
                #region  //串口通讯
                if (mySerialPort.IsOpen)
                {
                    mySerialPort.Close();
                    ComboBox2.IsEnabled = true;
                    ComboBox1.IsEnabled = true;
                    KButton1.Value = false;
                    KButton2.Value = false;
                    ComboBox3.IsEnabled = true;

                }
                else
                {
                    mySerialPort.PortName = ComboBox1.Text;
                    mySerialPort.BaudRate = Convert.ToInt32(ComboBox2.Text);
                    mySerialPort.Parity = Parity.None;
                    mySerialPort.StopBits = StopBits.One;

                    try
                    {
                        mySerialPort.Open();
                    }
                    catch
                    {
                        MessageBox.Show("串口被占用！", "警告");
                        KButton1.Value = false;
                        return;
                    }

                    ComboBox2.IsEnabled = false;
                    ComboBox1.IsEnabled = false;
                    KButton1.Value = true;
                    KButton2.Value = false;
                    ComboBox3.IsEnabled = false;
                }
                #endregion
            }
            else
            {
                #region 网络通讯

                try
                {
                    if (IPname.Text == "" || UDPPort.Text == "")
                    {
                        MessageBox.Show("远程IP或端口号不能为空！");
                      
                        KButton1.Value = false;
                        KButton2.Value = false;
                        ComboBox3.IsEnabled = true;

                        return;

                    }

                    if (!IsUdpcRecvStart) // 未监听的情况，开始监听  
                    {
                        IPEndPoint localIpep = new IPEndPoint(IPAddress.Parse(GetLocalIP()), 1200);    // 本机IP和监听端口号 
                        udpcRecv = new UdpClient(localIpep);
                        thrRecv = new Thread(UDPReceiveMessage);
                        thrRecv.Start();
                        IsUdpcRecvStart = true;

                        UDP_SentIP = IPname.Text;
                        UDP_SentPort = UDPPort.Text;

                        IPname.IsEnabled = false;
                        UDPPort.IsEnabled = false;
                        KButton1.Value = true;
                        KButton2.Value = false;
                        ComboBox3.IsEnabled = false;


                        //保存IP和端口号
                        try
                        {
                            OleDbConnection olecon = new OleDbConnection(AccessConnStr);
                            olecon.Open();
                            string Updsql = "UPDATE UINFO SET myuser='" + IPname.Text.ToString() + "',mypassword='" + UDPPort.Text.ToString() + "' WHERE [ID]=2";

                            OleDbCommand Updcmd = new OleDbCommand(Updsql, olecon);
                            Updcmd.ExecuteNonQuery();
                            olecon.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("保存IP错误" + ex.Message);
                        }

                    }
                    else // 正在监听的情况，终止监听  
                    {
                        thrRecv.Abort(); // 必须先关闭这个线程，否则会异常  
                        udpcRecv.Close();
                        IsUdpcRecvStart = false;

                        IPname.IsEnabled = true;
                        UDPPort.IsEnabled = true;
                        KButton1.Value = false;
                        KButton2.Value = false;
                        ComboBox3.IsEnabled = true;

                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "网络配置错误提示！");
                }

                #endregion
            }
            
        }

        private int UDP_RevDataLeng = 0;

        private void UDPReceiveMessage(object obj)
        {
            IPEndPoint remoteIpep = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                
                byte[] bytRecv = udpcRecv.Receive(ref remoteIpep); //获取UDP数据

                //模块单个字节上报
                //if (LinkFlag == true)
                //{
                //    if (bytRecv[0] == 0xfa)
                //    {
                //        RevUartData[0] = 0xfa;
                //        UDP_RevDataLeng = 1;
                //    }
                //    else
                //    {
                //        RevUartData[UDP_RevDataLeng] = bytRecv[0];
                //        UDP_RevDataLeng++;
                //    }

                //    if (UDP_RevDataLeng == 23)
                //    {
                //        if (RevUartData[1] == 0x02) //联机上报数据
                //        {
                //            NetequipmentDelay = 0;
                //            NetequipmentFlag = true;
                //            EnSentDataDelay = 10;
                //        }
                //    }
                //}

                //模块整体上报
                if (LinkFlag == true)
                {
                    if (bytRecv[0] == 0xfa && bytRecv.Length == 23)
                    {
                        if (bytRecv[1] == 0x02) //联机上报数据
                        {
                            NetequipmentDelay = 0;
                            NetequipmentFlag = true;
                            #region 下位机数据

                            for (int i = 0; i < bytRecv.Length; i++)
                            {
                                RevUartData[i] = bytRecv[i];
                            }
                            EnSentDataDelay = 10;

                            #endregion
                        }
                    }
                }

            }
        }
        private int EnSentDataDelay = 0;
        private static int [] RevUartData=new int [100];
        private void mySerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {

            int n = mySerialPort.BytesToRead;
            if (n == 0) return;
            byte[] buf = new byte[n];
            mySerialPort.Read(buf, 0, n);
            if (LinkFlag == false) return;
            if (buf[0] != 0xfa) return;
            else
            {
                try
                {
                    if (buf.Length < 2) return;
                    if (buf[1] == 0x02) //联机上报数据
                    {
                        NetequipmentDelay = 0;
                        NetequipmentFlag = true;
                        #region 下位机数据

                        for (int i = 0; i < buf.Length; i++)
                        {
                            RevUartData[i] = buf[i];
                        }
                        #endregion

                        EnSentDataDelay = 10; //延时允许发送数据
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("接收数据错误："+ex.Message);
                }
               
            }
            
        }

        private string UDP_SentIP;
        private string UDP_SentPort;
        private void UDP_SentMessage(byte [] sentdat) //udp发送函数
        {
            thrRecv.Abort(); // 必须先关闭这个线程，否则会异常  
            udpcRecv.Close();

            IPEndPoint localIpep = new IPEndPoint(IPAddress.Parse(GetLocalIP()), 1200); // 本机IP，指定的端口号  
            udpcSend = new UdpClient(localIpep);
            Thread thrSend = new Thread(SendMessage);
            thrSend.Start(sentdat); 
        }

        private void SendMessage(object obj)
        {
            byte[] sendbytes = (byte[])obj;
            //IPEndPoint remoteIpep = new IPEndPoint(IPAddress.Parse(IPname.Text), Convert.ToInt32(UDPPort.Text, 10));//发送到的端口号以及IP地址            

            IPEndPoint remoteIpep = new IPEndPoint(IPAddress.Parse(UDP_SentIP), Convert.ToInt32(UDP_SentPort, 10));//发送到的端口号以及IP地址  
            udpcSend.Send(sendbytes, sendbytes.Length, remoteIpep);
            udpcSend.Close();
            //恢复接收
            IPEndPoint localIpep = new IPEndPoint(IPAddress.Parse(GetLocalIP()), 1200);    // 本机IP和监听端口号 
            udpcRecv = new UdpClient(localIpep);
            thrRecv = new Thread(UDPReceiveMessage);
            thrRecv.Start();
        }

        private int NeedSentDataDelay=0;
        private byte NeedSentDataCmd;
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (EnSentDataDelay > 0) EnSentDataDelay--;//数据上报  禁止发送数据

            if (NetequipmentDelay > 0) NetequipmentDelay--;
            if (NetequipmentDelay == 1) 
            { 
                KButton2.Value = false;
                MessageBox.Show("联机失败,请检查设备是否在线！","警告");
            }
            //解析串口数据
            if (NetequipmentFlag==true)
            {
                #region 读取数据
                AKnob1.Value = 79- RevUartData[2] ;
                AKnob2.Value = 79 - RevUartData[3];
                AKnob3.Value = 79 - RevUartData[4];
                AKnob4.Value = 79 - RevUartData[5];
                TSlider1.Value = RevUartData[6] - 12;
                TSlider2.Value = RevUartData[7] - 12;
                TSlider3.Value = RevUartData[8] - 12;
                TSlider4.Value = RevUartData[9] - 12;
                TSlider5.Value = RevUartData[10] - 12;
                TSlider6.Value = RevUartData[11] - 12;
                TSlider7.Value = RevUartData[12] - 12;
                TSlider8.Value = RevUartData[13] - 12;
                TSlider9.Value = RevUartData[14] - 12;
                TSlider10.Value = RevUartData[15] - 12;
                TSlider11.Value = RevUartData[16];
                TSlider12.Value = RevUartData[17];
                ComboBox4.SelectedIndex = RevUartData[18];
                ComboBox5.SelectedIndex = RevUartData[19];
                TimeBox.Text = RevUartData[20].ToString();

                if ((RevUartData[21] & 0x01) == 0x01) KButton3.Value = true;
                else KButton3.Value = false;
                if ((RevUartData[21] & 0x02) == 0x02) KButton4.Value = true;
                else KButton4.Value = false;
                if ((RevUartData[21] & 0x04) == 0x04) KButton5.Value = true;
                else KButton5.Value = false;
                if ((RevUartData[21] & 0x08) == 0x08) KButton6.Value = true;
                else KButton6.Value = false;
                if ((RevUartData[21] & 0x10) == 0x10) KButton7.Value = true;
                else KButton7.Value = false;
                if ((RevUartData[21] & 0x20) == 0x20) KButton8.Value = true;
                else KButton8.Value = false;
                if ((RevUartData[21] & 0x40) == 0x40) KButton9.Value = true;
                else KButton9.Value = false;

                NetequipmentFlag = false;
                #endregion
            }

            #region //要发送数据 处理

            byte[] bytes = new byte[4];
            byte[] sendData = new byte[4];

            if (NeedSentDataDelay > 0) NeedSentDataDelay--;
            if (NeedSentDataDelay == 1)
            {
                if (EnSentDataDelay > 0) //禁止下发数据
                {
                    NeedSentDataDelay = 0;
                    NeedSentDataCmd = 0;
                }

                sendData[0] = 0xaa;
                sendData[3] = 0xff;
                switch (NeedSentDataCmd)
                {
                    case 0x01:
                        sendData[1] = 0x01;
                        if (KButton7.Value == true) sendData[2] = 0x01;
                        else sendData[2] = 0x00;
                        break;
                    case 0x02:
                        sendData[1] = 0x02;
                        bytes = BitConverter.GetBytes(TSlider12.Value);
                        sendData[2] = bytes[0];
                        break;
                    case 0x03:
                        sendData[1] = 0x03;
                        bytes = BitConverter.GetBytes(TSlider11.Value);
                        sendData[2] = bytes[0];
                        break;
                    case 0x04:
                        sendData[1] = 0x04;
                        bytes = BitConverter.GetBytes(ComboBox4.SelectedIndex);
                        sendData[2] = bytes[0];
                        break;
                    case 0x05:
                        sendData[1] = 0x05;
                        bytes = BitConverter.GetBytes(ComboBox5.SelectedIndex);
                        sendData[2] = bytes[0];
                        break;
                    case 0x06:
                        sendData[1] = 0x06;
                        if (KButton8.Value == true) sendData[2] = 0x01;
                        else sendData[2] = 0x00;
                        break;
                    case 0x07:
                        sendData[1] = 0x07;
                        bytes = BitConverter.GetBytes(TSlider1.Value + 12);
                        sendData[2] = bytes[0];
                        break;
                    case 0x08:
                        sendData[1] = 0x08;
                        bytes = BitConverter.GetBytes(TSlider2.Value + 12);
                        sendData[2] = bytes[0];
                        break;
                    case 0x09:
                        sendData[1] = 0x09;
                        bytes = BitConverter.GetBytes(TSlider3.Value + 12);
                        sendData[2] = bytes[0];
                        break;
                    case 0x0a:
                        sendData[1] = 0x0a;
                        bytes = BitConverter.GetBytes(TSlider4.Value + 12);
                        sendData[2] = bytes[0];
                        break;
                    case 0x0b:
                        sendData[1] = 0x0b;
                        bytes = BitConverter.GetBytes(TSlider5.Value + 12);
                        sendData[2] = bytes[0];
                        break;
                    case 0x0c:
                        sendData[1] = 0x0c;
                        bytes = BitConverter.GetBytes(TSlider6.Value + 12);
                        sendData[2] = bytes[0];
                        break;
                    case 0x0d:
                        sendData[1] = 0x0d;
                        bytes = BitConverter.GetBytes(TSlider7.Value + 12);
                        sendData[2] = bytes[0];
                        break;
                    case 0x0e:
                        sendData[1] = 0x0e;
                        bytes = BitConverter.GetBytes(TSlider8.Value + 12);
                        sendData[2] = bytes[0];
                        break;
                    case 0x0f:
                        sendData[1] = 0x0f;
                        bytes = BitConverter.GetBytes(TSlider9.Value + 12);
                        sendData[2] = bytes[0];
                        break;
                    case 0x10:
                        sendData[1] = 0x10;
                        bytes = BitConverter.GetBytes(TSlider10.Value + 12);
                        sendData[2] = bytes[0];
                        break;
                    case 0x11:
                        sendData[1] = 0x11;
                        bytes = BitConverter.GetBytes(79 - AKnob1.Value);
                        sendData[2] = bytes[0];
                        break;
                    case 0x12:
                        sendData[1] = 0x12;
                        bytes = BitConverter.GetBytes(79 - AKnob3.Value);
                        sendData[2] = bytes[0];
                        break;
                    case 0x13:
                        sendData[1] = 0x13;
                        bytes = BitConverter.GetBytes(79 - AKnob2.Value);
                        sendData[2] = bytes[0];
                        break;
                    case 0x14:
                        sendData[1] = 0x14;
                        bytes = BitConverter.GetBytes(79 - AKnob4.Value);
                        sendData[2] = bytes[0];
                        break;
                    case 0x15:
                        sendData[1] = 0x15;
                        if (KButton3.Value == true) sendData[2] = 0x01;
                        else sendData[2] = 0x00;
                        break;
                    case 0x16:
                        sendData[1] = 0x16;
                        if (KButton4.Value == true) sendData[2] = 0x01;
                        else sendData[2] = 0x00;
                        break;
                    case 0x17:
                        sendData[1] = 0x17;
                        byte bytess = Convert.ToByte(TimeBox.Text);
                        sendData[2] = bytess;
                        break;
                    case 0x18:
                        sendData[1] = 0x18;
                        if (KButton9.Value == true) sendData[2] = 0x01;
                        else sendData[2] = 0x00;
                        break;
                    case 0x19:
                        sendData[1] = 0x19;
                        if (KButton5.Value == true) sendData[2] = 0x01;
                        else sendData[2] = 0x00;
                        break;
                    case 0x1a:
                        sendData[1] = 0x1a;
                        if (KButton6.Value == true) sendData[2] = 0x01;
                        else sendData[2] = 0x00;
                        break;
                    case 0x20:
                        sendData[1] = 0x20;
                        sendData[2] = 0x01;
                        break;
                }
            }
            #endregion
            //串口打开 发送串口数据
            if (NeedSentDataDelay == 1)
            {
                if (ComboBox3.SelectedIndex == 0)
                {
                    if (mySerialPort.IsOpen) mySerialPort.Write(sendData, 0, sendData.Length);
                }
                else
                {
                    UDP_SentMessage(sendData);
                }
            }
            
           
        }
        private static Int16 NetequipmentDelay = 0;
        private static bool NetequipmentFlag = false;
        private void KButton2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (KButton2.Value == true)
            {
                //启动定时器查询联机情况
                NetequipmentDelay = 20;
                NetequipmentFlag = false;
               // if (mySerialPort.IsOpen)
                {
                    NeedSentDataCmd = 0x20;
                    NeedSentDataDelay = 2;
                }
                LinkFlag = true;
            }
            else
            {
                LinkFlag = false;
            }
        }

        private void KButton3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x15;
                NeedSentDataDelay = 2;
            }
        }

        private void KButton4_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x16;
                NeedSentDataDelay = 2;
            }
        }

        private void KButton5_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x19;
                NeedSentDataDelay = 2;
            }
        }

        private void KButton6_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x1a;
                NeedSentDataDelay = 2;
            }
        }

        private void KButton7_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x01;
                NeedSentDataDelay = 2;
            }
        }

        private void KButton8_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x06;
                NeedSentDataDelay = 2;
            }
        }

        private void KButton9_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x18;
                NeedSentDataDelay = 2;
            }
        }

        private void TSlider1_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x07;
                NeedSentDataDelay = 3;
            }
        }

        private void TSlider2_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x08;
                NeedSentDataDelay = 3;
            }
        }

        private void TSlider3_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x09;
                NeedSentDataDelay = 3;
            }
        }

        private void TSlider4_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x0A;
                NeedSentDataDelay = 3;
            }
        }

        private void TSlider5_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x0B;
                NeedSentDataDelay = 3;
            }
        }

        private void TSlider6_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x0C;
                NeedSentDataDelay = 3;
            }
        }

        private void TSlider7_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x0D;
                NeedSentDataDelay = 3;
            }
        }

        private void TSlider8_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x0E;
                NeedSentDataDelay = 3;
            }
        }

        private void TSlider9_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x0F;
                NeedSentDataDelay = 3;
            }
        }

        private void TSlider10_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x10;
                NeedSentDataDelay = 3;
            }
        }

        private void TSlider11_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x03;
                NeedSentDataDelay = 3;
            }
        }

        private void TSlider12_ValueChangedB(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
          //  if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x02;
                NeedSentDataDelay = 3;
            }
        }

        private void TimeUpdata_MouseDown(object sender, MouseButtonEventArgs e)
        {
           // if (NetequipmentFlag == true)
            {
                NeedSentDataCmd = 0x17;
                NeedSentDataDelay = 2;
            }
        }

        private void SystemWind_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //if (IsUdpcRecvStart)
            //{
            //    thrRecv.Abort(); // 必须先关闭这个线程，否则会异常  
            //    udpcRecv.Close();
            //}   
            Environment.Exit(0);  
        }

       

        

       
    

       

    }

}
