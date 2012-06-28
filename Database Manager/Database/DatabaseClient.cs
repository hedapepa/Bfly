﻿using System;
using System.Data;
using MySql.Data.MySqlClient;
using Database_Manager.Database.Session_Details.Interfaces;
using Database_Manager.Database.Session_Details;
using Helpers;


namespace Database_Manager.Database
{
    public class DatabaseClient : IDisposable
    {

        #region static declares
        /// <summary>
        /// The maximum idle time for a connection
        /// </summary>
        private static readonly int MAX_LIVE_CONNECTION_TIME = 1000 * 60 * 2; //2 hours max life time

        private static readonly Random randomLive = new Random();
        #endregion

        #region declares

        /// <summary>
        /// The last activity of this item
        /// </summary>
        private DateTime creatonTime;

        /// <summary>
        /// The Id of this connection
        /// </summary>
        private int connectionID;
        
        /// <summary>
        /// The database manager which this instance is a child of
        /// </summary>
        private DatabaseManager dbManager;

        /// <summary>
        /// The current conenctions state of this item
        /// </summary>
        private ConnectionState state;
        
        /// <summary>
        /// The database connection to the database
        /// </summary>
        private MySqlConnection connection;

        /// <summary>
        /// Contains the iformation about a out-given session
        /// </summary>
        private IQueryAdapter info;

        #endregion

        #region constructor
        /// <summary>
        /// Creates a new database client with the given database manager
        /// </summary>
        /// <param name="dbManager">The manager which contains this item</param>
        public DatabaseClient(DatabaseManager dbManager, int id)
        {
            this.dbManager = dbManager;
            this.connectionID = id;
            this.creatonTime = DateTime.Now.AddMinutes(randomLive.Next(1,60));
            state = ConnectionState.Closed;
            connection = new MySqlConnection(
                dbManager.getConnectionString());
            connection.StateChange += connecionStateChanged;
        }
        #endregion

        #region connection related methods
        /// <summary>
        /// Sets the new connection state generated by the connection event
        /// </summary>
        /// <param name="sender">The sender of the item</param>
        /// <param name="e">The StateChangeEvent which has the information</param>
        private void connecionStateChanged(object sender, StateChangeEventArgs e)
        {
            //Out.writeNotification(string.Format("Database connection [{0}] changed from [{1}] to [{2}] ", this.connectionID, this.state, e.CurrentState));
            this.state = e.CurrentState;
        }

        /// <summary>
        /// Connects this instance to a database
        /// </summary>
        public void connect()
        {
            connection.Open();
        }

        /// <summary>
        /// Disconnects this unit from the database
        /// </summary>
        public void disconnect()
        {
            try
            {
                //Out.writeNotification("Closing Database connection [" + connectionID + "]");
                connection.Close();
            }
            catch { }
        }

        /// <summary>
        /// Returns the current connection state of the database client
        /// </summary>
        /// <returns></returns>
        public ConnectionState getConnectionState()
        {
            int timeDifference = (int)(DateTime.Now - this.creatonTime).TotalMilliseconds;
            if (timeDifference >= MAX_LIVE_CONNECTION_TIME)
            {
                return ConnectionState.Broken;
            }
            else
            {
                return this.state;
            }
        }
        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the current item making it ready for another use
        /// </summary>
        public void Dispose()
        {
            this.info = null;
        }

        /// <summary>
        /// prepares a connection before giving it out to an object
        /// </summary>
        /// <param name="autoCommit">indication if the item should be auto commited or not</param>
        public void prepare(bool autoCommit)
        {
            //Out.writeNotification("Handing out Database client [" + this.getID() + "]");
            if (autoCommit)
            {
                this.info = new TransactionQueryReactor(this);
            }
            else
            {
                this.info = new NormalQueryReactor(this);
            }
        }

        #endregion

        #region Session related methods
        /// <summary>
        /// Generates a new Mysql commando for use with the information
        /// </summary>
        /// <returns></returns>
        internal MySqlCommand getNewCommand()
        {
            return connection.CreateCommand();
        }


        /// <summary>
        /// Generates a new transaction
        /// </summary>
        /// <returns>A new mysqltransaction</returns>
        internal MySqlTransaction getTransaction()
        {
            return connection.BeginTransaction();
        }

        /// <summary>
        /// Reports current queries as done
        /// </summary>
        internal void reportDone()
        {
            //Out.writeNotification("Returning Database client [" + this.getID() + "]");
            this.dbManager.reportDone(this);
            this.Dispose();
        }

        /// <summary>
        /// Returns a query reactor
        /// </summary>
        /// <returns></returns>
        internal IQueryAdapter getQueryReactor()
        {
            return info;
        }
        #endregion 
    
        internal int getID()
        {
            return this.connectionID;
        }

        internal DateTime getLastAction()
        {
            return this.creatonTime;
        }

        internal bool isAvailable()
        {
            return (this.info == null);
        }
    }
}