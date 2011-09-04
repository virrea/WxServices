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
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Reflection;

using Nini.Config;
using log4net;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Services;
using Wx.Services.Interfaces;
using Wx.Server;
using Wx.Data;

using OpenSim.Data;

namespace Wx.Server.Handlers
{
    // This is providing the endpoints for our applicaion server.
    public class WxUsersHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected IUserAccountService m_UserAccountService = null;
        //protected IWxUserService m_WxService = null;

        //
        // Here we go...
        //
        // WxServiceConnector creates an instance of our WxService and hands it to us
        // as it creates our instance...
        // Our instance gets passed to the server, then we're live and ready for action
        //
        public WxUsersHandler(IUserAccountService service)
            : base("POST", "/WxUser")
        {
            m_UserAccountService = service;
            m_log.Info("[WxUsersHandler]: Loading");
        }

        public override byte[] Handle(string path, Stream requestData, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            try
            {
                Dictionary<string, object> request = ServerUtils.ParseQueryString(body);

                m_log.DebugFormat("[WxUserHandler]: Handler {0}", body.ToString());

                if (!request.ContainsKey("METHOD"))
                    return FailureResult("Error, no method defined!");
                string method = request["METHOD"].ToString();

                // Look for our caller's method...
                switch (method)
                {
                    case "testing":
                        return TestResponse(request);

                    case "create_user":
                        return CreateUser(request);

                    case "get_user_by_name":
                        return GetUserByName(request);

                    case "get_user_by_id":
                        return GetUserById(request);

                    case "get_user_by_email":
                        return GetUserByEmail(request);

                    default:
                        m_log.DebugFormat("[WxUserHandler]: unknown method {0} request {1}", method.Length, method);
                        return FailureResult("WxUsersHandler: Unrecognized method requested!");
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[Wx HANDLER]: Exception {0}", e);
            }

            return FailureResult();
        }

        #region utility
        private byte[] FailureResult()
        {
            return FailureResult(String.Empty);
        }

        private byte[] FailureResult(string msg)
        {
            m_log.ErrorFormat("[WxUserHandler] " + msg);
            OSDMap doc = new OSDMap(2);
            doc["Result"] = OSD.FromString("Failure");
            doc["Message"] = OSD.FromString(msg);

            return DocToBytes(doc);
        }

        private byte[] SuccessResult(OSDMap response)
        {
            response["Result"] = OSD.FromString("Success");
            return DocToBytes(response);
        }

        private byte[] DocToBytes(OSDMap doc)
        {
            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(doc));
        }
        #endregion utility

        #region Handler Methods

