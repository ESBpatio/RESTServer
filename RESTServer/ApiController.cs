using System;
using System.Threading.Tasks;
using ESB_ConnectionPoints.PluginsInterfaces;
using System.IO;
using System.Text;
using Ceen;
using System.Linq;
using System.Collections.Generic;

namespace RESTServer
{
    public class ApiController : IHttpModule
    {

        private IngoingConnectionPoint connectionPoint;
        private IMessageHandler messageHandler;

        public ApiController(
            IngoingConnectionPoint ingoingConnectionPoint,
            IMessageHandler messageHandler)
        {
            this.connectionPoint = ingoingConnectionPoint;
            this.messageHandler = messageHandler;
        }
        Task<bool> IHttpModule.HandleAsync(IHttpContext context) => 
            Task.Factory.StartNew<bool>((Func<bool>)(() => this.ProcessRequest(context)));

        private bool ProcessRequest(IHttpContext context)
        {
            byte[] body = new byte[0];
            context.Response.SetNonCacheable();
            body = this.GetRequestBody(context);
            Message message = CreateMessage(context, body);

            if (message == null)
            {
                context.Response.StatusCode = (HttpStatusCode)500;
                context.Response.WriteAllAsync(Encoding.UTF8.GetBytes(string.Format("Произошла ошибка формирования сообщения"))).Wait();
                return false;
            }

            this.messageHandler.HandleMessage(message);           
            return true;
        }

        private void SendError(IHttpContext context, HttpStatusCode code)
        {
            this.connectionPoint.LoggerDebug(string.Format("Отправляем сообщение об ошибке {0}", (object)code));
            try
            {
                context.Response.StatusCode = code;
                context.Response.WriteAllAsync(Encoding.UTF8.GetBytes(string.Format("{0} {1}", (object)(int)code, (object)code.ToString())), (string)null).Wait();

            }
            catch (Exception ex)
            {

                this.connectionPoint.Logger.Error("Не удалось отправить сообщение об ошибке", ex);
            }
        }
        private string PrepareHTTPResponceProperty(string property)
        {
            string str = property.Replace("Properties_", "");
            str.Replace('-', '_').ToLower();
            return str;    
        }
        private byte[] GetRequestBody(IHttpContext context)
        {
            try
            {
                if (context.Request.ContentType == "application/x-www-form-urlencoded" && context.Request.Form.Count > 0)
                    return Encoding.UTF8.GetBytes("{" + string.Join(",", context.Request.Form.Select<KeyValuePair<string, string>, string>((Func<KeyValuePair<string, string>, string>)(form => "\"" + form.Key + "\": \"" + form.Value + "\""))) + "}");
                if (context.Request.Body.Length >= 0L)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        context.Request.Body.CopyTo((Stream)memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                this.connectionPoint.Logger.Error("Не удалось получить тело HTTP-запроса", ex);
            }
            return (byte[])null;
        }
        private Message CreateMessage(IHttpContext context , byte[] body)
        {
            this.connectionPoint.LoggerDebug("Выполняется формирования сообщения");
            try
            {
                Message message = this.connectionPoint.MessageFactory.CreateMessage("Request");
                message.AddPropertyWithValue<string>("Original_Path", context.Request.OriginalPath);
                message.AddPropertyWithValue<string>("Method", context.Request.Method);
                message.AddPropertyWithValue<string>("Path", context.Request.Path);
                foreach (KeyValuePair<string, string> header in (IEnumerable<KeyValuePair<string, string>>)context.Request.Headers)
                {
                    if (header.Key.Contains("Propertie_"))
                    {
                        string str = this.PrepareHTTPResponceProperty(header.Key);
                        message.AddPropertyWithValue<string>(str, header.Value);
                    }
                }
                if (body != null)
                    message.Body = body;
                return message;
            }
            catch (Exception ex)
            {

                this.connectionPoint.Logger.Error("Ошибка при формирование сообщения", ex);
                return (Message)null;
            }
        }
    }
}
