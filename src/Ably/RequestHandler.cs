using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ably
{
    internal class RequestHandler : IRequestHandler
    {
        public byte[] GetRequestBody(AblyRequest request)
        {
            if(request.PostData == null)
                return new byte[] {};

            if (request.PostData is Message)
                return GetMessagesRequestBody(new[] {request.PostData as Message},
                    request.Encrypted, request.CipherParams);
            if (request.PostData is IEnumerable<Message>)
                return GetMessagesRequestBody(request.PostData as IEnumerable<Message>, 
                    request.Encrypted, request.CipherParams);

            return JsonConvert.SerializeObject(request.PostData).GetBytes();
        }

        private byte[] GetMessagesRequestBody(IEnumerable<Message> messages, bool encrypted, CipherParams @params)
        {
            return GetMessagesRequestBody(messages, false, encrypted, @params);
        }

        byte[] GetMessagesRequestBody(IEnumerable<Message> messages, bool useTextProtocol, bool encrypted, CipherParams @params)
        {
                var payloads = messages.Select(message => CreateMessagePayload(message, encrypted, @params));

                var text = messages.Count() == 1 ? JsonConvert.SerializeObject(payloads.First()) : JsonConvert.SerializeObject(payloads);
                return text.GetBytes();
        }

        internal static MessagePayload CreateMessagePayload(Message message, bool encrypted, CipherParams @params)
        {
            var payload = new MessagePayload()
            {
                Name = message.Name,
                Timestamp = message.TimeStamp.DateTime.ToUnixTime()
            };

            if (encrypted)
            {
                var cipher = Config.GetCipher(@params);
                payload.Type = "";
                payload.Data = null;
                payload.Encoding = MessagePayload.EncryptedEncoding;
            }
            else if (message.IsBinaryMessage)
            {
                payload.Data = message.Value<byte[]>().ToBase64();
                payload.Encoding = MessagePayload.Base64Encoding;
            }
            else
            {
                payload.Data = GetDataString(message.Data);
            }
            return payload;
        }

        private static string GetDataString(object data)
        {
            var jobject = JObject.FromObject(new {data});
            return jobject["data"].ToString();
        }
    }
}