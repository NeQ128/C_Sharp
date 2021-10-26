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

namespace UDP區域聊天室
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        /*
        UDP通訊模式
        如同無線電對講機，透過設定頻道「EndPoint 網路通訊的端點」來接收與發出訊息
        EndPoint為「IP 電腦」與「Port 通訊埠」
        透過設定接收對象「IP」與頻道「Port」發送訊息；接收時則是監聽接收頻道「Port」等待訊息的傳入
        */
        //公用變數宣告
        UdpClient U;                                //宣告 UDP 通訊物件
        Thread Th;                                  //宣告監聽用執行續
        string MyName;                              //使用者名稱
        ArrayList ips = new ArrayList();            //在線使用者的IP列表
        const short Port = 6666;                    //本程式使用的通訊埠(頻道)
        string BC = IPAddress.Broadcast.ToString(); //廣播用IP
        /*
        在區域網路裡，網路區段的範圍大小取決於子網路遮罩，最常見的子網路遮罩為 : 255.255.255.0
        表示該區域網路的IP位址前三碼相同，最後一碼不同，而最後一碼為255的IP就是廣播用IP
        */

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;    //忽略跨執行續錯誤
            this.Text += " " + MyIP();          //顯示「本機IP」於標題
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

        //上線或離線的按鈕被按下的事件
        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();             //清空線上人員名單
            ips.Clear();                        //清空線上人員IP列表
            if(button1.Text == "登入")          //當使用者按下登入
            {
                if (textBox1.Text == "")        //如果沒有輸入使用者名稱，則出現提示訊息
                {
                    MessageBox.Show("請輸入使用者名稱!!");
                    return;
                }
                MyName = textBox1.Text;         //我的名稱
                Th = new Thread(Listen);        //建立監聽執行續
                Th.Start();                     //啟動監聽執行續
                Send(BC, "", "OnLine");         //廣播上線公告
                button1.Text = "登出";          //按鍵文字切換為「登出」
            }
            else{                               //當使用者按下登出
                Send(BC, "", "OffLine");        //廣播離線公告
                Th.Abort();                     //關閉監聽執行續
                U.Close();                      //關閉監聽器
                button1.Text = "登入";          //按鍵文字切換為「登入」
            }
        }

        //廣播按鍵按下事件
        private void button2_Click(object sender, EventArgs e)
        {
            Send(BC, textBox2.Text,"ToAll");    //發送廣播訊息
            textBox2.Text = "";                 //清空訊息方塊文字
        }

        //訊息方塊輸入事件
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)             //當使用者按下Enter時執行
            {
                if(listBox1.SelectedIndex < 0)      //尋找發送對象，如果沒有對象則出現提示訊息
                {
                    MessageBox.Show("請輸入傳送訊息的對象!!");
                    return;
                }
                else
                {   
                    //發送私密訊息
                    Send(ips[listBox1.SelectedIndex].ToString(), textBox2.Text, "Message");
                    //將發送的私密訊息加入「訊息欄」
                    listBox2.Items.Add("To   " + listBox1.SelectedItem.ToString() + " : " + textBox2.Text);
                }
                textBox2.Text = "";         //清空訊息方塊文字
            }
        }

        //清除選擇按鍵按下事件
        private void button3_Click(object sender, EventArgs e)
        {
            listBox1.ClearSelected();       //清除已選擇的私密對象
        }

        //傳送訊息函數，參數為 : 發送對象IP、發送訊息、執行動作
        private void Send(string ToIP,string msg,string doing)
        {
            //製作發送的訊息，訊息格式：使用者名稱:使用者IP:訊息:動作
            string A = MyName + ":" + MyIP() + ":" + msg + ":" + doing;
            byte[] B = Encoding.Default.GetBytes(A);        //將發送的訊息轉譯為純資料
            UdpClient V = new UdpClient(ToIP, Port);        //建立UDP通訊物件
            V.Send(B, B.Length);                            //發送資料
        }

        //監聽訊息函數
        private void Listen()
        {
            U = new UdpClient(Port);        //監聽UDP監聽器實體
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse(MyIP()), Port);
            //建立「網路通訊端點 IPEndPoint」，參數為「本機的IP」與程式使用的「Port 頻道」
            while (true)
            {
                byte[] B = U.Receive(ref EP);               //收到訊息時取得訊息的「Byte陣列」
                string A = Encoding.Default.GetString(B);   //翻譯「Byte陣列」轉換成字串A
                string[] C = A.Split(':');
                //將A字串的內容切割，切割後的內容為：
                //C[0] : 傳送者名稱、C[1] : 傳送者IP、C[2] : 訊息、C[3] : 執行動作
                /*
                當使用者「上線」時，先用廣播告知目前的在線者自己上線了
                在線的所有人(包括自己)收到「OnLine」動作的廣播
                如果是自己以外的人收到，再回傳「AddMe」給上線者，告知自己的名稱與IP
                */
                switch (C[3])       //根據接收到的「動作」決定行為
                {
                    case "OnLine":                  //動作「上線」
                        listBox1.Items.Add(C[0]);   //將傳送者名稱加入「在線使用者」
                        ips.Add(C[1]);              //將傳送者IP加入「在線使用者IP列表」
                        if (C[1] != MyIP())         //如果傳送者不是自己，則回傳「加入我」的動作
                            Send(C[1],"","AddMe");
                        break;

                    case "AddMe":                   //動作「加入我」
                        listBox1.Items.Add(C[0]);   //將傳送者名稱加入「在線使用者」
                        ips.Add(C[1]);              //將傳送者IP加入「在線使用者IP列表」
                        break;

                    case "OffLine":                 //動作「離線」
                        listBox1.Items.Remove(C[0]);//將傳送者名稱移出「在線使用者」
                        ips.Remove(C[1]);           //將傳送者IP移出「在線使用者IP列表」
                        break;

                    case "Message":                 //動作「訊息」
                        listBox2.Items.Add("From " + C[0] + " : " + C[2]);
                        //將接收到的傳訊人與訊息加入「訊息欄」
                        break;

                    case "ToAll":                   //動作「廣播」
                        listBox2.Items.Add("(廣播) " + C[0] + " : " + C[2]);
                        //將接收到的傳訊人與訊息加入「訊息欄」
                        break;
                }
            }
        }

        //關閉程序同時關閉執行續(如果有開啟的話)
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (button1.Text == "登出")     //如果使用者狀態為「上線」時執行
                {
                    Send(BC, "", "OffLine");    //廣播離線公告
                    Th.Abort();                 //關閉監聽執行續
                    U.Close();                  //關閉監聽器
                }
            }
            catch
            {
                //如果關閉失敗(未開啟監聽)，則忽略錯誤
            }
        }
    }
}
