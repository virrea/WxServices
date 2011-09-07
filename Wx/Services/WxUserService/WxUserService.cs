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
using System.Reflection;
using System.Collections.Generic;
using Nini.Config;
using log4net;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Data;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenMetaverse;

using Wx.Data;
using Wx.Services.Interfaces;

namespace Wx.Services.WxUserService
{
    public class WxUserService : WxUserServiceBase, IWxUserService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected IConfigSource m_config;
        protected IUserAccountService m_UserAccountService = null;

        #region properties
        // Let other classes use our custom database
        // see: Wx.Data
        public IWxUserData WxDb
        {
            get
            {
                return m_Database;
            }
        }
        #endregion

        // Our service
        // see WxServiceBase. That is where most of the configuration
        // for this is done. In this file we are setting up the external
        // and internal services our endpoints will use...
        public WxUserService(IConfigSource config)
            : base (config)
        {
            m_log.Info("[WxUserService]: Wx Loading ... ");
            m_config = config;
            IConfig WxConfig = config.Configs["WxUserService"];
            if (WxConfig != null)
            {
                // loading the UserAccountService so we can use it's methods in our example
                // see below: GetUserData(UUID userID)
                //
                // Read the configuration...
                string userService = WxConfig.GetString("UserAccountService", String.Empty);

                // Load it...
                if (userService != String.Empty)
                {
                    Object[] args = new Object[] { config };
                    m_UserAccountService = ServerUtils.LoadPlugin<IUserAccountService>(userService, args);
                }
            }

            // Add a command to the console
            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand("Wx", true,
                            "show names",
                            "show names",
                            "Show list of names",
                            String.Empty,
                            HandleShowNames);
            }
        }

        #region Service handlers

        // Uses the OpenSim core user services
        // We have [UserAccountService] in our ini to handle the configuration
        // when we load it.
        public UserAccount GetUserData(UUID userID) {

            UserAccount userInfo = null;

            userInfo = m_UserAccountService.GetUserAccount(UUID.Zero, userID);

            return userInfo;
        }

        public bool CreateUser(string firstName, string lastName, string email, UUID scopeId, int userFlags, int userLevel, string userTitle, Dictionary<string, object> servicesUrls)
        {
            if (m_UserAccountService.GetUserAccount(scopeId, firstName, lastName) == null)
            {
                UserAccount userAccount = new UserAccount(scopeId, firstName, lastName, email);
                if (userFlags != null) userAccount.UserFlags = userFlags;
                if (userLevel != null) userAccount.UserLevel = userLevel;
                if (userTitle != null) userAccount.UserTitle = userTitle;
                if (servicesUrls != null)
                    userAccount.ServiceURLs = servicesUrls;
                else
                {
                    userAccount.ServiceURLs["HomeURI"] = string.Empty;
                    userAccount.ServiceURLs["GatekeeperURI"] = string.Empty;
                    userAccount.ServiceURLs["InventoryServerURI"] = string.Empty;
                    userAccount.ServiceURLs["AssetServerURI"] = string.Empty;
                }

                m_UserAccountService.StoreUserAccount(userAccount);
                return true;
            }
            else
                return false;
        }

        public bool UpdateUser(UUID principalId, string firstName, string lastName, string email, UUID scopeId, int userFlags, int userLevel, string userTitle, Dictionary<string, object> servicesUrls)
        {
            UserAccount userAccount = m_UserAccountService.GetUserAccount(scopeId, principalId);
            if (userAccount != null)
            {
                userAccount.FirstName = firstName;
                userAccount.LastName = lastName;
                userAccount.Email = email;
                userAccount.ScopeID = scopeId;
                userAccount.UserFlags = userFlags;
                userAccount.UserLevel = userLevel;
                userAccount.UserTitle = userTitle;
                userAccount.ServiceURLs = servicesUrls;

                m_UserAccountService.StoreUserAccount(userAccount);
                return true;
            }
            else
                return false;
        }

        public UserAccount GetUserByName(string firstName, string lastName, UUID scopeId)
        {
            return m_UserAccountService.GetUserAccount(scopeId, firstName, lastName);
        }

        public UserAccount GetUserByEmail(string email, UUID scopeId)
        {
            return m_UserAccountService.GetUserAccount(scopeId, email);
        }

        public UserAccount GetUserById(UUID principalId, UUID scopeId)
        {
            return m_UserAccountService.GetUserAccount(scopeId, principalId);
        }

        public List<UserAccount> GetUsersByQuery(string query, UUID scopeId)
        {
            return m_UserAccountService.GetUserAccounts(scopeId, query);
        }

        #endregion

        #region console handlers
        private void HandleShowNames(string module, string[] cmd)
        {
            if ( cmd.Length < 2 )
            {
                MainConsole.Instance.Output("Syntax: show name");
                return;
            }

            List<UserData> list = m_Database.ListNames();

            foreach (UserData name in list)
            {
                MainConsole.Instance.Output(String.Format("{0} {1}",name.FirstName, name.LastName));
            }
        }
        #endregion
    }
}
