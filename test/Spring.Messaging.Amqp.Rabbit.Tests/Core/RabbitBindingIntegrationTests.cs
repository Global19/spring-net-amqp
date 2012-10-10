﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitBindingIntegrationTests.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   http://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Support.Converter;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Core
{
    /// <summary>
    /// Rabbit Binding Integration Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class RabbitBindingIntegrationTests : AbstractRabbitIntegrationTest
    {
        /// <summary>
        /// The queue.
        /// </summary>
        private static readonly Queue queue = new Queue("test.queue");

        /// <summary>
        /// The connection factory.
        /// </summary>
        private readonly IConnectionFactory connectionFactory = new CachingConnectionFactory(BrokerTestUtils.GetPort());

        /// <summary>
        /// The rabbit template.
        /// </summary>
        private readonly RabbitTemplate template;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitBindingIntegrationTests"/> class.
        /// </summary>
        public RabbitBindingIntegrationTests() { this.template = new RabbitTemplate(this.connectionFactory); }

        #region Fixture Setup and Teardown

        /// <summary>
        /// Code to execute before fixture setup.
        /// </summary>
        public override void BeforeFixtureSetUp() { }

        /// <summary>
        /// Code to execute before fixture teardown.
        /// </summary>
        public override void BeforeFixtureTearDown() { }

        /// <summary>
        /// Code to execute after fixture setup.
        /// </summary>
        public override void AfterFixtureSetUp() { }

        /// <summary>
        /// Code to execute after fixture teardown.
        /// </summary>
        public override void AfterFixtureTearDown() { }
        #endregion

        #region Test Setup and Teardown

        /// <summary>
        /// Sets up.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            var brokerRunning = BrokerRunning.IsRunningWithEmptyQueues(queue);
            if (!brokerRunning.Apply())
            {
                Assert.Ignore("Cannot execute test as the broker is not running with empty queues.");
            }
        }

        #endregion

        /// <summary>
        /// Tests the send and receive with topic single callback.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveWithTopicSingleCallback()
        {
            var admin = new RabbitAdmin(this.connectionFactory);
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);
            this.template.Exchange = exchange.Name;

            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange).With("*.end"));

            this.template.Execute<object>(
                delegate(IModel channel)
                {
                    var consumer = this.CreateConsumer(this.template);
                    var tag = consumer.ConsumerTag;
                    Assert.IsNotNull(tag);

                    this.template.ConvertAndSend("foo", "message");

                    try
                    {
                        var result = this.GetResult(consumer);
                        Assert.AreEqual(null, result);

                        this.template.ConvertAndSend("foo.end", "message");
                        result = this.GetResult(consumer);
                        Assert.AreEqual("message", result);
                    }
                    finally
                    {
                        try
                        {
                            channel.BasicCancel(tag);
                        }
                        catch (Exception e)
                        {
                            // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                            Console.WriteLine(e.Message);
                        }
                    }

                    return null;
                });
        }

        /// <summary>
        /// Tests the send and receive with non default exchange.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveWithNonDefaultExchange()
        {
            var admin = new RabbitAdmin(this.connectionFactory);
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);

            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange).With("*.end"));

            this.template.Execute<object>(
                delegate(IModel channel)
                {
                    var consumer = this.CreateConsumer(this.template);
                    var tag = consumer.ConsumerTag;
                    Assert.IsNotNull(tag);

                    this.template.ConvertAndSend("topic", "foo", "message");

                    try
                    {
                        var result = this.GetResult(consumer);
                        Assert.AreEqual(null, result);

                        this.template.ConvertAndSend("topic", "foo.end", "message");
                        result = this.GetResult(consumer);
                        Assert.AreEqual("message", result);
                    }
                    finally
                    {
                        try
                        {
                            channel.BasicCancel(tag);
                        }
                        catch (Exception e)
                        {
                            // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                            Console.WriteLine(e.Message);
                        }
                    }

                    return null;
                });
        }

        /// <summary>
        /// Tests the send and receive with topic consume in background.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveWithTopicConsumeInBackground()
        {
            var admin = new RabbitAdmin(this.connectionFactory);
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);
            this.template.Exchange = exchange.Name;

            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange).With("*.end"));

            var template = new RabbitTemplate(new CachingConnectionFactory());
            template.Exchange = exchange.Name;

            var consumer = this.template.Execute(
                delegate
                {
                    var consumerinside = this.CreateConsumer(template);
                    var tag = consumerinside.ConsumerTag;
                    Assert.IsNotNull(tag);

                    return consumerinside;
                });

            template.ConvertAndSend("foo", "message");
            var result = this.GetResult(consumer);
            Assert.AreEqual(null, result);

            this.template.ConvertAndSend("foo.end", "message");
            result = this.GetResult(consumer);
            Assert.AreEqual("message", result);

            try
            {
                consumer.Channel.BasicCancel(consumer.ConsumerTag);
            }
            catch (Exception e)
            {
                // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Tests the send and receive with topic two callbacks.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        [Ignore("Need to fix")]
        public void TestSendAndReceiveWithTopicTwoCallbacks()
        {
            var admin = new RabbitAdmin(this.connectionFactory);
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);
            this.template.Exchange = exchange.Name;

            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange).With("*.end"));

            this.template.Execute<object>(
                delegate(IModel channel)
                {
                    var consumer = this.CreateConsumer(this.template);
                    var tag = consumer.ConsumerTag;
                    Assert.IsNotNull(tag);

                    try
                    {
                        this.template.ConvertAndSend("foo", "message");
                        var result = this.GetResult(consumer);
                        Assert.AreEqual(null, result);
                    }
                    finally
                    {
                        try
                        {
                            channel.BasicCancel(tag);
                        }
                        catch (Exception e)
                        {
                            // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                            Console.WriteLine(e.Message);
                        }
                    }

                    return null;
                });

            this.template.Execute<object>(
                delegate(IModel channel)
                {
                    var consumer = this.CreateConsumer(this.template);
                    var tag = consumer.ConsumerTag;
                    Assert.IsNotNull(tag);

                    try
                    {
                        // TODO: Bug here somewhere...
                        this.template.ConvertAndSend("foo.end", "message");
                        var result = this.GetResult(consumer);
                        Assert.AreEqual("message", result);
                    }
                    finally
                    {
                        try
                        {
                            channel.BasicCancel(tag);
                        }
                        catch (Exception e)
                        {
                            // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                            Console.WriteLine(e.Message);
                        }
                    }

                    return null;
                });
        }

        /// <summary>
        /// Tests the send and receive with fanout.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveWithFanout()
        {
            var admin = new RabbitAdmin(this.connectionFactory);
            var exchange = new FanoutExchange("fanout");
            admin.DeclareExchange(exchange);
            this.template.Exchange = exchange.Name;

            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange));

            this.template.Execute<object>(
                delegate(IModel channel)
                {
                    var consumer = this.CreateConsumer(this.template);
                    var tag = consumer.ConsumerTag;
                    Assert.IsNotNull(tag);

                    try
                    {
                        this.template.ConvertAndSend("message");
                        var result = this.GetResult(consumer);
                        Assert.AreEqual("message", result);
                    }
                    finally
                    {
                        try
                        {
                            channel.BasicCancel(tag);
                        }
                        catch (Exception e)
                        {
                            // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                            Console.WriteLine(e.Message);
                        }
                    }

                    return null;
                });
        }

        /// <summary>Creates the consumer.</summary>
        /// <param name="accessor">The accessor.</param>
        /// <returns>The consumer.</returns>
        /// <remarks></remarks>
        private BlockingQueueConsumer CreateConsumer(RabbitAccessor accessor)
        {
            var consumer = new BlockingQueueConsumer(accessor.ConnectionFactory, new DefaultMessagePropertiesConverter(), new ActiveObjectCounter<BlockingQueueConsumer>(), AcknowledgeModeUtils.AcknowledgeMode.Auto, true, 1, queue.Name);
            consumer.Start();
            return consumer;
        }

        /// <summary>Gets the result.</summary>
        /// <param name="consumer">The consumer.</param>
        /// <returns>The result.</returns>
        /// <remarks></remarks>
        private string GetResult(BlockingQueueConsumer consumer)
        {
            var response = consumer.NextMessage(new TimeSpan(0, 0, 0, 20));
            if (response == null)
            {
                return null;
            }

            return (string)new SimpleMessageConverter().FromMessage(response);
        }
    }
}
