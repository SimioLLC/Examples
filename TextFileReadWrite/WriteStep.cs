using System;
using SimioAPI;
using SimioAPI.Extensions;

namespace CustomSimioStep
{
    class WriteStepDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyWriteText"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.  
        /// </summary>
        public string Description
        {
            get { return "The Write step may be used to write values to an output file. The user defined File element is used to specify the file."; }
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
        static readonly Guid MY_ID = new Guid("{37BE1266-3E23-4974-BA91-AF5BF7D72E19}"); //Jan2024/danH

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
            pd = schema.AddElementProperty("File", FileElementDefinition.MY_ID);

            // And a format specifier
            pd = schema.AddStringProperty("Format", String.Empty);
            pd.Description = "The format of the string to write out in C# string format syntax. Expressions defined in the 'Items' repeat group may " +
                "be included as data parameters in the formatted string using zero-based, sequentially numbered format characters within curly braces (e.g., the format character '{3}' indicates output of the fourth item in the 'Items' repeat group). " +
                "If this property is not specified, then a comma-delimited format of '{0},{1}, ,{N}' is assumed. Refer to a C# reference for more information on string format syntax in C#.";
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
            return new WriteStep(properties);
        }

        #endregion
    }

    class WriteStep : IStep
    {
        IPropertyReaders _properties;
        IElementProperty _fileElement;
        IRepeatingPropertyReader _items;
        IPropertyReader _format;
        public WriteStep(IPropertyReaders properties)
        {
            _properties = properties;
            _fileElement = (IElementProperty)_properties.GetProperty("File");
            _items = (IRepeatingPropertyReader)_properties.GetProperty("Items");
            _format = _properties.GetProperty("Format");
        }

        #region IStep Members

        /// <summary>
        /// Method called when a process token executes the step.
        /// </summary>
        public ExitType Execute(IStepExecutionContext context)
        {
            // Get the file
            FileElement fileElement = (FileElement)_fileElement.GetElement(context);
            if (fileElement == null)
            {
                context.ExecutionInformation.ReportError("File element is null.  Makes sure FilePath is defined correctly.");
            }
            else
            {
                // Get an array of double values from the repeat group's list of expressions
                object[] paramsArray = new object[_items.GetCount(context)];
                for (int i = 0; i < _items.GetCount(context); i++)
                {
                    // The thing returned from GetRow is IDisposable, so we use the using() pattern here
                    using (IPropertyReaders row = _items.GetRow(i, context))
                    {
                        // Get the expression property
                        IExpressionPropertyReader expressionProp = row.GetProperty("Expression") as IExpressionPropertyReader;
                        // Resolve the expression to get the value
                        paramsArray[i] = expressionProp.GetExpressionValue(context);
                    }
                }

                string format = _format.GetStringValue(context);
                // If the user didn't provide a format we will just make our own in the form {0},{1},{2},.. {n}
                if (String.IsNullOrEmpty(format))
                {
                    format = "";
                    for (int i = 0; i < _items.GetCount(context); i++)
                    {
                        format += "{" + i + (i == _items.GetCount(context) - 1 ? "}" : "},");
                    }
                }

                string writeOut = null;
                try
                {
                    writeOut = String.Format(format, paramsArray);
                }
                catch (FormatException)
                {
                    writeOut = null;
                    context.ExecutionInformation.ReportError("Bad format provided in Write step.");
                }

                if (writeOut != null)
                {
                    // Write out the formatted line to the file
                    if (fileElement.Writer != null)
                    {
                        try
                        {
                            fileElement.Writer.WriteLine(writeOut);
                            context.ExecutionInformation.TraceInformation(String.Format("Writing out \"{0}\" to file {1}", writeOut, (_fileElement as IPropertyReader).GetStringValue(context)));
                        }
                        catch (Exception)
                        {
                            context.ExecutionInformation.ReportError("Error writing out information to file using Write step.");
                        }
                    }
                }
            }

            // We are done writing, have the token proceed out of the primary exit
            return ExitType.FirstExit;
        }

        #endregion
    }
}
