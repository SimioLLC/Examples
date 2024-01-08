using System;
using System.Collections.Generic;
#if NETFRAMEWORK
using System.Drawing;
#elif NETSTANDARD1_2_OR_GREATER
using System.Drawing.Common;
#endif

using System.Text;
using SimioAPI;
using SimioAPI.Extensions;


namespace BinaryGate
{
    class CloseStepDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyCloseGate"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.  
        /// </summary>
        public string Description
        {
            get { return "The CloseGate step may be used to set the status of a user defined BinaryGate element to 'Closed'."; }
        }

        /// <summary>
        /// Property returning an icon to display for the step in the UI. 
        /// </summary>
        public Image Icon
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
        static readonly Guid MY_ID = new Guid("{9D858324-BB85-4C1D-A133-B6F6A2001C86}"); //Jan2024/dth

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
            // We will need a reference to the gate to open
            IPropertyDefinition pd = schema.AddElementProperty("Gate", GateElementDefinition.MY_ID);
            pd.Description = "The gate to close";
            pd.Required = true;
        }

        /// <summary>
        /// Method called to create a new instance of this step type to place in a process. 
        /// Returns an instance of the class implementing the IStep interface.
        /// </summary>
        public IStep CreateStep(IPropertyReaders properties)
        {
            return new CloseStep(properties);
        }

        #endregion
    }

    class CloseStep : IStep
    {
        IPropertyReaders _properties;
        IElementProperty _gateProp;
        public CloseStep(IPropertyReaders properties)
        {
            _properties = properties;
            _gateProp = (IElementProperty)_properties.GetProperty("Gate");
        }

        #region IStep Members

        /// <summary>
        /// Method called when a process token executes the step.
        /// </summary>
        public ExitType Execute(IStepExecutionContext context)
        {
            GateElement gate = (GateElement)_gateProp.GetElement(context);
            gate.CloseGate();
            context.ExecutionInformation.TraceInformation($"Closed gate {(_gateProp as IPropertyReader).GetStringValue(context)}");
            return ExitType.FirstExit;
        }

        #endregion
    }
}
