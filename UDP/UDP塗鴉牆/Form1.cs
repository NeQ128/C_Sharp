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
using Microsoft.VisualBasic.PowerPacks; //匯入VB向量繪圖功能

namespace UDP塗鴉牆
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
        //宣告繪圖相關變數
        ShapeContainer C;       //畫布物件，本機繪圖用
        ShapeContainer D;       //畫布物件，遠端繪圖用
        Point stP;              //繪圖座標起點
        //「Point」是一種包含「X」與「Y」座標的資料型別
        string p;               //筆畫座標字串

        private void Form1_Load(object sender, EventArgs e)
        {
            C = new ShapeContainer();       //建立本機繪圖用畫布
            this.Controls.Add(C);           //視窗加入畫布C
            D = new ShapeContainer();       //建立遠端繪圖用畫布
            this.Controls.Add(D);           //視窗加入畫布D
            this.Text += " " + MyIP();      //顯示「本機IP」於標題
            radioButton4.Checked = true;    //預設畫筆顏色為黑色
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
            int Port = int.Parse(textBox3.Text);    //設定監聽用的通訊埠
            U = new UdpClient(Port);                //監聽UDP監聽器實體
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port); //建立「網路通訊端點 IPEndPoint」
            //"127.0.0.1"為預設的「本機電腦 IP」，任何電腦輸入此IP會把訊息回傳給自己
            while (true)    //建立持續監聽的無限迴圈，有訊息則處理、無訊息則不斷重複等待
            {
                byte[] B = U.Receive(ref EP);               //收到訊息時取得訊息的「Byte陣列」
                string A = Encoding.Default.GetString(B);   //將收到的資料「Byte」轉譯為字串
                string[] Z = A.Split('_');              //切割顏色與座標資訊
                string[] Q = Z[1].Split('/');           //切割座標點資訊
                Point[] R = new Point[Q.Length];        //宣告座標陣列
                //將接收到的座標資料存放在本機準備繪製
                //執行次數為接收到的座標數量
                for(int i = 0;i < Q.Length; i++)
                {
                    string[] K = Q[i].Split(',');       //切割「X」與「Y」座標
                    R[i].X = int.Parse(K[0]);           //紀錄第i點的「X」座標
                    R[i].Y = int.Parse(K[1]);           //紀錄第i點的「Y」座標
                }
                //準備將處理好的座標資料進行繪製
                //由於每一筆畫需由起點及終點才能繪製
                //最後一個座標位置為前一個座標起點筆畫的終點，所以執行次數為座標總數 - 1
                for (int i = 0;i < Q.Length - 1; i++)
                {
                    LineShape L = new LineShape();      //建立線段物件
                    L.StartPoint = R[i];                //線段起點
                    L.EndPoint = R[i + 1];              //線段終點
                    switch (Z[0])           //透過「Switch」分辨線段顏色
                    {
                        case "1":
                            L.BorderColor = Color.Red;
                            break;
                        case "2":
                            L.BorderColor = Color.Green;
                            break;
                        case "3":
                            L.BorderColor = Color.Blue;
                            break;
                        case "4":
                            L.BorderColor = Color.Black;
                            break;
                    }
                    L.Parent = D;       //將線段L加入遠端繪圖用畫布D
                }
            }
        }

        //滑鼠點下事件，建立本機繪圖起點
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            stP = e.Location;                               //起點座標
            p = stP.X.ToString() + "," + stP.Y.ToString();  //紀錄起點座標的字串
        }

        //滑鼠移動事件，紀錄滑鼠移動軌跡座標
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            //在滑鼠移動時，必須先確認滑鼠左鍵是否處於按下狀態
            if(e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                LineShape L = new LineShape();      //建立線段物件
                L.StartPoint = stP;                 //線段起點
                L.EndPoint = e.Location;            //線段終點
                if (radioButton1.Checked)
                    L.BorderColor = Color.Red;
                if (radioButton2.Checked)
                    L.BorderColor = Color.Green;
                if (radioButton3.Checked)
                    L.BorderColor = Color.Blue;
                if (radioButton4.Checked)
                    L.BorderColor = Color.Black;
                //畫筆顏色
                L.Parent = C;       //將線段L加入本機繪圖用畫布C
                //動態產生的物件必須將其加入視窗，使用者才能看到該物件
                stP = e.Location;   //將終點座標覆蓋掉起點變數，成為新的起點座標
                p += "/" + stP.X.ToString() + "," + stP.Y.ToString();
                //紀錄傳送用顏色與座標資料
            }
        }

        //按鍵放開事件，送出繪圖資料
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (textBox1.Text != "" && textBox2.Text != "")
            //增加防呆機制，當接收方的IP與Port有輸入時，才執行發送動作
            {
                UdpClient S = new UdpClient(textBox1.Text, int.Parse(textBox2.Text));   //建立UDP通訊器，參數為目標IP與目標Port
                if (radioButton1.Checked)
                    p = "1_" + p;
                if (radioButton2.Checked)
                    p = "2_" + p;
                if (radioButton3.Checked)
                    p = "3_" + p;
                if (radioButton4.Checked)
                    p = "4_" + p;
                //將畫筆顏色加入傳送資料字串
                byte[] B = Encoding.Default.GetBytes(p);    //將紀錄的筆劃資料字串p轉譯為純資料Byte
                S.Send(B, B.Length);                        //傳送筆畫資料
                S.Close();                                  //關閉通訊器
            }
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
