using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;           //匯入網路通訊協定相關參數
using System.Net.Sockets;   //匯入網路插座功能函數
//「Socket 插座」，在網路通訊的概念為通訊兩端各建立一個插座，並以此處理訊息的收發
using System.Threading;     //匯入多執行續功能函數

namespace UDP即時通
{
    public partial class Form1 : Form
    {
        /*
        UDP通訊模式
        如同無線電對講機，透過設定頻道「EndPoint 網路通訊的端點」來接收與發出訊息
        EndPoint為「IP 電腦」與「Port 通訊埠」
        透過設定接收對象「IP」與頻道「Port」發送訊息；接收時則是監聽接收頻道「Port」等待訊息的傳入
        */
        UdpClient U;    //宣告 UDP 通訊物件
        Thread Th;      //宣告監聽用執行續
        public Form1()
        {
            InitializeComponent();
        }

        //程序啟動載入
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text += " " + MyIP();  //顯示「本機IP」於標題
        }

        //尋找「本機IP」函數
        private string MyIP()
        {
            string hn = Dns.GetHostName();                            //取得本機電腦名稱
            IPAddress[] ip_array = Dns.GetHostEntry(hn).AddressList;  //取得本機IP陣列
            foreach(IPAddress ip in ip_array)
            {
                if(ip.AddressFamily == AddressFamily.InterNetwork)    //如果IP為「IPv4」格式
                {
                    return ip.ToString();                             //回傳此IP字串
                }
            }
            return "";                                                //未找到則回傳空字串
        }

        //啟動監聽程序按鈕
        private void button1_Click(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;    //忽略跨執行續錯誤
            Th = new Thread(Listen);    //建立監聽執行續，執行程序為「Listen」
            Th.Start();                 //啟動監聽執行續
            button1.Enabled = false;    //使按鍵失效，避免重複啟動監聽
        }

        //建立監聽程序函數
        private void Listen()
        {
            int Port = int.Parse(textBox1.Text);    //設定監聽用的通訊埠
            U = new UdpClient(Port);                //監聽UDP監聽器實體
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port); //建立「網路通訊端點 IPEndPoint」
            //"127.0.0.1"為預設的「本機電腦 IP」，任何電腦輸入此IP會把訊息回傳給自己
            while (true)    //建立持續監聽的無限迴圈，有訊息則處理、無訊息則不斷重複等待
            {
                byte[] B = U.Receive(ref EP);                   //收到訊息時取得訊息的「Byte陣列」
                textBox2.Text = Encoding.Default.GetString(B);  //翻譯「Byte陣列」轉換成字串並顯示
            }
        }

        //發送UDP訊息
        private void button2_Click(object sender, EventArgs e)
        {
            string IP = textBox3.Text;                              //設定發送目標的IP
            int Port = int.Parse(textBox4.Text);                    //設定發送目標的Port
            byte[] B = Encoding.Default.GetBytes(textBox5.Text);    //將要發送的文字轉換為「Byte陣列」
            // 網路程式傳遞資料只能傳送「純資料 Byte」
            UdpClient S = new UdpClient();      //建立UDP通訊器
            S.Send(B, B.Length, IP, Port);      //發送資料到指定位置
            S.Close();                          //關閉通訊器
        }

        //關閉程序同時關閉執行續(如果有開啟的話)
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Th.Abort();     //關閉監聽執行續
                U.Close();      //關閉監聽器
            }
            catch
            {
                //如果關閉失敗(未開啟監聽)，則忽略錯誤
            }
        }
    }
}
