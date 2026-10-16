
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PartsClient
{
    public partial class Form1 : Form
    {
        UdpClient client = new UdpClient();
        TextBox address = new TextBox();
        TextBox answer = new TextBox();
        Button send = new Button();
        ComboBox parts = new ComboBox();

        public Form1()
        {
            Text = "Клієнт комплектуючих";
            Width = 500;
            Height = 300;

            address.Text = "127.0.0.1";
            address.SetBounds(10, 10, 200, 30);

            parts.SetBounds(10, 50, 250, 30);
            parts.Items.AddRange(new object[]
            {
                "процесор",
                "відеокарта",
                "оперативна пам'ять",
                "ssd"
            });
            parts.SelectedIndex = 0;

            send.Text = "Запитати ціну";
            send.SetBounds(270, 50, 150, 30);
            send.Click += SendRequest;

            answer.Multiline = true;
            answer.SetBounds(10, 100, 450, 140);
            answer.ReadOnly = true;

            Controls.Add(address);
            Controls.Add(parts);
            Controls.Add(send);
            Controls.Add(answer);
        }

        async void SendRequest(object sender, EventArgs e)
        {
            try
            {
                send.Enabled = false;

                using UdpClient udp = new UdpClient();
                udp.Client.ReceiveTimeout = 5000;

                string ip = address.Text;
                string part = parts.Text;

                byte[] data = Encoding.UTF8.GetBytes(part);

                await udp.SendAsync(data, data.Length, ip, 5000);

                var receiveTask = udp.ReceiveAsync();
                var timeoutTask = Task.Delay(5000);

                if (await Task.WhenAny(receiveTask, timeoutTask) != receiveTask)
                {
                    answer.Text = "Сервер не відповідає.";
                    return;
                }

                var result = await receiveTask;
                answer.Text = Encoding.UTF8.GetString(result.Buffer);
            }
            catch (Exception ex)
            {
                answer.Text = "Помилка: " + ex.Message;
            }
            finally
            {
                send.Enabled = true;
            }
        }
    }
}