        /// <summary>
        /// Creates a new user
        /// Required parameters : first_name, last_name, email
        /// Optional parameters : scope_id, user_flags, user_level, user_title
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] CreateUser(Dictionary<string, object> request)
        {
            try
            {
                // Checking required parameters for creating a user account
                if (request.ContainsKey("first_name") &&
                    request.ContainsKey("last_name") &&
                    request.ContainsKey("email"))
                {
                    string firstName = request["first_name"].ToString();
                    string lastName = request["last_name"].ToString();
                    string email = request["email"].ToString();

                    UUID scopeId = UUID.Zero;
                    if (request.ContainsKey("scope_id")) scopeId = UUID.Parse(request["scope_id"].ToString());

                    if (m_UserAccountService.GetUserAccount(scopeId, firstName, lastName) == null)
                    {
                        UserAccount userAccount = new UserAccount(UUID.Zero, firstName, lastName, email);

                        if (request.ContainsKey("scope_id")) userAccount.ScopeID = scopeId;
                        if (request.ContainsKey("user_flags")) userAccount.UserFlags = int.Parse(request["user_flags"].ToString());
                        if (request.ContainsKey("user_level")) userAccount.UserLevel = int.Parse(request["user_level"].ToString());
                        if (request.ContainsKey("user_title")) userAccount.UserTitle = request["user_title"].ToString();

                        userAccount.ServiceURLs["HomeURI"] = string.Empty;
                        userAccount.ServiceURLs["GatekeeperURI"] = string.Empty;
                        userAccount.ServiceURLs["InventoryServerURI"] = string.Empty;
                        userAccount.ServiceURLs["AssetServerURI"] = string.Empty;

                        m_UserAccountService.StoreUserAccount(userAccount);
                        m_log.InfoFormat("[WxUserHandler] Created user {0} {1} with UUID {2}", userAccount.FirstName, userAccount.LastName, userAccount.PrincipalID);

                        OSDMap response = new OSDMap();
                        return SuccessResult(response);
                    }
                    else
                        return FailureResult("A user with the same first and last names already exists");
                }
                else
                    return FailureResult("Some or all required parameters missing");
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WxUserHandler] " + e.ToString());
                return FailureResult("Exception while creating user");
            }
        }

        /// <summary>
        /// Gets user info by name
        /// Required parameters : first_name, last_name
        /// Optional parameters : scope_id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] GetUserByName(Dictionary<string, object> request)
        {
            try
            {
                // Checking required parameters for creating a user account
                if (request.ContainsKey("first_name") &&
                    request.ContainsKey("last_name"))
                {
                    string firstName = request["first_name"].ToString();
                    string lastName = request["last_name"].ToString();

                    UUID scopeId = UUID.Zero;
                    if (request.ContainsKey("scope_id")) scopeId = UUID.Parse(request["scope_id"].ToString());

                    UserAccount userAccount = m_UserAccountService.GetUserAccount(scopeId, firstName, lastName);
                    if (userAccount != null)
                    {
                        m_log.InfoFormat("[WxUserHandler] Getting user info for {0} {1}", userAccount.FirstName, userAccount.LastName);

                        OSDMap response = new OSDMap();
                        foreach (KeyValuePair<string, object> data in userAccount.ToKeyValuePairs())
                            response.Add(data.Key.ToString(), data.Value.ToString());

                        return SuccessResult(response);
                    }
                    else
                        return FailureResult("Not found");
                }
                else
                    return FailureResult("Some or all required parameters missing");
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WxUserHandler] " + e.ToString());
                return FailureResult("Exception while getting user info");
            }
        }

        /// <summary>
        /// Gets uesr info by PrincipalId
        /// Required parameters : principal_id
        /// Optional parameters : scope_id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] GetUserById(Dictionary<string, object> request)
        {
            try
            {
                // Checking required parameters for creating a user account
                if (request.ContainsKey("principal_id"))
                {
                    UUID principalId = UUID.Parse(request["principal_id"].ToString());

                    UUID scopeId = UUID.Zero;
                    if (request.ContainsKey("scope_id")) scopeId = UUID.Parse(request["scope_id"].ToString());

                    UserAccount userAccount = m_UserAccountService.GetUserAccount(scopeId, principalId);
                    if (userAccount != null)
                    {
                        m_log.InfoFormat("[WxUserHandler] Getting user info for {0} {1}", userAccount.FirstName, userAccount.LastName);

                        OSDMap response = new OSDMap();
                        foreach (KeyValuePair<string, object> data in userAccount.ToKeyValuePairs())
                            response.Add(data.Key.ToString(), data.Value.ToString());

                        return SuccessResult(response);
                    }
                    else
                        return FailureResult("Not found");
                }
                else
                    return FailureResult("Some or all required parameters missing");
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WxUserHandler] " + e.ToString());
                return FailureResult("Exception while getting user info");
            }
        }

        /// <summary>
        /// Gets uesr info by email
        /// Required parameters : email
        /// Optional parameters : 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] GetUserByEmail(Dictionary<string, object> request)
        {
            try
            {
                // Checking required parameters for creating a user account
                if (request.ContainsKey("email"))
                {
                    string email = request["email"].ToString();

                    UUID scopeId = UUID.Zero;
                    if (request.ContainsKey("scope_id")) scopeId = UUID.Parse(request["scope_id"].ToString());

                    UserAccount userAccount = m_UserAccountService.GetUserAccount(scopeId, email);
                    if (userAccount != null)
                    {
                        m_log.InfoFormat("[WxUserHandler] Getting user info for {0} {1}", userAccount.FirstName, userAccount.LastName);

                        OSDMap response = new OSDMap();
                        foreach (KeyValuePair<string, object> data in userAccount.ToKeyValuePairs())
                            response.Add(data.Key.ToString(), data.Value.ToString());

                        return SuccessResult(response);
                    }
                    else
                        return FailureResult("Not found");
                }
                else
                    return FailureResult("Some or all required parameters missing");
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WxUserHandler] " + e.ToString());
                return FailureResult("Exception while getting user info");
            }
        }

        private byte[] TestResponse(Dictionary<string, object> request)
        {
            OSDMap doc = new OSDMap(request.Count + 1);
            if ( request.ContainsKey("HELLO"))
            {
                m_log.InfoFormat("[Wx]: Users Testing {0}", request["HELLO"].ToString());
                doc["Greeting"] = OSD.FromString("Goodbye!");

                foreach (KeyValuePair<string, object> item in request) {

                    doc[item.Key] = OSD.FromString(item.Value.ToString());

                }

                return DocToBytes(doc);
            }
            return FailureResult("You must say HELLO!");
        }

        #endregion Handler Methods

        #region Wx Database
        //private byte[] PutWxUser(Dictionary<string, object> request)
        //{
        //    IWxUserData data = m_WxService.WxDb;

        //    UserData user = new UserData();

        //    user.FirstName = request["first_name"].ToString();
        //    user.LastName = request["last_name"].ToString();
        //    user.Food = request["fav_food"].ToString();

        //    data.StoreName(user);
        //    OSDMap doc = new OSDMap();

        //    doc["result"] = "success";
        //    return DocToBytes(doc);
        //}

        //private byte[] ListWxUser()
        //{
        //    IWxUserData data = m_WxService.WxDb;

        //    List<UserData> list = data.ListNames();
        //    OSDMap doc = new OSDMap();

        //    foreach ( UserData u in list )
        //    {
        //        OSDMap udata = new OSDMap();
        //        string uname = String.Format("{0} {1}", u.FirstName, u.LastName);
        //        string ufood = u.Food.ToString();

        //        udata["name"] = OSD.FromString(uname);
        //        udata["food"] = OSD.FromString(ufood);
        //        doc.Add(uname, udata);
        //    }

        //    return DocToBytes(doc);
        //}
        #endregion Wx Database

        #region User Functions
        //public UserAccount GetUserData(UUID userID) {

        //    UserAccount userInfo = null;

        //    userInfo = m_WxService.GetUserData(userID);

        //    return userInfo;
        //}
        #endregion User Functions
    }
}

