using System;
using System.Globalization;
using SimioAPI;
using SimioAPI.Extensions;

namespace CustomSimioStep
{
    class ReadStepDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyReadText"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.  
        /// </summary>
        public string Description
        {
            get { return "The Read step may be used to read values from an input file into state variables. The user defined File element is used to specify the file."; }
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
        static readonly Guid MY_ID = new Guid("{B85EBBEB-554E-4148-854F-D9420CD0C759}");

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
            pd = schema.AddElementProperty("File", FileElementDefinition.MY_ID);

            pd = schema.AddStringProperty("Separator", String.Empty);
            pd.Description = "The character that seperates the numeric values in the file";
            pd.Required = false;

            // A repeat group of states to read into
            IRepeatGroupPropertyDefinition parts = schema.AddRepeatGroupProperty("States");
            parts.Description = "The state values to read the values into";

            pd = parts.PropertyDefinitions.AddStateProperty("State");
            pd.Description = "A state to read a value into from a file.";
        }

        /// <summary>
        /// Method called to create a new instance of this step type to place in a process. 
        /// Returns an instance of the class implementing the IStep interface.
        /// </summary>
        public IStep CreateStep(IPropertyReaders properties)
        {
            return new ReadStep(properties);
        }

        #endregion
    }

    class ReadStep : IStep
    {
        IPropertyReaders _props;
        IPropertyReader _seperatorProp;
        IElementProperty _fileProp;
        IRepeatingPropertyReader _states;
        public ReadStep(IPropertyReaders properties)
        {
            _props = properties;
            _seperatorProp = _props.GetProperty("Separator");
            _fileProp = (IElementProperty)_props.GetProperty("File");
            _states = (IRepeatingPropertyReader)_props.GetProperty("States");
        }

        #region IStep Members

        /// <summary>
        /// Method called when a process token executes the step.
        /// </summary>
        public ExitType Execute(IStepExecutionContext context)
        {
            // Get the file
            FileElement file = (FileElement)_fileProp.GetElement(context);
            if (file == null)
            {
                context.ExecutionInformation.ReportError("File element is null.  Makes sure FilePath is defined correctly.");
            }
            else
            {
                // Try to read the next line
                string line = null;

                if (file.Reader != null)
                    line = file.Reader.ReadLine();

                // If we haven't reached the end of the file yet
                if (line != null)
                {
                    // Tokenize the input
                    string[] parts = line.Split(new string[] { _seperatorProp.GetStringValue(context) }, StringSplitOptions.None);

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
                            string part = parts[i];

                            if (TryAsNumericState(state, part) ||
                                TryAsDateTimeState(state, part) ||
                                TryAsStringState(state, part))
                            {
                                numReadIn++;
                            }
                        }
                    }

                    context.ExecutionInformation.TraceInformation(String.Format("Read in the line \"{0}\" from file {1} into {2} states", line, (_fileProp as IPropertyReader).GetStringValue(context), numReadIn));
                }
            }

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
