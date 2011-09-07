/*
** Copyright 2011 BlueWall Information Technologies, LLC
**
**   Licensed under the Apache License, Version 2.0 (the "License");
**   you may not use this file except in compliance with the License.
**   You may obtain a copy of the License at
**
**       http://www.apache.org/licenses/LICENSE-2.0
**
**   Unless required by applicable law or agreed to in writing, software
**   distributed under the License is distributed on an "AS IS" BASIS,
**   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
**   See the License for the specific language governing permissions and
**   limitations under the License.
*/

using System;
using Nini.Config;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;
using Wx.Services.Interfaces;
using Wx.Server.Handlers;

namespace Wx.Connector
{
    public class WxUserServiceConnector : ServiceConnector
    {

        private IWxUserService m_WxUserService;
        private string m_ConfigName = "WxUserService";
        // Our connector
        // We load this first. See the ini [Startup] ServiceConnectors
        // We give the port we want our applications to connect to us, the
        // name of the dll (determined by the "name=Wx.Connector" in the
        // WxConnectors/prebuild.xml) and the name of the class in the
        // dll assembly that we want to use...
        // ServiceConnectors = "8114/Wx.Connector.dll:WxServiceConnector"
        //
        // In the [Network] section we have configured an ssl port on 8114
        // So, we will use https://* to talk to our application server.
        //
        // We load our Wx.Service (see: Wx.Service/Wx.ServiceBase)
        //
        // Then we pass our server the handler that is given the Wx.Service
        //
        public WxUserServiceConnector(IConfigSource config, IHttpServer server, string configName)
            : base(config, server, configName)
        {
            IConfig serverConfig = config.Configs[m_ConfigName];
            if (serverConfig == null)
                throw new Exception(String.Format("No section {0} in config file", m_ConfigName));

            string WxUserService = serverConfig.GetString("LocalServiceModule",
                    String.Empty);

            if (WxUserService == String.Empty)
                throw new Exception("No LocalServiceModule in config file");

            Object[] args = new Object[] { config };
            m_WxUserService = ServerUtils.LoadPlugin<IWxUserService>(WxUserService, args);

            server.AddStreamHandler(new WxUsersHandler(m_WxUserService));
        }
    }
}
