using System;
using System.Collections.Generic;
using System.Text;
using SimioAPI;
using SimioAPI.Extensions;

namespace CustomSimioStep
{
    class FileElementDefinition : IElementDefinition
    {
        #region IElementDefinition Members

        /// <summary>
        /// Property returning the full name for this type of element. The name should contain no spaces. 
        /// </summary>
        public string Name
        {
            get { return "MyTextFile"; }
        }

        /// <summary>
        /// Property returning a short description of what the element does.  
        /// </summary>
        public string Description
        {
            get { return "Used with ReadText and WriteText steps.\nThe File element may be used in conjunction with the user defined Read and Write steps to read and write to an external file."; }
        }

        /// <summary>
        /// Property returning an icon to display for the element in the UI. 
        /// </summary>
        public System.Drawing.Image Icon
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
        // We need to use this ID in the element reference property of the Read/Write steps, so we make it public
        public static readonly Guid MY_ID = new Guid("{29F7FF04-3EC6-4EDC-A614-1DED3DF1A9F8}"); //Jan2024/danH

        /// <summary>
        /// Method called that defines the property, state, and event schema for the element.
        /// </summary>
        public void DefineSchema(IElementSchema schema)
        {
            IPropertyDefinition pd = schema.PropertyDefinitions.AddStringProperty("FilePath", String.Empty);
            pd.Description = "The name of the text file that is being read from or written to.";

            IPropertyDefinition mergeOutputFiles = schema.PropertyDefinitions.AddBooleanProperty("AutoMergeWriteStepFilesForExperiment");
            mergeOutputFiles.DisplayName = "Auto Merge Write Step Files For Experiment";
            mergeOutputFiles.Description = "Specifies whether to automatically merge all the experiment output files per File Element into one new file. If enabled, the new file will be located in the same folder.";
        }

        /// <summary>
        /// Method called to add a new instance of this element type to a model. 
        /// Returns an instance of the class implementing the IElement interface.
        /// </summary>
        public IElement CreateElement(IElementData data)
        {
            return new FileElement(data);
        }

        #endregion
    }

    class FileElement : IElement, IDisposable
    {
        IElementData _data;
        string _writerFileName;
        string _readerFileName;
        bool bMergeFileElementExperimentOutputFiles;
        public FileElement(IElementData data)
        {
            _data = data;
            IPropertyReader fileNameProp = _data.Properties.GetProperty("FilePath");
            bMergeFileElementExperimentOutputFiles =  _data.Properties.GetProperty("AutoMergeWriteStepFilesForExperiment").GetDoubleValue(_data.ExecutionContext) == 1.0;

            // Cache the names of the files to open for reading or writing
            string fileName = fileNameProp.GetStringValue(_data.ExecutionContext);
            if (String.IsNullOrEmpty(fileName) == false)
            {
                string fileRoot = null;
                string fileDirectoryName = null;
                string fileExtension = null;

                try
                {
                    fileRoot = System.IO.Path.GetPathRoot(fileName);
                    fileDirectoryName = System.IO.Path.GetDirectoryName(fileName);
                    fileExtension = System.IO.Path.GetExtension(fileName);
                }
                catch (ArgumentException e)
                {
                    data.ExecutionContext.ExecutionInformation.ReportError(String.Format("Failed to create runtime file element. Message: {0}", e.Message));
                }

                string simioProjectFolder = _data.ExecutionContext.ExecutionInformation.ProjectFolder;
                string simioExperimentName = _data.ExecutionContext.ExecutionInformation.ExperimentName;
                string simioScenarioName = _data.ExecutionContext.ExecutionInformation.ScenarioName;
                string simioReplicationNumber = _data.ExecutionContext.ExecutionInformation.ReplicationNumber.ToString();

                if (String.IsNullOrEmpty(fileDirectoryName) || String.IsNullOrEmpty(fileRoot))
                {
                    fileDirectoryName = simioProjectFolder;
                    fileName = fileDirectoryName + "\\" + fileName;
                }

                _readerFileName = fileName;

                if (String.IsNullOrEmpty(simioExperimentName))
                {
                    _writerFileName = fileName;
                }
                else
                {
                    var dirName = System.IO.Path.GetDirectoryName(fileName);
                    var sanitizedFileName = FileUtils.SanitizeFileName(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(fileName), null) + "_" + 
                                                                       simioExperimentName + "_" + 
                                                                       simioScenarioName + 
                                                                       "_Rep" + simioReplicationNumber + 
                                                                       fileExtension);

                    _writerFileName = System.IO.Path.Combine(dirName, sanitizedFileName);
                }
            }
        }

