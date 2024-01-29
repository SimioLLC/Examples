using System;
using SimioAPI;
using SimioAPI.Extensions;

namespace DBReadWrite
{
    /// <summary>
    /// A Step to execute a SQL Statement
    /// </summary>
    class DbExecuteStepDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyDbExecute"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.  
        /// </summary>
        public string Description
        {
            get { return "The DbExecute step may be used to execute an SQL statement on database."; }
        }

        /// <summary>
        /// Property returning an icon to display for the step in the UI. 
        /// </summary>
        public System.Drawing.Image Icon
        {
            get { return null; }
        }

        /// <summary>
        /// Property returning a unique static GUID for the step.  
        /// </summary>
        public Guid UniqueID
        {
            get { return MY_ID; }
        }
        static readonly Guid MY_ID = new Guid("{A3EFDAD8-08E4-4B86-A3DD-E44BCB0D1D38}");    //Jan2024/danH

        /// <summary>
        /// Property returning the number of exits out of the step. Can return either 1 or 2. 
        /// </summary>
        public int NumberOfExits
        {
            get { return 2; }
        }

        /// <summary>
        /// Method called that defines the property schema for the step.
        /// </summary>
        public void DefineSchema(IPropertyDefinitions schema)
        {
            IPropertyDefinition pd;

            // Reference to the file to write to
            pd = schema.AddElementProperty("DbConnect", DBConnectElementDefinition.MY_ID);
            pd.DisplayName = "DB Connect Element";
            pd.Description = "A Simio Element that defines how to connect to the database";
            pd.Required = true;

            // And a format specifier
            pd = schema.AddStringProperty("SQLStatement", String.Empty);
            pd.DisplayName = "SQL Statement";
            pd.Description = "SQL Statement using parameters. E.g. DELETE FROM myCustomers WHERE LastName=@paramLastName AND DateOfBirth=@paramDob";
            pd.Required = false;

            // A repeat group of values to write out
            IRepeatGroupPropertyDefinition parts = schema.AddRepeatGroupProperty("Items");
            parts.Description = "The expression items to be written out.";

            pd = parts.PropertyDefinitions.AddExpressionProperty("Expression", String.Empty);
            pd.Description = "Expression value to be written out.";
        }

        /// <summary>
        /// Method called to create a new instance of this step type to place in a process. 
        /// Returns an instance of the class implementing the IStep interface.
        /// </summary>
        public IStep CreateStep(IPropertyReaders properties)
        {
            return new DbExecuteStep(properties);
        }

        #endregion
    }

    class DbExecuteStep : IStep
    {
        IPropertyReaders _readers;
        IPropertyReader _prSqlstatements;
        IElementProperty _dbconnectElementProp;
        IRepeatingPropertyReader _rgprItems;
        public DbExecuteStep(IPropertyReaders properties)
        {
            _readers = properties;
            _prSqlstatements = _readers.GetProperty("SQLStatement");
            _dbconnectElementProp = (IElementProperty)_readers.GetProperty("DbConnect");
            _rgprItems = (IRepeatingPropertyReader)_readers.GetProperty("Items");
        }

        #region IStep Members

        /// <summary>
        /// Method called when a process token executes the step.
        /// </summary>
        public ExitType Execute(IStepExecutionContext context)
        {
            string marker = "Getting expressions for Parameters.";
            try
            {
                // Get an array of double values from the repeat group's list of expressions
                object[] paramsArray = new object[_rgprItems.GetCount(context)];
                int itemCount = _rgprItems.GetCount(context);
                for (int i = 0; i < itemCount; i++)
                {
                    marker = $"Getting Item# {i} of {itemCount} Items from the Repeating Group.";
                    // The thing returned from GetRow is IDisposable, so we use the using() pattern here
                    using (IPropertyReaders row = _rgprItems.GetRow(i, context))
                    {
                        // Get the expression property reader
                        IExpressionPropertyReader prExpression = row.GetProperty("Expression") as IExpressionPropertyReader;
                        // Use the reader to get the expression value
                        paramsArray[i] = prExpression.GetExpressionValue(context);
                        marker += $" Value={paramsArray[i]}";
                    }
                }

                // set DB data
                DBConnectElement dbconnect = (DBConnectElement)_dbconnectElementProp.GetElement(context);
                String sqlString = _prSqlstatements.GetStringValue(context);

                int numberOfRowsAffected = 0;
                try
                {
                    // for each parameter..Backwards to ensure higher parameters are not overwritten by lower parameters
                    for (int i = paramsArray.Length - 1; i >= 0; i--)
                    {
                        int paramIndex = i + 1;
                        String replaceString = "@" + paramIndex.ToString();
                        sqlString = sqlString.Replace(replaceString, paramsArray[i].ToString());
                        marker = $"Index={i}. SQL={sqlString}";
                    }
                    marker = $"Executing SQL={sqlString}";
                    numberOfRowsAffected = dbconnect.ExecuteResults(sqlString);
                }
                catch (FormatException)
                {
                    context.ExecutionInformation.ReportError("Bad format provided in Db Execute step.");
                    return ExitType.AlternateExit;
                }

                context.ExecutionInformation.TraceInformation($"DbExecute ran using the SQL=[{sqlString}. Rows affected={numberOfRowsAffected}");

                // We are done writing, have the token proceed out of the primary exit
                return ExitType.FirstExit;

            }
            catch (Exception ex)
            {
                context.ExecutionInformation.ReportError($"Marker={marker}. Error Executing SQL. Err={ex.Message}");
                return ExitType.AlternateExit;
            }
        }

        #endregion
    }
}
