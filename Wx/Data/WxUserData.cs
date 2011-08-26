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
using System.Collections.Generic;
using OpenSim.Data;
using OpenSim.Data.MySQL;
using MySql.Data.MySqlClient;


namespace Wx.Data
{
    public class WxData_User
    {
        public string UserID;
        public Dictionary<string, string> Data;

        public WxData_User()
        {
            Data = new Dictionary<string, string>();
        }
    }

    // Our interface for our database handler...
    public interface IWxUserData
    {
        bool StoreName(UserData data);
        List<UserData> ListNames();

    }
    // Make a class here just so we can pass it around instead of several strings...
    public class UserData
    {
        public string FirstName
        {
            get
            {
                return m_FirstName;
            }

            set
            {
                m_FirstName = value;
            }
        }
        protected string m_FirstName;

        public string LastName
        {
            get
            {
                return m_LastName;
            }

            set
            {
                m_LastName = value;
            }
        }
        protected string m_LastName;

        public string Food
        {
            get
            {
                return m_Food;
            }

            set
            {
                m_Food = value;
            }
        }
        protected string m_Food;
    }

    // We will build our database handler over MySQLGenericTableHandler...
    public class WxUserData: MySQLGenericTableHandler<WxData_User>, IWxUserData
    {
        private string m_Database = "WxUser";

        public WxUserData(string connectionString, string realm) : base(connectionString, realm, "WxDataStore") {}

        #region data manipulators
        // Store our data
        public bool StoreName(UserData u)
        {
            MySqlCommand cmd = new MySqlCommand();

            cmd.CommandText = String.Format("REPLACE INTO {0} (FirstName, LastName, Food) VALUES (?FirstName,?LastName,?Food)",
                m_Database);

            cmd.Parameters.AddWithValue("?FirstName", u.FirstName);
            cmd.Parameters.AddWithValue("?LastName", u.LastName);
            cmd.Parameters.AddWithValue("?Food", u.Food);
            ExecuteNonQuery(cmd);

            return true;
        }
        // Get a list of items in the database...
        public List<UserData> ListNames()
        {
            try
            {
                List<UserData> data = new List<UserData>();

                lock (m_dbLock)
                {
                    using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                    {
                        dbcon.Open();

                        using (MySqlCommand result = new MySqlCommand(String.Format("SELECT `FirstName`, `LastName`, `Food` from {0}", m_Database), dbcon))
                        {
                            using (MySqlDataReader reader = result.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    UserData item = new UserData();

                                    item.FirstName = reader["FirstName"].ToString();
                                    item.LastName = reader["LastName"].ToString();
                                    item.Food = reader["Food"].ToString();

                                    if (item != null)
                                        data.Add(item);
                                }

                                return data;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
            return null;
        }
        #endregion
    }
}
