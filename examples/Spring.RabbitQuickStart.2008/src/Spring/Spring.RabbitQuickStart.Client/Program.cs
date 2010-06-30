#region License

/*
 * Copyright 2002-2009 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.Threading;
using System.Windows.Forms;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Context;
using Spring.Context.Support;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.RabbitQuickStart.Client.UI;

namespace Spring.RabbitQuickStart.Client
{
    static class Program
    {

        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                log.Info("Running....");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (IApplicationContext ctx = ContextRegistry.GetContext())
                {
                    InitializeRabbitQueues();
                    StockForm stockForm = new StockForm();
                    Application.ThreadException += ThreadException;
                    Application.Run(stockForm);
                }
            }
            catch (Exception e)
            {
                log.Error("Spring.RabbitQuickStart.Client is broken.", e);
            }
        }

        private static void ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            log.Error("Uncaught application exception.", e.Exception);
            Application.Exit();
        }

        private static void InitializeRabbitQueues()
        {
            RabbitTemplate template = ContextRegistry.GetContext().GetObject("RabbitTemplate") as RabbitTemplate;
            template.Execute<object>(delegate(IModel model)
            {
                model.QueueDeclare("APP.STOCK.MARKETDATA");
                //TODO Bind XSD needs to take into accout parameters nowait and 'Dictionary' args
                model.QueueBind("APP.STOCK.MARKETDATA", "", "", false, null);
                return null;
            });
        }
    }
}