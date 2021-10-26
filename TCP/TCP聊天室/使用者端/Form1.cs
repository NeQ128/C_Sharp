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

namespace 使用者端
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Socket T;       //通訊物件
        Thread Th;      //網路監聽執行緒
        string User;    //使用者
        private void button1_Click(object sender, EventArgs e)
        {
            if(button1.Text == "登入伺服器")
            {
                CheckForIllegalCrossThreadCalls = false;
                string IP = textBox1.Text;                  //伺服器IP
                int Port = int.Parse(textBox2.Text);        //伺服器Port
                IPEndPoint EP = new IPEndPoint(IPAddress.Parse(IP), Port);      //伺服器連線端點
                T = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //建立雙向通訊的TCP連線
                User = textBox3.Text;       
                try                         //嘗試連接伺服器
                {
                    T.Connect(EP);          //連接伺服器的端點EP
                    Th = new Thread(Listen);//建立監聽執行緒
                    Th.Start();             //開始監聽值型緒
                    textBox4.Text += "已連接伺服器!!\r\n";  //連接成功時出現訊息
                    Send("0" + User);       //連接成功後傳送登入指令與自己的使用者名稱給伺服器
                }
                catch
                {
                    textBox4.Text += "無法連接伺服器!!\r\n";//連接失敗時出現訊息
                    return;
                }
                button1.Text = "登出伺服器";
                button2.Enabled = true;         //開啟送出按鈕使用
                button4.Enabled = true;         //開啟廣播按鈕使用
            }
            else
            {
                Send("1" + User);       //登出時傳送登出指令與自己的使用者名稱給伺服器
                Th.Abort();             //關閉監聽執行緒
                T.Close();              //關閉網路通訊器
                button1.Text = "登入伺服器";
                button2.Enabled = false;        //關閉送出按鈕使用
                button4.Enabled = false;        //關閉廣播按鈕使用
            }
        }

        private void Listen()
        {
            EndPoint ServerEP = (EndPoint)T.RemoteEndPoint;     //Server 的 EndPoint
            byte[] B = new byte[1023];      //接收資料用的Byte陣列
            int inLen = 0;                  //接收的位元組數目
            string Msg;                     //接收到的訊息
            string Cmd;                     //訊息指令
            string Str;                     //訊息內容
            while (true)                    //監聽訊息用的無限迴圈
            {
                try
                {
                    inLen = T.ReceiveFrom(B, ref ServerEP);     //接收訊息並取得位元組數目
                }
                catch       //當連接發生錯誤時
                {
                    T.Close();              //關閉通訊器
                    listBox1.Items.Clear(); //清除線上名單
                    MessageBox.Show("與伺服器中斷連線!!");      //提示訊息
                    button1.Text = "登入伺服器";
                    button2.Enabled = false;        //關閉送出按鈕使用
                    button4.Enabled = false;        //關閉廣播按鈕使用
                    Th.Abort();             //關閉監聽執行緒
                }
                Msg = Encoding.Default.GetString(B, 0, inLen);      //轉譯收到的純資料為字串
                Cmd = Msg.Substring(0, 1);      //取出指令
                Str = Msg.Substring(1);         //取出後續訊息
                switch (Cmd)        //依照指令執行
                {
                    case "L":       //接收線上成員名單
                        listBox1.Items.Clear();
                        string[] M = Str.Split(',');
                        for(int i = 0;i < M.Length; i++)
                        {
                            listBox1.Items.Add(M[i]);
                        }
                        break;

                    case "9":       //接收廣播訊息
                        textBox4.Text += Str + "\r\n";
                        textBox4.SelectionStart = textBox4.Text.Length;     //游標移到最後
                        textBox4.ScrollToCaret();                           //捲動到游標位置
                        break;
                    case "2":       //接收私人訊息
                        textBox4.Text += Str + "\r\n";
                        textBox4.SelectionStart = textBox4.Text.Length;     //游標移到最後
                        textBox4.ScrollToCaret();                           //捲動到游標位置
                        break;
                }
            }
        }

        private void Send(string Str)
        {
            byte[] B = Encoding.Default.GetBytes(Str);      //轉譯字串為純資料
            T.Send(B, 0, B.Length, SocketFlags.None);       //使用通訊物件傳送資料
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (button1.Text == "登出伺服器")
            {
                Send("1" + User);       //登出時傳送登出指令與自己的使用者名稱給伺服器
                T.Close();              //關閉網路通訊器
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox5.Text == "")                //未填入發送訊息時不做事
                return;
            if(listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("請選擇傳送對象!!");//沒選擇發送對象時出現提示
                return;
            }
            else
            {
                Send("2"+"From " + User + " : " + textBox5.Text + "|" + listBox1.SelectedItem);
                //製作發送訊息並傳送
                textBox4.Text += "To   " + listBox1.SelectedItem + " : " + textBox5.Text + "\r\n";
                //在訊息欄裡寫入發送的訊息
                textBox5.Text = "";     //清除發言欄
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Send("9" + User + " to 所有人 : " + textBox5.Text);
            //製作發送訊息並傳送
            textBox5.Text = "";     //清除發言欄
        }
  
        private void button3_Click(object sender, EventArgs e)
        {
            listBox1.ClearSelected();   //清除選取
        }
    }
}
