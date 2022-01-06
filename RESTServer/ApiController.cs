using Ceen;
using System;
using System.Threading.Tasks;
using ESB_ConnectionPoints.PluginsInterfaces;

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
            var a = context.Request.Method;
            return true;
        }
    }
}
