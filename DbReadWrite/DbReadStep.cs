using System;
using System.Globalization;
using SimioAPI;
using SimioAPI.Extensions;

namespace DBReadWrite
{
    public class DbReadStepDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyDbRead"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.  
        /// </summary>
        public string Description
        {
            get { return "The DbRead step may be used read data from a database."; }
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
        static readonly Guid MY_ID = new Guid("{E9A072D3-91F1-4F20-A385-4FA8C999C633}"); //Jan2024/danH

        /// <summary>
        /// Property returning the number of exits out of the step. Can return either 1 or 2. 
        /// </summary>
        public int NumberOfExits
        {
            get { return 1; }
        }

        /// <summary>
        /// Method called that defines the property schema for the step.
        /// </summary>
        public void DefineSchema(IPropertyDefinitions schema)
        {
            IPropertyDefinition pd;

            // Reference to the file to read from
            pd = schema.AddElementProperty("DbConnect", DBConnectElementDefinition.MY_ID);

            // Table name
            pd = schema.AddStringProperty("TableName", String.Empty);
            pd.DisplayName = "Table Name";
            pd.Description = "The database table name where the data will be read.";
            pd.Required = true;

            // A repeat group of columns and states where the data will be read
            IRepeatGroupPropertyDefinition columns = schema.AddRepeatGroupProperty("Columns");
            columns.Description = "The column names and values for reading the data.";
            pd = columns.PropertyDefinitions.AddStringProperty("Column", String.Empty);
            pd.Description = "The column name where data will be read.";
            pd.Required = true;
            pd = columns.PropertyDefinitions.AddStateProperty("State");
            pd.Description = "The state where the data will be read into.";
            pd.Required = true;

            // Where Column name
            IRepeatGroupPropertyDefinition wheres = schema.AddRepeatGroupProperty("Where");
            wheres.Description = "The column names and values to select data from.";
            pd = wheres.PropertyDefinitions.AddStringProperty("WhereColumn", String.Empty);
            pd.DisplayName = "Where Column";
            pd.Description = "The column name to select the data from.";
            pd.Required = false;
            // Where State name
            pd = wheres.PropertyDefinitions.AddStateProperty("WhereState");
            pd.DisplayName = "Where State";
            pd.Description = "The state whose value to select data from.";
            pd.Required = false;
        }

        /// <summary>
        /// Method called to create a new instance of this step type to place in a process. 
        /// Returns an instance of the class implementing the IStep interface.
        /// </summary>
        public IStep CreateStep(IPropertyReaders properties)
        {
            return new DbReadStep(properties);
        }

        #endregion
    }

    class DbReadStep : IStep
    {
        IPropertyReaders _props;
        IPropertyReader _tablenameProp;
        IElementProperty _dbconnectElementProp;
        IRepeatingPropertyReader _columns;
        IRepeatingPropertyReader _wheres;
        public DbReadStep(IPropertyReaders properties)
        {
            _props = properties;
            _dbconnectElementProp = (IElementProperty)_props.GetProperty("DbConnect");
            _tablenameProp = _props.GetProperty("TableName");
            _columns = (IRepeatingPropertyReader)_props.GetProperty("Columns");
            _wheres = (IRepeatingPropertyReader)_props.GetProperty("Where");
        }

        #region IStep Members

