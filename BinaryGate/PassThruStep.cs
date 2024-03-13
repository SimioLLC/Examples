using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using SimioAPI;
using SimioAPI.Extensions;

namespace BinaryGate
{
    class PassThruStepDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyPassThruGate"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.  
        /// </summary>
        public string Description
        {
            get { return "The PassThruGate step may be used to hold a token at the step until the status of a user defined BinaryGate element is 'Open'."; }
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
        static readonly Guid MY_ID = new Guid("{BC719654-BFFA-45D3-B8A4-AEF7308A1FD0}");    //Jan2024/danH

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
            // We will need a reference to the gate to pass through
            IPropertyDefinition pd = schema.AddElementProperty("Gate", GateElementDefinition.MY_ID);
            pd.Description = "The gate to pass through. If the gate is currently closed the token will wait here until the gate opens.";
            pd.Required = true;
        }

        /// <summary>
        /// Method called to create a new instance of this step type to place in a process. 
        /// Returns an instance of the class implementing the IStep interface.
        /// </summary>
        public IStep CreateStep(IPropertyReaders properties)
        {
            return new PassThruStep(properties);
        }

        #endregion
    }

    /// <summary>
    /// The Step where Entities are stopped or continue based on the Gate.
    /// </summary>
    class PassThruStep : IStep
    {
        IPropertyReaders _properties;
        IElementProperty _prGate; // Get element PropertyReader

        public PassThruStep(IPropertyReaders properties)
        {
            _properties = properties;
            _prGate = (IElementProperty)_properties.GetProperty("Gate");
        }

        /// <summary>
        /// A class to hold the information for a token while waiting for a gate to open, 
        /// Its method OnGateOpened is called when the gate is opened.
        /// </summary>
        class WaitForGateOpen
        {
            IStepExecutionContext _context;
            public WaitForGateOpen(IStepExecutionContext context)
            {
                _context = context;
            }

            public void OnGateOpened(object sender, EventArgs e)
            {
                // Note that if the GateElement hadn't explicitly removed the listeners from
                //  it's Opened event when it fired, we would want to make sure to remove 
                //  ourself here, so that we don't get events when we don't want them.

                // Move to the next step
                _context.ProceedOut(ExitType.FirstExit);
            }
        }

        #region IStep Members

        /// <summary>
        /// Method called when a process token executes the step.
        /// </summary>
        public ExitType Execute(IStepExecutionContext context)
        {
            // Get the gate
            GateElement gate = (GateElement)_prGate.GetElement(context);

            if (gate.IsOpen)
            {
                context.ExecutionInformation.TraceInformation($"Passing through gate {(_prGate as IPropertyReader).GetStringValue(context)}");

                // If the gate is open we can just proceed to the next step
                return ExitType.FirstExit;
            }
            else
            {
                // The gate is closed, so we will need to wait for it to open. To do this,
                //  we will "save off" our context into a temporary object, and wire the 
                //  gate's opened event directly to it. When the opened event fires, the 
                //  WaitForGateOpen logic will make the ProceedOut call to go to the next step
                //  and the gate will set its Opened event to null, which will drop the
                //  reference to the temporary WaitForGateOpen object, causing it to be collected.
                gate.Opened += new WaitForGateOpen(context).OnGateOpened;

                context.ExecutionInformation.TraceInformation($"Waiting for gate {(_prGate as IPropertyReader).GetStringValue(context)} to Open");

                // Since we are waiting to something to happen, we don't want the token to proceed
                //  to the next step, so return Wait here.
                return ExitType.Wait;
            }
        }

        #endregion
    }
}
