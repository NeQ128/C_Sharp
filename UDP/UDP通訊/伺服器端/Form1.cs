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

namespace 伺服器端
{
    public partial class Form1 : Form
    {
        UdpClient U;                //宣告 UDP 通訊物件
        Thread Th;                  //宣告監聽用執行續
        const short Port = 7777;    //本程式使用 的通訊埠(頻道)
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Th = new Thread(Listen);        //建立監聽執行續
            Th.IsBackground = true;         //設定為背景執行續
            Th.Start();                     //啟動監聽執行續
        }

        private void Listen()
        {
            U = new UdpClient(Port);      //監聽UDP監聽器實體
            while (true)    //建立持續監聽的無限迴圈，有訊息則處理、無訊息則不斷重複等待
            {
                IPEndPoint EP = new IPEndPoint(IPAddress.Any, Port);    ////建立「網路通訊端點 IPEndPoint」
                //「IPAddress.Any」為接收任何IP
                byte[] B = U.Receive(ref EP);
                string A = Encoding.Default.GetString(B);
                string M = "不明白的指令";                  //宣告回傳的訊息
                label3.Text = A;                            //顯示接收到的訊息
                label4.Text = EP.Address.ToString();        //顯示接收到的來源IP
                if (A == "Time")                    //如果接收到的訊息為"Time"
                {
                    M = DateTime.Now.ToString();    //回傳訊息變成現在時間
                }
                B = Encoding.Default.GetBytes(M);
                U.Send(B, B.Length, EP);            //傳送設定好的回傳訊息給來源EP
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Th.Abort();         //關閉監聽執行續
            U.Close();          //關閉監聽器
        }
    }
}
