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
using OpenSim.Framework;
using OpenSim.Data;
using Nini.Config;
using OpenSim.Services.Interfaces;
using OpenSim.Services.Base;
using Wx.Data;

namespace Wx.Services.WxUserService
{
    // We handle our own configuration here...
    public class WxUserServiceBase : ServiceBase
    {
        protected IWxUserData m_Database = null;
        protected string connString = null;
        protected string realm = null;

        public WxUserServiceBase(IConfigSource config)
            : base(config)
        {
            string dllName = String.Empty;

            // Try reading the [DatabaseService] section, if it exists
            //
            IConfig dbConfig = config.Configs["DatabaseService"];
            if (dbConfig != null)
            {
                if (dllName == String.Empty)
                    dllName = dbConfig.GetString("StorageProvider", String.Empty);
                if (connString == String.Empty)
                    connString = dbConfig.GetString("ConnectionString", String.Empty);
            }
            //
            // [WxService] section overrides [DatabaseService], if it exists
            //
            IConfig WxConfig = config.Configs["WxUserService"];
            if (WxConfig != null)
            {
                // Look at the ini [WxService] to see the database setup.
                // We give Wx.Data.dll as the StorageProvider, so we load
                // our custom database handler.
                // Have a look at Wx.Data. We do migrations on load.
                //
                dllName = WxConfig.GetString("StorageProvider", dllName);
                connString = WxConfig.GetString("ConnectionString", connString);
                realm = WxConfig.GetString("Realm", realm);
            }
            //
            // We tried, but this doesn't exist. We can't proceed.
            //
            if (dllName.Equals(String.Empty))
                throw new Exception("No StorageProvider configured");

            // This is our example database
            // Look at Wx.Data
            m_Database = LoadPlugin<IWxUserData>(dllName, new Object[] { connString, realm });
            if (m_Database == null)
                throw new Exception("Could not find a storage interface in the given module");

        }
    }
}


