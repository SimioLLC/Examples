using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SimioAPI;
using SimioAPI.Extensions;
using System.Data;
using System.Data.Common;
using System.Globalization;

namespace DBReadWrite
{
    class DBConnectElementDefinition : IElementDefinition
    {
        #region IElementDefinition Members

        /// <summary>
        /// Property returning the full name for this type of element. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyDbConnect"; }
        }

        /// <summary>
        /// Property returning a short description of what the element does.  
        /// </summary>
        public string Description
        {
            get { return "Used with DbRead, DbWrite, DbQuery and DbExecute steps.\nThe DbConnect element may be used in conjunction with the user-defined DbRead, DbWrite, DbQuery and DbExecute steps to create, read, update and delete data in a database."; }
        }

        /// <summary>
        /// Property returning an icon to display for the element in the UI. 
        /// </summary>
        public System.Drawing.Image Icon
        {
            get { return null; }
        }

        /// <summary>
        /// Property returning a unique static GUID for the element.  
        /// </summary>
        public Guid UniqueID
        {
            get { return MY_ID; }
        }
        // We need to use this ID in the element reference property of the Read/Write steps, so we make it public
        public static readonly Guid MY_ID = new Guid("{3C8A23DB-E56C-4CEE-BE2A-FFFA08269F92}"); //Jan2024/danH

        /// <summary>
        /// Method called that defines the property, state, and event schema for the element.
        /// </summary>
        public void DefineSchema(IElementSchema schema)
        {
            IPropertyDefinition pd = schema.PropertyDefinitions.AddStringProperty("ConnectionString", String.Empty);
            pd.DisplayName = "Connection String";
            pd.Description = "The connection string used to define the connection to the database.";           
            pd.Required = true;

            IPropertyDefinition pd2 = schema.PropertyDefinitions.AddStringProperty("ProviderName", String.Empty);
            pd2.DisplayName = "Provider Name";
            pd2.Description = "The provider type used to specify what database provider to use to connect to the database.";            
            pd2.Required = true;
        }

        /// <summary>
        /// Method called to add a new instance of this element type to a model. 
        /// Returns an instance of the class implementing the IElement interface.
        /// </summary>
        public IElement CreateElement(IElementData data)
        {
            return new DBConnectElement(data);
        }

