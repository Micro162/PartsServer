
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace PartsServer
{
    public partial class Form1 : Form
    {
        UdpClient server;
        bool running;

        Dictionary<string, DateTime> clients = new Dictionary<string, DateTime>();
        Dictionary<string, List<DateTime>> requests = new Dictionary<string, List<DateTime>>();

        TextBox log = new TextBox();
        Button start = new Button();

        int maxClients = 5;

        public Form1()
        {
            Text = "Сервер комплектуючих";
            Width = 600;
            Height = 400;

            log.Multiline = true;
            log.ScrollBars = ScrollBars.Vertical;
            log.SetBounds(10, 50, 560, 290);

            start.Text = "Запустити сервер";
            start.SetBounds(10, 10, 200, 30);
            start.Click += StartServer;

            Controls.Add(log);
            Controls.Add(start);
        }

        async void StartServer(object sender, EventArgs e)
        {
            try
            {
                server = new UdpClient(5000);
                running = true;
                start.Enabled = false;
                log.AppendText("Сервер запущено. Порт 5000\r\n");

                while (running)
                {
                    UdpReceiveResult result = await server.ReceiveAsync();

                    string key = result.RemoteEndPoint.ToString();
                    string part = Encoding.UTF8.GetString(result.Buffer).Trim();

                    DateTime now = DateTime.Now;

                    foreach (string old in clients
                        .Where(x => (now - x.Value).TotalMinutes >= 10)
                        .Select(x => x.Key).ToList())
                    {
                        clients.Remove(old);
                        requests.Remove(old);
                        log.AppendText("Неактивного клієнта видалено\r\n");
                    }

                    if (!clients.ContainsKey(key))
                    {
                        if (clients.Count >= maxClients)
                        {
                            await Send("Сервер зайнятий. Спробуйте пізніше.",
                                result.RemoteEndPoint);
                            continue;
                        }

                        clients[key] = now;
                        requests[key] = new List<DateTime>();
                    }

                    clients[key] = now;

                    requests[key].RemoveAll(
                        x => (now - x).TotalHours >= 1);

                    if (requests[key].Count >= 10)
                    {
                        await Send("Ліміт: максимум 10 запитів за годину.",
                            result.RemoteEndPoint);
                        continue;
                    }

                    requests[key].Add(now);

                    string answer = GetPrice(part);

                    log.AppendText(key + ": " + part + "\r\n");

                    await Send(answer, result.RemoteEndPoint);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                log.AppendText("Помилка: " + ex.Message + "\r\n");
            }
        }

        string GetPrice(string part)
        {
            part = part.ToLower();

            if (part.Contains("процесор"))
                return "Ціна процесора: 200 €";

            if (part.Contains("відеокарта"))
                return "Ціна відеокарти: 450 €";

            if (part.Contains("оперативна пам"))
                return "Ціна оперативної пам'яті: 60 €";

            if (part.Contains("ssd"))
                return "Ціна SSD: 80 €";

            return "Товар не знайдено.";
        }

        async Task Send(string message, IPEndPoint endpoint)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            await server.SendAsync(data, endpoint);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            running = false;
            server?.Close();
            base.OnFormClosing(e);
        }
    }
}
