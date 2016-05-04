﻿using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NUnit.Framework;

public class When_sending_databus_properties
{
    [Test]
    public async Task Should_receive_the_message_the_largeproperty_correctly()
    {
        var endpointConfiguration = new EndpointConfiguration("AzureBlobStorageDataBus.Test");
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.UseSerialization<JsonSerializer>();

        endpointConfiguration.UseDataBus<AzureDataBus>()
            .ConnectionString(Environment.GetEnvironmentVariable("AzureStorageQueueTransport.ConnectionString"));

        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        endpointConfiguration.EnableInstallers();

        var endpoint = await Endpoint.Start(endpointConfiguration);

        await endpoint.SendLocal(new MyMessageWithLargePayload
        {
            Payload = new DataBusProperty<byte[]>(PayloadToSend)
        });

        ManualResetEvent.WaitOne(TimeSpan.FromSeconds(30));
        await endpoint.Stop();
        Assert.AreEqual(PayloadToSend, PayloadReceived, "The large payload should be marshalled correctly using the databus");
    }


    static byte[] PayloadToSend = new byte[1024 * 1024];
    static byte[] PayloadReceived;

    static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);

    public class MyMessageHandler : IHandleMessages<MyMessageWithLargePayload>
    {
        public Task Handle(MyMessageWithLargePayload message, IMessageHandlerContext context)
        {
            PayloadReceived = message.Payload.Value;
            ManualResetEvent.Set();
            return Task.FromResult(0);
        }
    }

    public class MyMessageWithLargePayload : ICommand
    {
        public DataBusProperty<byte[]> Payload { get; set; }
    }
}