        #endregion
    }

    class DBConnectElement : IElement
    {
        IElementData _data;
        private DbProviderFactory _db;
        private DbConnection _connection;
        public DBConnectElement(IElementData data)
        {
            _data = data;
            // Create some propertyReaders
            IPropertyReader connectStringProp = _data.Properties.GetProperty("ConnectionString");
            IPropertyReader providerNameProp = _data.Properties.GetProperty("ProviderName");

            // Get and cache the connect string and connection
            string connectionString = connectStringProp.GetStringValue(_data.ExecutionContext);
            string providerName = providerNameProp.GetStringValue(_data.ExecutionContext);

            // Display each row and column value.
            List<String> providerNames = new List<String>();
#if NETSTANDARD2_0
            var factoryTypeName = providerNameProp.GetStringValue(_data.ExecutionContext);
            if (factoryTypeName == "System.Data.SqlClient")
                _db = System.Data.SqlClient.SqlClientFactory.Instance;
            providerNames.Add("System.Data.SqlClient");
#else
            DataTable table = DbProviderFactories.GetFactoryClasses();
            foreach (DataRow row in table.Rows)
            {
                providerNames.Add(row[0].ToString());
                if (row[0].ToString().ToLower() == providerNameProp.GetStringValue(_data.ExecutionContext).ToLower())
                {
                    _db = DbProviderFactories.GetFactory(row);
                    break;
                }
            }
#endif

            if (_db == null)
            {
                string providerList = System.Environment.NewLine + string.Join(System.Environment.NewLine, providerNames.ToArray());
                string msg = $"Provider not found. Available providers are={providerList}";
                data.ExecutionContext.ExecutionInformation.ReportError(msg);
            }


            if (_connection == null && _db != null)
            {
                try
                {
                    _connection = _db.CreateConnection();
                }
                catch(Exception ex)
                {
                    string msg = $"Exception trying to create the connection object. Message: '{ex.Message}'";
                    data.ExecutionContext.ExecutionInformation.ReportError(msg);
                    _connection = null;
                    return;
                }

                var connectionStringPropValue = connectStringProp.GetStringValue(_data.ExecutionContext);
                try
                {
                    _connection.ConnectionString = connectionStringPropValue;
                }
                catch(Exception ex)
                {
                    string msg = $"Exception trying to set connection string='{connectionStringPropValue}'. Message: '{ex.Message}'";
                    data.ExecutionContext.ExecutionInformation.ReportError(msg);
                    _connection = null;
                    return;
                }

                try
                {
                    _connection.Open();
                }
                catch(Exception ex)
                {
                    string msg = $"Exception trying to open database connection. Message: '{ex.Message}'";
                    data.ExecutionContext.ExecutionInformation.ReportError(msg);
                    _connection = null;
                    return;
                }
            }
        }

        /// <summary>
        /// A Sql Select command that selects one or more rows, which are
        /// then placed in a SQL DataTable, which is then placed into
        /// a 2D string array.
        /// </summary>
        /// <param name="sqlString"></param>
        /// <returns></returns>
        public string[,] QueryResults(string sqlString)
        {            
            DbDataAdapter dataAdapter = _db.CreateDataAdapter();
            var command = _connection.CreateCommand();
            command.CommandText = sqlString;
            dataAdapter.SelectCommand = command;
            DataTable dataTable = new DataTable
            {
                Locale = System.Globalization.CultureInfo.InvariantCulture
            };
            dataAdapter.Fill(dataTable);

            string[,] stringArray = new string[dataTable.Rows.Count, dataTable.Columns.Count];

            for (int row = 0; row < dataTable.Rows.Count; ++row)
            {
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    stringArray[row, col] = dataTable.Rows[row][col].ToString();
                }
            }

            return stringArray;
        }

        public int ExecuteResults(string sqlString)
        {
            var command = _connection.CreateCommand();
            command.CommandText = sqlString;
            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// Write the data in the stringArray as a single row into the database.
        /// Since there is often some form of data conversion, we'll
        /// wrap the method with a try-catch block and report any exceptions.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="stringArray"></param>
        internal void WriteTable(string tableName, string[,] stringArray)
        {
            string marker = $"Create a SQL Command.";
            try
            {
                // setup data adapter
                DbDataAdapter dataAdapter = _db.CreateDataAdapter();
                var command = _connection.CreateCommand();
                command.CommandText = "Select * from " + tableName;
                dataAdapter.SelectCommand = command;

                // define command builder
                marker = $"Insert Command={command}";
                DbCommandBuilder commandBuilder = _db.CreateCommandBuilder();
                commandBuilder.DataAdapter = dataAdapter;
                dataAdapter.InsertCommand = commandBuilder.GetInsertCommand();

                // define data table
                marker = $"Fill DataTable. Command={command}";
                DataTable dataTable = new DataTable();
                dataTable.Locale = System.Globalization.CultureInfo.InvariantCulture;
                DataTable[] dataTables = { dataTable };
                dataAdapter.Fill(1, 1, dataTables);

                // define data row
                DataRow dataRow = dataTable.NewRow();
                // for each parameter
                for (int i = 0; i < (stringArray.Length / 2); i++)
                {
                    string columnName = stringArray[i, 0];
                    string columnValue = stringArray[i, 1];
                    marker = $"Assigning Column={columnName}";
                    dataRow[columnName] = columnValue;
                }

                marker = $"Adding row and Updating";
                dataTable.Rows.Add(dataRow);
                dataAdapter.Update(dataTable);
            }
            catch (Exception ex)
            {
                throw new Exception($"Table={tableName}. Marker={marker}. Err={ex.Message}");
            }
        }

        internal string[,] ReadTable(string tableName, string[,] stringArray, string[,] whereArray)
        {
            // get column names
            string columnNamesConcat = "";
            int numCRows = stringArray.Length / 2;
            for (int i = 0; i < numCRows; i++)
            {
                if (i == 0)
                {
                    columnNamesConcat = stringArray[i, 0];
                }
                else
                {
                    columnNamesConcat = $"{columnNamesConcat}, {stringArray[i, 0]}";
                }
            }

            // get wheres
            string wheresNamesConcat = "";
            int numWRows = whereArray.Length / 2;
            for (int i = 0; i < numWRows; i++)
            {
                if (i == 0)
                {
                    wheresNamesConcat = $"{whereArray[i, 0]} = {whereArray[i, 1]}"; 
                }
                else
                {
                    wheresNamesConcat = $"{wheresNamesConcat} AND {whereArray[i, 0]} = {whereArray[i, 1]}";
                }
            }

            // setup data adapter
            DbDataAdapter dataAdapter = _db.CreateDataAdapter();
            var command = _connection.CreateCommand();

            if (wheresNamesConcat.Length > 0)
            {
                command.CommandText = $"SELECT {columnNamesConcat} FROM {tableName} WHERE {wheresNamesConcat}";
            }
            else
            {
                command.CommandText = $"SELECT {columnNamesConcat} FROM {tableName}";
            }
            dataAdapter.SelectCommand = command;

            DataTable dataTable = new DataTable
            {
                Locale = System.Globalization.CultureInfo.InvariantCulture
            };
            dataAdapter.Fill(dataTable);

            // Put the database column value into the 'value' portaion of the array
            if (dataTable.Rows.Count > 0)
            {
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    stringArray[col, 1] = dataTable.Rows[0][col].ToString();
                }
            }

            return stringArray;
        }
        
#region IElement Members

        /// <summary>
        /// Method called when the simulation run is initialized.
        /// </summary>
        public void Initialize()
        { 
            // No initialization logic needed, we will open the file on the first read or write request
        }

        /// <summary>
        /// Method called when the simulation run is terminating.
        /// </summary>
        public void Shutdown()
        {
            // On shutdown, we need to make sure to close the DB Connection
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

#endregion
    }
}