        /// <summary>
        /// Method called when a process token executes the step.
        /// </summary>
        public ExitType Execute(IStepExecutionContext context)
        {          

            DBConnectElement dbconnect = (DBConnectElement)_dbconnectElementProp.GetElement(context);
            String tableName = _tablenameProp.GetStringValue(context);

            int numInRepeatGroups = _columns.GetCount(context);
            string[,] stringArray = new string[numInRepeatGroups, 2];
            for (int i = 0; i < numInRepeatGroups; i++)
            {
                // The thing returned from GetRow is IDisposable, so we use the using() pattern here
                using (IPropertyReaders columnsRow = _columns.GetRow(i, context))
                {
                    // Get the string property
                    IPropertyReader column = columnsRow.GetProperty("Column");
                    stringArray[i, 0] = column.GetStringValue(context);
                }
            }

            int numWhereInRepeatGroups = _wheres.GetCount(context);
            string[,] whereArray = new string[numWhereInRepeatGroups, 2];
            for (int i = 0; i < numWhereInRepeatGroups; i++)
            {
                // The thing returned from GetRow is IDisposable, so we use the using() pattern here
                using (IPropertyReaders wheresRow = _wheres.GetRow(i, context))
                {
                    // Get the string property
                    IPropertyReader wherecolumn = wheresRow.GetProperty("WhereColumn");
                    whereArray[i, 0] = wherecolumn.GetStringValue(context);
                    IPropertyReader wherestate = wheresRow.GetProperty("WhereState");
                    whereArray[i, 1] = getWhereString(wherestate, context);
                }
            }
            
            string[,] parts = dbconnect.ReadTable(tableName, stringArray, whereArray);       

            int numReadIn = 0;
            for (int i = 0; i < parts.Length && i < _columns.GetCount(context); i++)
            {
                // The thing returned from GetRow is IDisposable, so we use the using() pattern here
                using (IPropertyReaders row = _columns.GetRow(i, context))
                {
                    // Get the state property out of the i-th tuple of the repeat group
                    IStateProperty stateprop = (IStateProperty)row.GetProperty("State");
                    // Resolve the property value to get the runtime state
                    IState  state = stateprop.GetState(context);
                    string part = parts[i, 1];

                    if (TryAsNumericState(state, part) ||
                        TryAsDateTimeState(state, part) ||
                        TryAsStringState(state, part))
                    {
                        numReadIn++;
                    }
                }
            }

            context.ExecutionInformation.TraceInformation($"DbRead has read data from table={tableName}");

            // We are done reading, have the token proceed out of the primary exit
            return ExitType.FirstExit;
        }

        string getWhereString(IPropertyReader wherestate, IStepExecutionContext context)
        {
            String whereState = "";
            IStateProperty stateProp = (IStateProperty)wherestate;
            IState state = stateProp.GetState(context);
            String stateName = "";
            if (state != null)
            {
                try
                {
                    stateName = state.Name;
                }
                catch { /* uh... wha? I guess we need this... not sure why though -ccrooks */ }

                if (stateName.Length > 0)
                {
                    IRealState realState = state as IRealState;
                    if (realState != null)
                    {
                        double d = 0.0;
                        if (Double.TryParse(realState.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                        {
                            whereState = d.ToString();
                        }
                    }
                    if (whereState.Length == 0)
                    {
                        IDateTimeState dateTimeState = state as IDateTimeState;
                        if (dateTimeState != null)
                        {
                            DateTime dt;
                            if (DateTime.TryParse(dateTimeState.Value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                            {
                                whereState = "'" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                            }

                            // If it isn't a DateTime, maybe it is just a number, which we can interpret as hours from start of simulation.
                            double d = 0.0;
                            if (Double.TryParse(dateTimeState.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                            {
                                whereState = "'" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                            }
                        }
                    }
                    if (whereState.Length == 0)
                    {
                        IStringState stringState = state as IStringState;
                        if (stringState != null)
                        {
                            whereState = "'" + stringState.Value + "'";
                        }
                    }
                }                
            }
            return whereState;
        }

        bool TryAsNumericState(IState state, string rawValue)
        {
            IRealState realState = state as IRealState;
            if (realState == null)
                return false; // destination state is not a real.

            double d = 0.0;
            if (Double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
            {
                realState.Value = d;
                return true;
            }
            else if (String.Compare(rawValue, "True", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                realState.Value = 1.0;
                return true;
            }
            else if (String.Compare(rawValue, "False", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                realState.Value = 0.0;
                return true;
            }

            return false; // incoming value can't be interpreted as a real.
        }

        bool TryAsDateTimeState(IState state, string rawValue)
        {
            IDateTimeState dateTimeState = state as IDateTimeState;
            if (dateTimeState == null)
                return false; // destination state is not a DateTime.

            DateTime dt;
            if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                dateTimeState.Value = dt;
                return true;
            }

            // If it isn't a DateTime, maybe it is just a number, which we can interpret as hours from start of simulation.
            double d = 0.0;
            if (Double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
            {
                state.StateValue = d;
                return true;
            }

            return false;
        }

        bool TryAsStringState(IState state, string rawValue)
        {
            IStringState stringState = state as IStringState;
            if (stringState == null)
                return false; // destination state is not a string.

            // Since all input value are already strings, this is easy.
            stringState.Value = rawValue;
            return true;
        }

        #endregion
    }
}
