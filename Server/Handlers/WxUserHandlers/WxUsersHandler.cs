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
                    case "testing":
                        return TestResponse(request);

                    case "get_user_info":
                        return GetUserInfo(request);

                    case "put_wxuser":
                        return PutWxUser(request);

                    case "list_wxuser":
                        return ListWxUser();

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
            OSDMap doc = new OSDMap(2);
            doc["Result"] = OSD.FromString("Failure");
            doc["Message"] = OSD.FromString(msg);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(OSDMap doc)
        {
            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(doc));
        }
        #endregion utility

        #region Handler Methods
        private byte[] GetUserInfo (Dictionary<string, object> request)
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

        private byte[] TestResponse (Dictionary<string, object> request)
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

