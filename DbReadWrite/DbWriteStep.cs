using System;
using System.Globalization;
using SimioAPI;
using SimioAPI.Extensions;

namespace DBReadWrite
{
    public class DbWriteStepDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyDbWrite"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.  
        /// </summary>
        public string Description
        {
            get { return "The DbWrite step may be used to write data to a database."; }
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
        static readonly Guid MY_ID = new Guid("{2B13172C-A49B-4261-BCAE-46C1902B54A3}"); //Jan2024/danH

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

            // Reference to the file to write to
            pd = schema.AddElementProperty("DbConnect", DBConnectElementDefinition.MY_ID);
            pd.DisplayName = "DB Connect Element";
            pd.Description = "A Simio Element that defines how to connect to the database";
            pd.Required = true;

            // Table name
            pd = schema.AddStringProperty("TableName", String.Empty);
            pd.DisplayName = "Table Name";
            pd.Description = "The database table name where the data is to be written.";
            pd.Required = true;

            // A repeat group of columns and expression where the data will be written
            IRepeatGroupPropertyDefinition columns = schema.AddRepeatGroupProperty("Columns");
            columns.Description = "The column names and values for writting the data.";
            pd = columns.PropertyDefinitions.AddStringProperty("Column", String.Empty);
            pd.Description = "The column name where data will be written.";            
            pd = columns.PropertyDefinitions.AddExpressionProperty("Expression", String.Empty);
            pd.Description = "The expression where the data will be written from.";
        }

        /// <summary>
        /// Method called to create a new instance of this step type to place in a process. 
        /// Returns an instance of the class implementing the IStep interface.
        /// </summary>
        public IStep CreateStep(IPropertyReaders properties)
        {
            return new DbWriteStep(properties);
        }

        #endregion
    }

    class DbWriteStep : IStep
    {
        IPropertyReaders _props;
        IPropertyReader _tablenameProp;
        IElementProperty _dbconnectElementProp;
        IRepeatingPropertyReader _columns;
        public DbWriteStep(IPropertyReaders properties)
        {
            _props = properties;
            _dbconnectElementProp = (IElementProperty)_props.GetProperty("DbConnect");
            _tablenameProp = _props.GetProperty("TableName");            
            _columns = (IRepeatingPropertyReader)_props.GetProperty("Columns");
        }

        #region IStep Members

        /// <summary>
        /// Method called when a process token executes the step.
        /// Write the expressions in the repeating group to the database.
        /// Each member of the repeating group is a column in the database table.
        /// </summary>
        public ExitType Execute(IStepExecutionContext context)
        {

            int numInRepeatGroups = _columns.GetCount(context);

            object[,] paramsArray = new object[numInRepeatGroups, 2];

            // Create a 2D array from the Step's repeating group which is called 'Columns'
            // with each row have two fields: "Column" and "Expression"
            // Our array will place "Column" at index 0, and the evaluated "Expression" at index 1.
            for (int i = 0; i < numInRepeatGroups; i++)
            {
                // The thing returned from GetRow is IDisposable, so we use the using() pattern here
                using (IPropertyReaders columnsRow = _columns.GetRow(i, context))
                {
                    // Get the database column name
                    IPropertyReader column = columnsRow.GetProperty("Column");
                    paramsArray[i, 0] = column.GetStringValue(context);
                    IExpressionPropertyReader expressionProp = columnsRow.GetProperty("Expression") as IExpressionPropertyReader;
                    // Resolve the expression to get the value
                    paramsArray[i, 1] = expressionProp.GetExpressionValue(context);
                }
            }

            DBConnectElement dbconnect = (DBConnectElement)_dbconnectElementProp.GetElement(context);
            String tableName = _tablenameProp.GetStringValue(context);

            try
            {
                // for each parameter
                string[,] stringArray = new string[numInRepeatGroups, 2];
                for (int i = 0; i < numInRepeatGroups; i++)
                {
                    stringArray[i, 0] = (Convert.ToString(paramsArray[i, 0], CultureInfo.CurrentCulture));
                    double doubleValue = paramsArray[i, 1] is double ? (double)paramsArray[i, 1] : Double.NaN;
                    if (!System.Double.IsNaN(doubleValue))
                    {
                         stringArray[i, 1] = (Convert.ToString(doubleValue, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        DateTime datetimeValue = TryAsDateTime((Convert.ToString(paramsArray[i, 1], CultureInfo.InvariantCulture)));
                        if (datetimeValue > System.DateTime.MinValue)
                        {
                             stringArray[i, 1] = (Convert.ToString(datetimeValue, CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            stringArray[i, 1] = (Convert.ToString(paramsArray[i, 1], CultureInfo.InvariantCulture));
                        }
                    }
                }

                dbconnect.WriteTable(tableName, stringArray);
            }
            catch (FormatException)
            {
                context.ExecutionInformation.ReportError("Bad format provided in DbWrite step.");
            }

            context.ExecutionInformation.TraceInformation(String.Format("DbWrite inserted data into table {0}", tableName));

            // We are done writing, have the token proceed out of the primary exit
            return ExitType.FirstExit;
        }

        DateTime TryAsDateTime(string rawValue)
        {
            if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt;
            }

            return DateTime.MinValue;
        }

        #endregion
    }
}
