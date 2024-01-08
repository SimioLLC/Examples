using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimioAPI;
using SimioAPI.Extensions;

namespace WriteModelSummaryOnSave
{
    //
    // This is an example Modeling Helper AddIn, it demonstrates both adding new add-in properties to the model (via DefineSchema),
    //  and hooking to model events for additional behavior.
    //

    public class WriteModelSummaryOnSave : IModelHelperAddIn
    {
        public string Name => "Write Model Summary On Save";

        public string Description => "Writes a summary of the model to a specified file next to the project when the model is saved.";

        public Image Icon => Properties.Resources.Icon;

        // If this example is used as a the basis of another addin make, sure to change this id, so that Simio can disambiguate the 
        //  new addin from this existing example
        public Guid UniqueID => new Guid("FEC2E02B-33E3-4373-8924-7BC696A745D5");

        public ModelAddInEnvironment AddInEnvironment => ModelAddInEnvironment.InteractiveDesktop;

        public IDisposable CreateInstance(IModelHelperContext context)
        {
            return new WriteModelSummaryOnSaveInstance(context);
        }

        public void DefineSchema(IModelHelperAddInSchema schema)
        {
            var fileNameProp = schema.Properties.AddStringProperty("SummaryFileName");
            fileNameProp.DisplayName = "Summary File Name";
            fileNameProp.Description = "The optional name of a file where the summary of the model will be saved when the model is saved.";
            fileNameProp.DefaultValue = String.Empty;
        }
    }

    class WriteModelSummaryOnSaveInstance : IDisposable
    {
        readonly IModelHelperContext _context;

        public WriteModelSummaryOnSaveInstance(IModelHelperContext context)
        {
            _context = context;

            // The context contains various model events you can subscribe to. You will usually
            //  want to unsubscribe from the event in the implementation of Dispose()
            _context.ModelSaved += Context_ModelSaved;
        }

        private void Context_ModelSaved(IModelSavedArgs args)
        {
            //
            // This is the handler for the "Model Saved" event, this is the bulk of the "helping" logic
            //

            var projectFileLocation = args.ProjectFileName;
            if (String.IsNullOrWhiteSpace(projectFileLocation))
                return; // Not a valid project file location, nothing to do

            var summaryFileProp = _context.PropertyValues.FirstOrDefault(p => p.Name == "SummaryFileName");
            if (summaryFileProp == null)
                return; // Could not find our custom property, this shouldn't happen, we are just being very careful

            var summaryFile = summaryFileProp.Value?.ToString();
            if (String.IsNullOrWhiteSpace(summaryFile))
                return; // User provided no file name to write to, nothing to do

            if (System.IO.Path.IsPathRooted(summaryFile) == false)
            {
                // The file name is not rooted, that means its relative to the location of the saved file
                var projectFileDirectory = System.IO.Path.GetDirectoryName(projectFileLocation);
                summaryFile = System.IO.Path.Combine(projectFileDirectory, summaryFile);
            }

            using (var writer = new System.IO.StreamWriter(summaryFile))
            {
                writer.WriteLine($"Summary for model '{_context.Model.Name}'");
                writer.WriteLine($"Number of objects: {_context.Model.Facility.IntelligentObjects.Count()}");
                writer.WriteLine($"Number of elements: {_context.Model.Elements.Count()}");
                writer.WriteLine($"Number of states: {_context.Model.StateDefinitions.Count()}");
                writer.WriteLine($"Number of properties: {_context.Model.PropertyDefinitions.Count()}");
            }
        }

        public void Dispose()
        {
            // Unsubscribing from the events here, as we no longer need to listen to them,
            //  Dispose() indicates the addin has been "unloaded"
            _context.ModelSaved -= Context_ModelSaved;
        }
    }
}
