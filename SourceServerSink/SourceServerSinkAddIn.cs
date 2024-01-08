using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SimioAPI;
using SimioAPI.Extensions;

namespace SourceServerSink
{
    public class SourceServerSinkAddIn : IDesignAddIn //, IDesignAddInGuiDetails
    {
        #region IDesignAddIn Members

        /// <summary>
        /// Property returning the name of the add-in. This name may contain any characters and is used as the display name for the add-in in the UI.
        /// </summary>
        public string Name
        {
            get { return "Source, Server, Sink"; }
        }

        /// <summary>
        /// Property returning a short description of what the add-in does.  
        /// </summary>
        public string Description
        {
            get { return "Creates a Source, Server, and Sink set of objects.\nPlaces Source, Server, and Sink objects from the Simio Standard Library into the current model, and connects those objects using Path objects from the Standard Library"; }
        }

        /// <summary>
        /// Property returning an icon to display for the add-in in the UI.
        /// </summary>
        public System.Drawing.Image Icon
        {
            get { return Properties.Resources.Icon; }
        }

        /// <summary>
        /// Method called when the add-in is run.
        /// </summary>
        public void Execute(SimioAPI.Extensions.IDesignContext context)
        {
            if (context.ActiveModel != null)
            {
                var intelligentObjects = context.ActiveModel.Facility.IntelligentObjects;

                // Create the Source, Server, and Sink. Space them out along a diagonal line. The X and Z coordinate of the location specify the left to right and top to bottom 
                //  coordinates from a top down view. The Y coordinate specifies the elevation. We cast them to IFixedObject here so that we can get to their Nodes collection
                //  later
                var source = intelligentObjects.CreateObject("Source", new FacilityLocation(-5, 0, -5)) as IFixedObject;
                var server = intelligentObjects.CreateObject("Server", new FacilityLocation(0, 0, 0)) as IFixedObject;
                var sink = intelligentObjects.CreateObject("Sink", new FacilityLocation(5, 0, 5)) as IFixedObject;

                if (source == null || server == null || sink == null)
                {
                    MessageBox.Show("Could not create Standard Library objects. You need to load the Standard Library in the Facility view.");
                    return;
                }

                // Nodes is an IEnumerable, so we will create a temporary List from it to quickly get to the first node in the set
                var sourceoutput = new List<INodeObject>(source.Nodes)[0];

                var servernodes = new List<INodeObject>(server.Nodes);
                var serverinput = servernodes[0];
                var serveroutput = servernodes[1];

                var sinkinput = new List<INodeObject>(sink.Nodes)[0];

                // This path goes directly from the output of source to the input of server
                var path1 = intelligentObjects.CreateLink("Path", sourceoutput, serverinput, null);
                // This path goes from the output of server to the input of sink, with one vertex in between
                var path2 = intelligentObjects.CreateLink("Path", serveroutput, sinkinput, new List<FacilityLocation> { new FacilityLocation(3, 0, 0) });
            }
            else
            {
                MessageBox.Show("You must have an active model to run this add-in.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region IDesignAddInGuiDetails Members

        // Here is a sample implementation of the optional IDesignAddInGuiDetails interface.
        // To use this implementation, un-comment the interface name on the "class" line at
        // the top of this file.
        //
        // If a design-time add-in implements this optional interface, it can specify where
        // in Simio's ribbon area it should appear.  Merely implementing the interface, and
        // returning null for CategoryName, TabName, and GroupName will cause the add-in to
        // appear at a default location defined by Simio.  However, the add-in can return a
        // specific name for any or all of these properties, to indicate where it should be
        // located in Simio's ribbon area.

        /// <summary>
        /// Property returning the category name for this Add-In.  Return null to use Simio's default add-in category name.
        /// </summary>
        public string CategoryName
        {
            get { return null; }
        }

        /// <summary>
        /// Property returning the group name for this Add-In.  Return null to use Simio's default add-in group name.
        /// </summary>
        public string GroupName
        {
            get { return null; }
        }

        /// <summary>
        /// Property returning the tab name for this Add-In.  Return null to use Simio's default add-in tab name.
        /// </summary>
        public string TabName
        {
            get { return null; }
        }

        #endregion
    }
}
