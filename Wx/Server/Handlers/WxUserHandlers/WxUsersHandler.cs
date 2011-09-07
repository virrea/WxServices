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


namespace Wx.Server.Handlers
{
    // This is providing the endpoints for our applicaion server.
    public class WxUsersHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected IUserAccountService m_userAccounts = null;
        protected IWxUserService m_WxService = null;

        //
        // Here we go...
        //
        // WxServiceConnector creates an instance of our WxService and hands it to us
        // as it creates our instance...
        // Our instance gets passed to the server, then we're live and ready for action
        //
        public WxUsersHandler(IWxUserService service) : base("POST", "/WxUser")
        {
            m_WxService = service;
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
                    #region Handlers

                    case "create_user":
                        return CreateUser(request);

                    case "update_user":
                        return UpdateUser(request);

                    case "get_user_by_name":
                        return GetUserByName(request);

                    case "get_user_by_email":
                        return GetUserByEmail(request);

                    case "get_user_by_id":
                        return GetUserById(request);

                    case "get_users_by_query":
                        return GetUsersByQuery(request);

                    #endregion Handlers

                    #region Example handlers

                    case "testing":
                        return TestResponse(request);

                    case "get_user_info":
                        return GetUserInfo(request);

                    case "put_wxuser":
                        return PutWxUser(request);

                    case "list_wxuser":
                        return ListWxUser();

                    #endregion Example Handlers

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

        #region Utility

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

        #endregion Utility


        #region Handler Methods

        /// <summary>
        /// Creates a new user
        /// Required parameters : first_name, last_name, email
        /// Optional parameters : scope_id, user_flags, user_level, user_title, services_urls
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

                    // Default parameters
                    // We can default parameters to a non-null value except servicesUrls which default value may be better handled in the WxService ?
                    UUID scopeId = UUID.Zero;
                    int userFlags = 0;
                    int userLevel = 0;
                    string userTitle = "";
                    Dictionary<string, object> servicesUrls = null;
                    
                    // Request parameters
                    if (request.ContainsKey("scope_id")) scopeId = UUID.Parse(request["scope_id"].ToString());
                    if (request.ContainsKey("user_flags")) userFlags = int.Parse(request["user_flags"].ToString());
                    if (request.ContainsKey("user_level")) userLevel = int.Parse(request["user_level"].ToString());
                    if (request.ContainsKey("user_title")) userTitle = request["user_title"].ToString();

                    bool success = m_WxService.CreateUser(firstName, lastName, email, scopeId, userFlags, userLevel, userTitle, servicesUrls);

                    if (success)
                    {
                        m_log.InfoFormat("[WxUserHandler] Created user {0} {1}", firstName, lastName);
                        return SuccessResult(new OSDMap());
                    }
                    else
                    {
                        m_log.ErrorFormat("[WxUserHandler] Could not create user {0} {1}", firstName, lastName);
                        return FailureResult("User already exists");
                    }
                }
                else
                    return FailureResult("Required parameter missing");
            }
            catch
            {
                return FailureResult("Internal error");
            }
        }

        /// <summary>
        /// Creates a new user
        /// Required parameters : principal_id, first_name, last_name, email, scope_id, user_flags, user_level, user_title, services_urls
        /// Optional parameters : 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] UpdateUser(Dictionary<string, object> request)
        {
            try
            {
                // Checking required parameters for creating a user account
                if (request.ContainsKey("principal_id") &&
                    request.ContainsKey("first_name") &&
                    request.ContainsKey("last_name") &&
                    request.ContainsKey("email") &&
                    request.ContainsKey("scope_id") &&
                    request.ContainsKey("user_flags") &&
                    request.ContainsKey("user_level") &&
                    request.ContainsKey("user_title") &&
                    request.ContainsKey("services_urls"))
                {
                    UUID principalId = UUID.Parse(request["principal_id"].ToString());
                    string firstName = request["first_name"].ToString();
                    string lastName = request["last_name"].ToString();
                    string email = request["email"].ToString();
                    UUID scopeId = UUID.Parse(request["scope_id"].ToString());
                    int userLevel = int.Parse(request["user_level"].ToString());
                    int userFlags = int.Parse(request["user_flags"].ToString());
                    string userTitle = request["user_title"].ToString();
                    Dictionary<string, object> servicesUrls = (Dictionary<string, object>)request["services_urls"];

                    bool success = m_WxService.UpdateUser(principalId, firstName, lastName, email, scopeId, userFlags, userLevel, userTitle, servicesUrls);
                    if (success)
                    {
                        m_log.InfoFormat("[WxUserHandler] Updated user {0} {1}", firstName, lastName);
                        return SuccessResult(new OSDMap());
                    }
                    else
                    {
                        m_log.ErrorFormat("[WxUserHandler] Could not update user {0} {1}", firstName, lastName);
                        return FailureResult("User does not exists");
                    }
                }
                else
                    return FailureResult("Required parameter missing");
            }
            catch
            {
                return FailureResult("Internal error");
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
                    // Required parameters
                    string firstName = request["first_name"].ToString();
                    string lastName = request["last_name"].ToString();

                    UUID scopeId = UUID.Zero;
                    if (request.ContainsKey("scope_id")) scopeId = UUID.Parse(request["scope_id"].ToString());

                    // Querying service
                    UserAccount userAccount = m_WxService.GetUserByName(firstName, lastName, scopeId);
                    if (userAccount != null)
                    {
                        m_log.InfoFormat("[WxUserHandler] Got user info for {0} {1}", userAccount.FirstName, userAccount.LastName);

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
                return FailureResult("Exception");
            }
        }

        /// <summary>
        /// Gets user info by email
        /// Required parameters : email
        /// Optional parameters : scope_id
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
                    // Required parameters
                    string email = request["email"].ToString();

                    UUID scopeId = UUID.Zero;
                    if (request.ContainsKey("scope_id")) scopeId = UUID.Parse(request["scope_id"].ToString());

                    // Querying service
                    UserAccount userAccount = m_WxService.GetUserByEmail(email, scopeId);
                    if (userAccount != null)
                    {
                        m_log.InfoFormat("[WxUserHandler] Got user info for {0} {1}", userAccount.FirstName, userAccount.LastName);

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
                return FailureResult("Exception");
            }
        }

        /// <summary>
        /// Gets user info by email
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
                    // Required parameters
                    UUID principalId = UUID.Parse(request["principal_id"].ToString());

                    UUID scopeId = UUID.Zero;
                    if (request.ContainsKey("scope_id")) scopeId = UUID.Parse(request["scope_id"].ToString());

                    // Querying service
                    UserAccount userAccount = m_WxService.GetUserById(principalId, scopeId);
                    if (userAccount != null)
                    {
                        m_log.InfoFormat("[WxUserHandler] Got user info for {0} {1}", userAccount.FirstName, userAccount.LastName);

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
                return FailureResult("Exception");
            }
        }

        /// <summary>
        /// Gets user info by email
        /// Required parameters : query
        /// Optional parameters : scope_id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] GetUsersByQuery(Dictionary<string, object> request)
        {
            try
            {
                // Checking required parameters for creating a user account
                if (request.ContainsKey("query"))
                {
                    // Required parameters
                    string query = request["query"].ToString();

                    UUID scopeId = UUID.Zero;
                    if (request.ContainsKey("scope_id")) scopeId = UUID.Parse(request["scope_id"].ToString());

                    // Querying service
                    List<UserAccount> userAccounts = m_WxService.GetUsersByQuery(query, scopeId);
                    if (userAccounts.Count > 0)
                    {
                        m_log.InfoFormat("[WxUserHandler] Got user info query for {0} avatar(s)", userAccounts.Count);

                        OSDMap response = new OSDMap();
                        foreach (UserAccount user in userAccounts)
                        {
                            OSDMap userMap = new OSDMap();
                            foreach (KeyValuePair<string, object> data in user.ToKeyValuePairs())
                                userMap.Add(data.Key.ToString(), data.Value.ToString());

                            userMap.Add(user.PrincipalID.ToString(), userMap);
                        }

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
                return FailureResult("Exception");
            }
        }

        #endregion Handler Methods

        #region Example Handler Methods

        private byte[] GetUserInfo(Dictionary<string, object> request)
        {
            UUID id = UUID.Zero;
            UserAccount d = null;

            if ( request.ContainsKey("user_id"))
            {
                if (UUID.TryParse(request["user_id"].ToString(), out id))
                {
                    d = m_WxService.GetUserData(id);
                }
                else
                {
                    return FailureResult(String.Format("Error getting userID {0}", id));
                }
            }

            if ( d != null )
            {
                Dictionary<string, object> userData = d.ToKeyValuePairs();
                OSDMap doc = new OSDMap(userData.Count);

                foreach (KeyValuePair<string, object> item in userData) {

                    doc[item.Key] = OSD.FromString(item.Value.ToString());

                }
                return DocToBytes(doc);
            }
            return FailureResult("Error getting user info");
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

        #endregion Example Handler Methods

        #region Wx Database
        private byte[] PutWxUser(Dictionary<string, object> request)
        {
            IWxUserData data = m_WxService.WxDb;

            UserData user = new UserData();

            user.FirstName = request["first_name"].ToString();
            user.LastName = request["last_name"].ToString();
            user.Food = request["fav_food"].ToString();

            data.StoreName(user);
            OSDMap doc = new OSDMap();

            doc["result"] = "success";
            return DocToBytes(doc);
        }

        private byte[] ListWxUser()
        {
            IWxUserData data = m_WxService.WxDb;

            List<UserData> list = data.ListNames();
            OSDMap doc = new OSDMap();

            foreach ( UserData u in list )
            {
                OSDMap udata = new OSDMap();
                string uname = String.Format("{0} {1}", u.FirstName, u.LastName);
                string ufood = u.Food.ToString();

                udata["name"] = OSD.FromString(uname);
                udata["food"] = OSD.FromString(ufood);
                doc.Add(uname, udata);
            }

            return DocToBytes(doc);
        }
        #endregion Wx Database

        #region User Functions
        public UserAccount GetUserData(UUID userID) {

            UserAccount userInfo = null;

            userInfo = m_WxService.GetUserData(userID);

            return userInfo;
        }
        #endregion User Functions
    }
}

