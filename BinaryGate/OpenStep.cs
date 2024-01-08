using System;
using System.Collections.Generic;
using System.Text;
using SimioAPI;
using SimioAPI.Extensions;
using System.Drawing;

namespace BinaryGate
{
    class OpenStepDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyOpenGate"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.  
        /// </summary>
        public string Description
        {
            get { return "The OpenGate step may be used to set the status of a user defined BinaryGate element to 'Open'."; }
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
        static readonly Guid MY_ID = new Guid("{0C07E364-364F-4795-9DC0-EBCBEDFEC5FE}"); //Jan2024/danH

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
            pd.Description = "The gate to open";
            pd.Required = true;
        }

        /// <summary>
        /// Method called to create a new instance of this step type to place in a process. 
        /// Returns an instance of the class implementing the IStep interface.
        /// </summary>
        public IStep CreateStep(IPropertyReaders properties)
        {
            return new OpenStep(properties);
        }

        #endregion
    }

    class OpenStep : IStep
    {
        IPropertyReaders _properties;
        IElementProperty _gateProp;
        public OpenStep(IPropertyReaders properties)
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
            context.ExecutionInformation.TraceInformation(String.Format("Opening gate {0}", (_gateProp as IPropertyReader).GetStringValue(context)));
            gate.OpenGate();
            return ExitType.FirstExit;
        }

        #endregion
    }
}
