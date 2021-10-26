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
using System.Collections;   //匯入集合物件功能

namespace 伺服器端
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        /*
        TCP通訊模式
        TCP與UDP不同的差異在，TCP就像是打電話，撥出電話後一定要對方確定接聽，才能開始真正的對話
        在通訊過程中可以確定對方是在線上與自己通話中
        TPC可以追蹤每一個資料封包的去向，如果沒有確實傳到便會重傳，可以說是使命必達，一定會把資料傳送到目的地
        
        TCP通訊模式的伺服器端
        在TCP通訊的伺服器與客戶端，如同星狀拓樸
        多個客戶端會先與伺服器端取得雙向的連線，客戶之間如果需要互通訊息，則先傳給伺服器端，再由伺服器端轉送到另一位客戶
        伺服器端通常需要同時服務多個客戶，負擔會比客戶端重，所以多數網路程式會盡量讓伺服器端的任務單純化，通常只做轉送訊息的工作

        TCP通訊連線與離線的流程
        首先伺服器端使用某一Port啟動監聽，客戶端針對伺服器端的IP與Port提出連線請求
        伺服器端收到請求後會替該客戶建立一個專屬的連線，之後就可以隨時進行雙向的通訊
        離線時也必須是客戶端主動提出離線的請求，伺服器端收到離線請求後便會將先前建立的連線給移除
        */
        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;    //忽略跨執行續錯誤
            textBox1.Text = MyIP();
        }

        //尋找「本機IP」函數
        private string MyIP()
        {
            string hn = Dns.GetHostName();                            //取得本機電腦名稱
            IPAddress[] ip_array = Dns.GetHostEntry(hn).AddressList;  //取得本機IP陣列
            foreach (IPAddress ip in ip_array)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)    //如果IP為「IPv4」格式
                {
                    return ip.ToString();                             //回傳此IP字串
                }
            }
            return "";                                                //未找到則回傳空字串
        }
        TcpListener Server;     //伺服器端網路監聽器
        Socket Client;          //給使用者用連線物件
        Thread Th_Sev;          //伺服器端監聽執行續
        Thread Th_Cli;          //使用者端連線執行續
        Hashtable HT = new Hashtable();     //存放使用者與連線物件的集合
        /*「Hashtable」與「Dictionary」
        「Hashtable」與「Dictionary」從資料結構上來說都屬於雜湊表，都是透過關鍵字Key來查詢值Value
        在單執行緒程式中，Dictionary有泛型優勢，且讀取速度較快、容量利用更充分
        在多執行緒程式中，Hashtable默認允許單執行緒寫入、多執行緒讀取
        */

        //啟動伺服器的啟動事件，開啟Server，用Server Thread來監聽Client
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "啟 動 伺 服 器")
            {
                Th_Sev = new Thread(ServerSub);     //建立伺服器端監聽執行續
                Th_Sev.IsBackground = true;         //設定為背景執行續
                Th_Sev.Start();                     //執行伺服器端監聽執行續
                button1.Text = "關 閉 伺 服 器";    //切換按鍵文字為關閉
            }
            else
            {
                Application.ExitThread();           //關閉所有執行續
                button1.Text = "啟 動 伺 服 器";    //切換按鍵文字為開啟
            }
        }

        //接受使用者連線的請求，針對每一個使用者建立一個專用監聽執行續
        private void ServerSub()
        {
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse(textBox1.Text), int.Parse(textBox2.Text));
            //建立「網路通訊端點 IPEndPoint」，參數為「Server IP」與「Port 頻道」
            Server = new TcpListener(EP);       //建立伺服器端監聽器
            Server.Start(100);                  //啟動監聽並設定最大允許連線數
            while (true)        //無限迴圈監聽連線請求
            {
                Client = Server.AcceptSocket();     //建立此使用者的連線物件
                Th_Cli = new Thread(Listen);        //建立監聽這個使用者的專用執行續
                Th_Cli.IsBackground = true;         //設定為背景執行續
                Th_Cli.Start();                     //開始執行續
                /*
                設定背景執行緒，降低新增的執行緒的地位，讓主執行緒在結束時可以順利關閉新增的執行緒
                如果設定為背景執行，在主程式被關閉時的檢查動作就會被忽略，而避免一些例外狀況導致無法關閉執行緒
                而如果新增的執行緒與主執行緒的地位相當，當主執行緒的表單被關閉時，新增的執行緒還在系統運作，可能發生莫名其妙的程式衝突
                */
            }
        }

        private void Listen()
        {
            Socket Sck = Client;        //宣告使用者專用連線物件Sck是Client通訊物件
            Thread Th = Th_Cli;         //宣告區域變數使用者專用執行續是Th_Cli執行續
            while (true)
            {
                try
                {
                    byte[] B = new byte[1023];          //建立「Byte陣列」物件，用來接收訊息，大小必須大於可能接收的訊息長度
                    int inLen = Sck.Receive(B);         //將接收到的資料放進B
                    string Msg = Encoding.Default.GetString(B, 0, inLen);   //轉譯接收到的訊息成字串
                    string Cmd = Msg.Substring(0, 1);       //取出訊息的第1個字(動作)
                    string Str = Msg.Substring(1);          //取出動作後的訊息
                    switch (Cmd)        //根據使用者的動作決定行為
                    {
                        case "0":       //動作為使用者登入
                            HT.Add(Str, Sck);           //使用者登入，將使用者加入雜湊表
                            //Key : 使用者、Value : 連線物件(Socket)
                            listBox1.Items.Add(Str);    //將使用者加入「線上使用者」清單
                            break;

                        case "1":       //動作為使用者登出
                            HT.Remove(Str);             //使用者登出，將使用者移出雜湊表
                            listBox1.Items.Remove(Str); //將使用者移出「線上使用者」清單
                            Sck.Close();                //關閉使用者的連線
                            Th.Abort();                 //結束此使用者專用監聽執行續
                            break;
                    }
                }
                catch
                {
                    //有錯誤時忽略，避免使用者端無預警的強制關閉程式
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.ExitThread();       //關閉所有執行續
        }
    }
}