        System.IO.TextWriter _writer;
        public System.IO.TextWriter Writer
        {
            get
            {
                // We can't read and write at the same time
                if (_reader != null)
                {
                    _data.ExecutionContext.ExecutionInformation.ReportError(String.Format("Trying to write to {0}, which is already open for reading.", _writerFileName ?? "[No file specified]"));
                    return null;
                }

                // If we don't already have a writer, create one
                try
                {
                    if (String.IsNullOrEmpty(_writerFileName))
                        ReportFileOpenError("[No file specified]", "writing", "[None]");
                    else if (_writer == null)
                        _writer = new System.IO.StreamWriter(_writerFileName);
                }
                catch (Exception e)
                {
                    _writer = null;
                    ReportFileOpenError(_writerFileName, "writing", e.Message);
                }

                if (bMergeFileElementExperimentOutputFiles)
                    _data.ExecutionContext.ExecutionInformation.NotifyTextFileWritten(_readerFileName, _writerFileName);

                return _writer;
            }
        }

        System.IO.TextReader _reader;
        public System.IO.TextReader Reader
        {
            get
            {
                // We can't read and write at the same time
                if (_writer != null)
                {
                    _data.ExecutionContext.ExecutionInformation.ReportError(String.Format("Trying to read from {0}, which is already open for writing.", _readerFileName ?? "[No file specified]"));
                    return null;
                }

                // If we don't already have a reader, create one
                try
                {
                    if (String.IsNullOrEmpty(_readerFileName))
                        ReportFileOpenError("[No file specified]", "reading", "[None]");
                    if (_reader == null)
                        _reader = new System.IO.StreamReader(_readerFileName);
                }
                catch (Exception e)
                {
                    _reader = null;
                    ReportFileOpenError(_readerFileName, "reading", e.Message);
                }

                return _reader;
            }
        }

        void ReportFileOpenError(string fileName, string action, string exceptionMessage)
        {
            _data.ExecutionContext.ExecutionInformation.ReportError($"Error opening {fileName} for {action}. This may mean the specified file, path or disk does not exist.\n\nInternal exception message: {exceptionMessage}");
        }

        #region IElement Members

        /// <summary>
        /// Method called when the simulation run is initialized.
        /// </summary>
        public void Initialize()
        { 
            // No initialization logic needed, we will open the file on the first read or write request
        }

        /// <summary>
        /// Method called when the simulation run is terminating.
        /// </summary>
        public void Shutdown()
        {
            // On shutdown, we need to make sure to close the file

            if (_writer != null)
            {
                try
                {
                    _writer.Close();
                    _writer.Dispose();
                }
                catch(Exception e)
                {
                    _data.ExecutionContext.ExecutionInformation.ReportError($"There was a problem closing file '{_writerFileName ?? String.Empty}' for writing. Message: {e.Message}");
                }
                finally
                {
                    _writer = null;
                }
            }

            if (_reader != null)
            {
                try
                {
                    _reader.Close();
                    _reader.Dispose();
                }
                catch(Exception e)
                {
                    _data.ExecutionContext.ExecutionInformation.ReportError($"There was a problem closing file '{_readerFileName ?? String.Empty}' for reading. Message: {e.Message}");
                }
                finally
                {
                    _reader = null;
                }
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Shutdown();
        }

        #endregion
    }
}
