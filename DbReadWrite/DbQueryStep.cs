using System;
using System.Globalization;
using SimioAPI;
using SimioAPI.Extensions;

namespace DBReadWrite
{
    class DbQueryStepDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyDbQuery"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.  
        /// </summary>
        public string Description
        {
            get { return "The Db Query step may be used to query data from a database."; }
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
        static readonly Guid MY_ID = new Guid("{86724777-E8F8-49E7-847F-447DE0FC18BD}"); //Jan2024/danH

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
            pd.DisplayName = "DB Connect Element";
            pd.Description = "A Simio Element that defines how to connect to the database";
            pd.Required = true;

            pd = schema.AddStringProperty("SQLStatement", String.Empty);
            pd.DisplayName = "SQL Statement";
            pd.Description = "SQL Statement. Use @ sign with an index to specify a parameter in the States repeating property";
            pd.Required = false;

            // A repeat group of states to read into
            IRepeatGroupPropertyDefinition parts = schema.AddRepeatGroupProperty("States");
            parts.Description = "The state values to read the values into";

            pd = parts.PropertyDefinitions.AddStateProperty("State");
            pd.Description = "A state to read a value into from DB.";
        }

        /// <summary>
        /// Method called to create a new instance of this step type to place in a process. 
        /// Returns an instance of the class implementing the IStep interface.
        /// </summary>
        public IStep CreateStep(IPropertyReaders properties)
        {
            return new DbQueryStep(properties);
        }

        #endregion
    }

    class DbQueryStep : IStep
    {
        IPropertyReaders _props;
        IPropertyReader _sqlstatementProp;
        IElementProperty _dbconnectElementProp;
        IRepeatingPropertyReader _states;
        public DbQueryStep(IPropertyReaders properties)
        {
            _props = properties;
            _sqlstatementProp = _props.GetProperty("SQLStatement");
            _dbconnectElementProp = (IElementProperty)_props.GetProperty("DbConnect");
            _states = (IRepeatingPropertyReader)_props.GetProperty("States");
        }

        #region IStep Members

        /// <summary>
        /// Method called when a process token executes the step.
        /// </summary>
        public ExitType Execute(IStepExecutionContext context)
        {
            // Get DB data
            DBConnectElement dbconnect = (DBConnectElement)_dbconnectElementProp.GetElement(context);
            String sqlString = _sqlstatementProp.GetStringValue(context);

            // Get an array of double values from the repeat group's list of states
            object[] paramsArray = new object[_states.GetCount(context)];
            // update sqlString based on state value...Backwards to ensure higher parameters are not overwritten by lower parameters
            for (int i = _states.GetCount(context) - 1; i >= 0; i--)
            {
                // The thing returned from GetRow is IDisposable, so we use the using() pattern here
                using (IPropertyReaders row = _states.GetRow(i, context))
                {
                    int paramIndex = i + 1;
                    String replaceString = "@" + paramIndex.ToString();

                    IStateProperty stateprop = (IStateProperty)row.GetProperty("State");
                    IState state = stateprop.GetState(context);

                    String replaceValue = "";
                    if (state is IStringState stringState)
                    {
                        replaceValue = stringState.Value;
                    }
                    else if (state is IDateTimeState dateTimeState)
                    {
                        replaceValue = dateTimeState.Value.ToString();
                    }
                    else
                    {
                        replaceValue = state.StateValue.ToString();
                    }
                    if (replaceValue.Length > 0)
                    {
                        sqlString = sqlString.Replace(replaceString, replaceValue);
                    }
                }
            }

            // Tokenize the input
            string[,] parts = dbconnect.QueryResults(sqlString);

            int numReadIn = 0;
            for (int i = 0; i < parts.Length && i < _states.GetCount(context); i++)
            {
                // The thing returned from GetRow is IDisposable, so we use the using() pattern here
                using (IPropertyReaders row = _states.GetRow(i, context))
                {
                    // Get the state property out of the i-th tuple of the repeat group
                    IStateProperty stateprop = (IStateProperty)row.GetProperty("State");
                    // Resolve the property value to get the runtime state
                    IState state = stateprop.GetState(context);
                    string part = parts[0,i];

                    if (TryAsNumericState(state, part) ||
                        TryAsDateTimeState(state, part) ||
                        TryAsStringState(state, part))
                    {
                        numReadIn++;
                    }
                }
            }

            context.ExecutionInformation.TraceInformation( $"DbQuery ran using the SQL statement {sqlString} into {numReadIn} states");

            // We are done reading, have the token proceed out of the primary exit
            return ExitType.FirstExit;
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
