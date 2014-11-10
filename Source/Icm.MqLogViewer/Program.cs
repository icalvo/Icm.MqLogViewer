using System;
using System.Configuration;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Icm.MqLogViewer
{
    internal class Program
    {
        private static ConsoleColor _defaultBackgroundColor;
        private static ConsoleColor _defaultForegroundColor;

        private static void Main()
        {
            Console.WriteLine("LOGVIEWER 1.0");
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = ConfigurationManager.AppSettings["RabbitMqHost"],
                UserName = ConfigurationManager.AppSettings["RabbitMqUser"],
                Password = ConfigurationManager.AppSettings["RabbitMqPassword"],
                Protocol = Protocols.DefaultProtocol
            };

            using (IConnection connection = factory.CreateConnection())
            using (IModel model = connection.CreateModel())
            {
                QueueingBasicConsumer consumer = new QueueingBasicConsumer(model);
                QueueDeclareOk queue = model.QueueDeclare(
                    ConfigurationManager.AppSettings["RabbitMqQueue"], 
                    durable: true, 
                    exclusive: true, 
                    autoDelete: true,
                    arguments: null);
                model.QueueBind(
                    queue: queue,
                    exchange: ConfigurationManager.AppSettings["RabbitMqExchange"],
                    routingKey: ConfigurationManager.AppSettings["RabbitMqRoutingKey"]);
                model.BasicConsume(
                    queue: queue, 
                    noAck: true,
                    consumer: consumer);

                Console.WriteLine("Started listening");

                _defaultForegroundColor = Console.ForegroundColor;
                _defaultBackgroundColor = Console.BackgroundColor;
                while (true)
                {
                    BasicDeliverEventArgs message = consumer.Queue.Dequeue();
                    try
                    {
                        string messageBody = message.Body.AsUtf8String();
                        LogEntry logEntry = JsonConvert.DeserializeObject<LogEntry>(messageBody);
                        Log(logEntry);
                    }
                    catch (Exception ex)
                    {
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private static void Log(LogEntry logEntry)
        {
            Console.BackgroundColor = _defaultBackgroundColor;
            switch (logEntry.Level)
            {
                case "TRACE":
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case "DEBUG":
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    break;
                case "INFO":
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case "WARN":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case "ERROR":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case "FATAL":
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.BackgroundColor = ConsoleColor.Red;
                    break;
            }
            Console.WriteLine("{0} {1} {2} {3}", logEntry.Date, logEntry.Level, logEntry.Message, logEntry.Exception);
            Console.ForegroundColor = _defaultForegroundColor;
            Console.BackgroundColor = _defaultBackgroundColor;
        }
    }

    internal static class Extensions
    {
        public static string AsUtf8String(this byte[] args)
        {
            return Encoding.UTF8.GetString(args);
        }
    }
}
