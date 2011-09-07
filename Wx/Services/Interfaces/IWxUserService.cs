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
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Wx.Data;
using System.Collections.Generic;


namespace Wx.Services.Interfaces
{
    public interface IWxUserService
    {
        IWxUserData WxDb
        {
            get;
        }

        UserAccount GetUserData(UUID userID);

        bool CreateUser(string firstName, string lastName, string email, UUID scopeId, int userFlags, int userLevel, string userTitle, Dictionary<string, object> servicesUrls);
        bool UpdateUser(UUID principalId, string firstName, string lastName, string email, UUID scopeId, int userFlags, int userLevel, string userTitle, Dictionary<string, object> servicesUrls);
        UserAccount GetUserByName(string firstName, string lastName, UUID scopeId);
        UserAccount GetUserByEmail(string email, UUID scopeId);
        UserAccount GetUserById(UUID principalId, UUID scopeId);
        List<UserAccount> GetUsersByQuery(string query, UUID scopeId);
    }
}

