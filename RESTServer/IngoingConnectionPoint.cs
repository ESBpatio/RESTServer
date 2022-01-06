using System;
using System.Threading;
using ESB_ConnectionPoints.PluginsInterfaces;
using Ceen;
using Ceen.Httpd;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RESTServer
{
    public sealed class IngoingConnectionPoint : IStandartIngoingConnectionPoint, IIngoingConnectionPoint
    {
        private readonly ServerConfig serverConfig;
        private int port;
        private IPAddress address;
        private Task listeningServerTask;
        private ApiController controller;
        public IMessageFactory MessageFactory { get; }
        public ESB_ConnectionPoints.PluginsInterfaces.ILogger Logger { get; }
        public bool IsDebugMode { get; }

        public IngoingConnectionPoint(string jsonSettings, bool debugMode, IServiceLocator serviceLocator)
        {
            ServerConfig serverConfig = new ServerConfig();
            serverConfig.AutoParseMultipartFormData = false;
            serverConfig.MaxActiveRequests = 10;
            serverConfig.MaxPostSize = 204800L;
            serverConfig.MaxProcessingTimeSeconds = 20;
            this.serverConfig = serverConfig;
            this.port = 8080;
            this.address = IPAddress.Any;

            if (string.IsNullOrEmpty(jsonSettings))
                throw new ArgumentException("Отсутсвуют настройки <jsonSettings>");
            if(serviceLocator != null)
            {
                this.Logger = serviceLocator.GetLogger(this.GetType());
                this.MessageFactory = serviceLocator.GetMessageFactory();
            }
            this.IsDebugMode = debugMode;
            this.ParseSettings(jsonSettings);
        }

        private void ParseSettings(string settings)
        {
            JObject jObject;
            try
            {
                jObject = JObject.Parse(settings);
            }
            catch (Exception ex)
            {

                throw new Exception(string.Format("Не удалось разобрать JSON настройки! \n {0}", ex.Message));
            }
            string ipString = JsonUtils.StringValue(jObject, "Network.Address");
            if(!string.IsNullOrEmpty(ipString))
            {
                try
                {
                    this.address = IPAddress.Parse(ipString);
                }
                catch (Exception ex)
                {

                    throw new ArgumentException(string.Format("Не удалось разобрать адрес : {0} \n {1}",ipString, ex));
                }
            }
            this.port = JsonUtils.IntValue(jObject, "Network.Port");
            if (this.port == 0)
                throw new ArgumentException("Порт не задан!");
            this.serverConfig.MaxRequestLineSize = JsonUtils.IntValue(jObject, "MaxRequestLineSize", this.serverConfig.MaxRequestLineSize);
            this.serverConfig.MaxRequestHeaderSize = JsonUtils.IntValue(jObject, "MaxRequestHeaderSize", this.serverConfig.MaxRequestHeaderSize);
            this.serverConfig.MaxActiveRequests = (JsonUtils.IntValue(jObject, "MaxActiveRequests", this.serverConfig.MaxActiveRequests));
            this.serverConfig.MaxUrlEncodedFormSize = (JsonUtils.IntValue(jObject, "MaxUrlEncodedFormSize", this.serverConfig.MaxUrlEncodedFormSize));
            this.serverConfig.MaxPostSize = ((long)JsonUtils.IntValue(jObject, "MaxPostSize", (int)this.serverConfig.MaxPostSize));
            this.serverConfig.RequestIdleTimeoutSeconds = (JsonUtils.IntValue(jObject, "RequestIdleTimeoutSeconds", this.serverConfig.RequestIdleTimeoutSeconds));
            this.serverConfig.RequestHeaderReadTimeoutSeconds = (JsonUtils.IntValue(jObject, "RequestHeaderReadTimeoutSeconds", this.serverConfig.RequestHeaderReadTimeoutSeconds));
            this.serverConfig.KeepAliveMaxRequests = (JsonUtils.IntValue(jObject, "KeepAliveMaxRequests", this.serverConfig.KeepAliveMaxRequests));
            this.serverConfig.KeepAliveTimeoutSeconds = (JsonUtils.IntValue(jObject, "KeepAliveTimeoutSeconds", this.serverConfig.KeepAliveTimeoutSeconds));
            this.serverConfig.MaxProcessingTimeSeconds = (JsonUtils.IntValue(jObject, "MaxProcessingTimeSeconds", this.serverConfig.MaxProcessingTimeSeconds));
        }
        private void InitServerListeningTask(
            IMessageHandler messageHandler,
            CancellationToken ct)
        {
            lock(this)
            {
                if (this.controller == null)
                    this.controller = new ApiController(this, messageHandler);
                if (this.listeningServerTask != null)
                    return;
                this.serverConfig.AddRoute((IHttpModule)this.controller);
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        this.listeningServerTask = HttpServer.ListenAsync(
                            new IPEndPoint(this.address, this.port), false, this.serverConfig, ct);
                        break;
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Error(string.Format("Не удалось запустить REST-сервер \n {0}", ex));
                        ct.WaitHandle.WaitOne(TimeSpan.FromSeconds(10.0));
                    }
                }
            }
        }

        public void StartListener(
            IMessageHandler messageHandler,
            CancellationToken ct)
        {
            this.InitServerListeningTask(messageHandler, ct);
        }
        public void Run(IMessageHandler messageHandler, CancellationToken ct)
        {
            this.InitServerListeningTask(messageHandler, ct);
            if (this.listeningServerTask == null)
                return;
            this.listeningServerTask.Wait();
        }
        public void LoggerDebug(string message)
        {
            if (!this.IsDebugMode)
                return;
            this.Logger.Debug(message);
        }
        public void Process(
            Message message,
            IMessageSource messageSource,
            IMessageReplyHandler replyHandler,
            CancellationToken ct)
        {
            //return Task.Factory.StartNew((Action)(() => this.controller.ProcessReplyMessage(message)));
        }
        public void Cleanup()
        {

        }

        public void Dispose()
        {

        }

        public void Initialize()
        {

        }


    }
}
