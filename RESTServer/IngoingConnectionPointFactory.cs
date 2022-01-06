using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESB_ConnectionPoints.PluginsInterfaces;
using ESB_ConnectionPoints.Utils;

namespace RESTServer
{
    public sealed class IngoingConnectionPointFactory : IIngoingConnectionPointFactory
    {
        public IIngoingConnectionPoint Create(
            Dictionary<string, string> parameters, 
            IServiceLocator serviceLocator)
        {
            return (IIngoingConnectionPoint) new IngoingConnectionPoint(parameters.GetStringParameter("Настройки в формате JSON"), 
                parameters.GetBoolParameter("Режим отладки", false), serviceLocator);
        }
    }
}
