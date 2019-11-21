﻿using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Core.Abstract;
using RabbitMQ.Core.Consts;
using RabbitMQ.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Core.Concrete
{
    public class PublisherManager : IPublisherService
    {
        private readonly IRabbitMQService _rabbitMQServices;
        private readonly IObjectConvertFormat _objectConvertFormat;
        public PublisherManager(IRabbitMQService rabbitMQServices, IObjectConvertFormat objectConvertFormat)
        {
            _rabbitMQServices = rabbitMQServices;
            _objectConvertFormat = objectConvertFormat;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueDataModels">Herhangi bir tipte gönderilebilir where koşullaırına uyan</param>
        /// <param name="queueName">Queue kuyrukta hangi isimde tutulacağı bilgisi operasyon istek zamanı gönderilebilir.</param>
        public void Enqueue<T>(IEnumerable<T> queueDataModels, string queueName) where T : class, new()
        {
            try
            {
                using (var channel = _rabbitMQServices.GetModel())
                {
                    channel.QueueDeclare(queue: queueName,
                                         durable: true,      // ile in-memory mi yoksa fiziksel olarak mı saklanacağı belirlenir.
                                         exclusive: false,   // yalnızca bir bağlantı tarafından kullanılır ve bu bağlantı kapandığında sıra silinir - özel olarak işaretlenirse silinmez
                                         autoDelete: false,  // en son bir abonelik iptal edildiğinde en az bir müşteriye sahip olan kuyruk silinir
                                         arguments: null);   // isteğe bağlı; eklentiler tarafından kullanılır ve TTL mesajı, kuyruk uzunluğu sınırı, vb. 

                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.Expiration = RabbitMQConsts.MessagesTTL.ToString();

                    foreach (var queueDataModel in queueDataModels)
                    {
                        var body = Encoding.UTF8.GetBytes(_objectConvertFormat.ObjectToJson(queueDataModel));
                        channel.BasicPublish(exchange: "",
                                             routingKey: queueName,
                                             basicProperties: properties,
                                             body: body);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.InnerException.Message.ToString());
            }
        }
    }
}


