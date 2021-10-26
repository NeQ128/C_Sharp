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

namespace 使用者端
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Socket T;       //通訊物件
        string User;    //使用者
        private void button1_Click(object sender, EventArgs e)
        {
            if(button1.Text == "登入伺服器")
            {
                string IP = textBox1.Text;                  //伺服器IP
                int Port = int.Parse(textBox2.Text);        //伺服器Port
                IPEndPoint EP = new IPEndPoint(IPAddress.Parse(IP), Port);      //伺服器連線端點
                T = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //建立雙向通訊的TCP連線
                User = textBox3.Text;       
                try                     //嘗試連接伺服器
                {
                    T.Connect(EP);      //連接伺服器的端點EP
                    Send("0" + User);   //連接成功後傳送登入指令與自己的使用者名稱給伺服器
                }
                catch
                {
                    MessageBox.Show("連接伺服器失敗!!");
                    return;
                }
                button1.Text = "登出伺服器";
            }
            else
            {
                Send("1" + User);       //登出時傳送登出指令與自己的使用者名稱給伺服器
                T.Close();              //關閉網路通訊器
                button1.Text = "登入伺服器";
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
    }
}
