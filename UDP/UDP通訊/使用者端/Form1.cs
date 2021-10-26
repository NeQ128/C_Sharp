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

        private void button1_Click(object sender, EventArgs e)
        {
            UdpClient C = new UdpClient();      //建立UDP實體
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777);
            //建立「網路通訊端點 IPEndPoint」，參數為「伺服器的IP」與伺服器使用的「Port 頻道」
            //因測試用，使用本機電腦的IP「127.0.0.1」
            C.Connect(EP);      //透過通訊端點與伺服器連接
            byte[] B = Encoding.Default.GetBytes(textBox1.Text);
            C.Send(B, B.Length);
            byte[] R = C.Receive(ref EP);                   //送出訊息後，從端點接收到的純資料
            textBox2.Text = Encoding.Default.GetString(R);  //將接收到的純資料轉換為字串並顯示
        }
    }
}
