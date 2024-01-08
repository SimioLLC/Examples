using System;
using System.Collections.Generic;
using System.Text;
using SimioAPI;
using SimioAPI.Extensions;
using System.Drawing;


namespace BinaryGate
{
    class GateElementDefinition : IElementDefinition
    {
        #region IElementDefinition Members

        /// <summary>
        /// Property returning the full name for this type of element. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyBinaryGate"; }
        }

        /// <summary>
        /// Property returning a short description of what the element does.  
        /// </summary>
        public string Description
        {
            get { return "Used with MyOpenGate, MyCloseGate, and MyPassThruGate steps.\nThe BinaryGate element may be used in conjunction with the user defined OpenGate, CloseGate, and PassThruGate steps to define an 'Open/Closed' token flow constraint in a process."; }
        }

        /// <summary>
        /// Property returning an icon to display for the element in the UI. 
        /// </summary>
        public Image Icon
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
        // We need to use this ID in the element reference property of the Open/Close/PassThru steps, so we make it public
        public static readonly Guid MY_ID = new Guid("{5AB91AED-E961-41A4-AC95-97D091341AA6}"); //Jan2024/danH

        /// <summary>
        /// Method called that defines the property, state, and event schema for the element.
        /// </summary>
        public void DefineSchema(IElementSchema schema)
        {
            // This element has no properties
        }

        /// <summary>
        /// Method called to add a new instance of this element type to a model. 
        /// Returns an instance of the class implementing the IElement interface.
        /// </summary>
        public IElement CreateElement(IElementData data)
        {
            return new GateElement(data);
        }

        #endregion
    }

    class GateElement : IElement
    {
        IElementData _data;
        public GateElement(IElementData data)
        {
            _data = data;
        }

        // Simply boolean to track open/closed state
        bool _bIsOpen;
        public bool IsOpen
        {
            get { return _bIsOpen; }
        }

        public void OpenGate()
        {
            // When we open the gate, we set the boolean, and fire the opened event, to let anyone
            //  waiting know that the gate is now opened
            _bIsOpen = true;
            OnOpened();
        }
        public void CloseGate()
        {
            _bIsOpen = false;
        }

        public event EventHandler Opened;
        void OnOpened()
        {
            // The opened event is listened to by the PassThru step
            if (Opened != null)
                Opened(this, EventArgs.Empty);

            // Once we fire, we remove the listeners. More subscriptions to
            //  the event will from from the PassThru step
            Opened = null;
        }

        #region IElement Members

        /// <summary>
        /// Method called when the simulation run is initialized.
        /// </summary>
        public void Initialize()
        {
            // No initialization code necessary
        }

        /// <summary>
        /// Method called when the simulation run is terminating.
        /// </summary>
        public void Shutdown()
        {
            // No shutdown code necessary
        }

        #endregion
    }

}
