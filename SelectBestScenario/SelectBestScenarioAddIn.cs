using System;
using System.Collections.Generic;
using SimioAPI;
using SimioAPI.Extensions;

namespace SelectBestScenario
{
    public class SelectBestScenarioAddInDefinition : IExperimentationAddInDefinition
    {
        #region IExperimentationAddInDefinition Members

        /// <summary>
        /// All add-ins must provide a name.  This is displayed in the user-interface for selecting an add-in.
        /// </summary>
        public string Name
        {
            get { return "My Select Best Scenario using KN"; }
        }

        /// <summary>
        /// The optional Description is shown in a tooltip when the user hovers the mouse over the add-in's name.
        /// </summary>
        public string Description
        {
            get { return "Use the Kim and Nelson ranking and selection procedure.\nKN is a sequential procedure by Kim and Nelson for selecting the best scenario from a set of candidate scenarios."; }
        }

        /// <summary>
        /// The optional Icon is displayed next to the add-in's name in the list of add-ins.
        /// </summary>
        public System.Drawing.Image Icon
        {
            get { return MySelectBestScenario.Properties.Resources.Icon; }
        }

        /// <summary>
        /// All experimentation add-ins must have their own UniqueID.  If you copy this example for your own purposes, you should
        /// rename it, and change its UniqueID.  You can generate a new Guid by using Microsoft's GuidGen.exe tool or by browsing
        /// to http://www.guidgen.com or - if in VisualStudio - select menu Tools > Create GUID.  
        /// </summary>
        public Guid UniqueID
        {
            get { return MY_ID; }
        }
        static readonly Guid MY_ID = new Guid("{85B2607B-D45D-4CD7-A368-985E88584D9D}"); //Jan2024/danH

        /// <summary>
        /// This method is called by Simio whenever it needs to connect this add-in to a Simio experiment.
        /// </summary>
        /// <param name="experiment">The experiment for which this add-in is being created.</param>
        /// <returns>An instance of a class that implements the IExperimentationAddIn interface.</returns>
        public IExperimentationAddIn CreateExperimentationAddIn(IExperiment experiment)
        {
            // The main purpose of this method is to create a new instance of the add-in's IExperimentationAddIn-derived class.
            return new SelectBestScenarioAddIn();
        }

        #endregion
    }

    public class SelectBestScenarioAddIn : IExperimentationAddIn
    {
        #region IExperimentationAddIn Members

        /// <summary>
        /// Initialize is called by Simio when the add-in is first connected to an experiment by the user,
        /// as well as at project load-time for any experiments that reference this add-in.
        /// </summary>
        /// <param name="context">An object with methods that are useful during add-in initialization.</param>
        public void Initialize(IExperimentationAddInInitializationContext context)
        {
            ProbabilityOfCorrectSelectionParameter = context.DefineParameter("Confidence Level", ExperimentParameterType.Real, null, "Confidence level for selecting the best scenario.", Default_ProbabilityOfCorrectSelection.ToString());
            IndifferenceZoneParameter = context.DefineParameter("Indifference Zone", ExperimentParameterType.Real, null, "The Indifference Zone defines the smallest meaningful difference that is used for defining the best.  All scenarios that fall within this smallest meaningful difference are close enough that any one of them may be selected as the best.", null);
            ReplicationLimitParameter = context.DefineParameter("Replication Limit", ExperimentParameterType.Integer, null, "The maximum number of replications that will be made before automatically terminating the procedure.", Default_ReplicationLimit.ToString()); // add: The procedure is guaranteed to terminate without this limit set.
        }

        internal IExperimentParameter ProbabilityOfCorrectSelectionParameter;
        internal IExperimentParameter IndifferenceZoneParameter;
        internal IExperimentParameter ReplicationLimitParameter;

        const double Default_ProbabilityOfCorrectSelection = 0.95;
        const int Default_ReplicationLimit = 100;
        // Note there is no default value for IndifferenceZone.

        internal const int Minimum_ReplicationLimit = 10; // Need at least this many for any meaningful results.

        /// <summary>
        /// Requested by Simio when the add-in is selected by the user.  If this returns a non-empty string, it will be displayed to the interactive user.
        /// </summary>
        public string LoadTimeMessage
        {
            get { return null; }
        }

        /// <summary>
        /// Called by Simio when the user is editing an experiment parameter.  You may validate and possibly reject the new value.
        /// </summary>
        public bool ExperimentParameterValueChanging(IExperiment experiment, IExperimentParameter parameter, string proposedValue, ref string failureReason)
        {
            // Validate the values that the user entered.
            if (parameter == ProbabilityOfCorrectSelectionParameter)
            {
                double result;
                if (Double.TryParse(proposedValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out result) == false || result <= 0.0 || result >= 1.0)
                {
                    failureReason = "Confidence Level must be a probability between 0.0 and 1.0.";
                    return false;
                }
            }

            if (parameter == IndifferenceZoneParameter)
            {
                if (!String.IsNullOrEmpty(proposedValue))
                {
                    double result;
                    if (Double.TryParse(proposedValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out result) == false || result <= 0.0)
                    {
                        failureReason = "Indifference Zone must be a real number > 0.0.";
                        return false;
                    }
                }
            }

            if (parameter == ReplicationLimitParameter)
            {
                int result;
                if (Int32.TryParse(proposedValue, out result) == false || result < Minimum_ReplicationLimit)
                {
                    failureReason = "Replication Limit must be at least " + Minimum_ReplicationLimit.ToString() + ".";
                    return false;
                }
            }

            return true;
        }

        #region Unused interface methods

        public void ExperimentParameterValueChanged(IExperiment experiment, IExperimentParameter parameter)
        {
        }

        public IEnumerable<string> GetExperimentParameterListValues(IExperiment experiment, IExperimentParameter parameter)
        {
            return null;
        }

        public void ControlAdded(IControlParametersDefinitionContext context)
        {
        }

        public void ControlRemoved(IExperimentControl control)
        {
        }

        public bool ControlValueChanging(IExperimentControl control, string proposedValue, ref string failureReason)
        {
            return true;
        }

        public void ControlValueChanged(IExperimentControl control)
        {
        }

        public bool ControlParameterValueChanging(IExperimentControl control, IExperimentParameter parameter, string proposedValue, ref string failureReason)
        {
            return true;
        }

        public void ControlParameterValueChanged(IExperimentControl control, IExperimentParameter parameter)
        {
        }

        public IEnumerable<string> GetControlParameterListValues(IExperimentControl control, IExperimentParameter parameter)
        {
            return null;
        }

        public void ResponseAdded(IResponseParametersDefinitionContext context)
        {
        }

        public void ResponseRemoved(IExperimentResponse response)
        {
        }

        public bool ResponseParameterValueChanging(IExperimentResponse response, IExperimentParameter parameter, string proposedValue, ref string failureReason)
        {
            return true;
        }

        public void ResponseParameterValueChanged(IExperimentResponse response, IExperimentParameter parameter)
        {
        }

        public IEnumerable<string> GetResponseParameterListValues(IExperimentResponse response, IExperimentParameter parameter)
        {
            return null;
        }

        /// <summary>
        /// Called by Simio when the add-in is about to be unloaded.
        /// </summary>
        public void Terminate()
        {
        }

        #endregion

        public IExperimentRunner ExperimentRunner
        {
            get { return new SelectBestScenarioExperimentRunner(this); }
        }

        #endregion
    }

    /// <summary>
    /// This add-in implements the optional (and advanced) IExperimentRunner interface
    /// in order to take greater control of the process of running the experiment.
    /// The main reason we do this is so we can do scenario screening.
    /// </summary>
    public class SelectBestScenarioExperimentRunner : IExperimentRunner
    {
        public SelectBestScenarioExperimentRunner(SelectBestScenarioAddIn addIn)
        {
            _addIn = addIn;
        }
        SelectBestScenarioAddIn _addIn;

        #region IExperimentRunner Members

        public void Run(IExperimentationContext context)
        {
            Validate(context); // Will throw an exception if something is not acceptable.

            // Make a list of all of the scenarios currently marked as Active.
            // These will be our starting point, and we'll remove scenarios from
            // this list until only one remains.
            _scenarios = new List<IScenario>();
            foreach (IScenario scenario in context.Experiment.Scenarios)
                if (scenario.Active)
                    _scenarios.Add(scenario);

            if (_scenarios.Count <= 1)
                return; // We have nothing to do.

            // First, we need to make sure all scenarios under consideration have
            // the same number of replications. Find the largest number of replications
            // requested or completed by any remaining scenario.
            int maxReps = SelectBestScenarioAddIn.Minimum_ReplicationLimit; // We'll start with at least this many replications just for some sort of statistical validity.
            foreach (IScenario scenario in _scenarios)
            {
                if (scenario.ReplicationsRequired > maxReps)
                    maxReps = scenario.ReplicationsRequired;
                if (scenario.ReplicationsCompleted > maxReps)
                    maxReps = scenario.ReplicationsCompleted;
            }

            // maxReps now tells us how many replications we need them all to have,
            // so bring the rest of the remaining scenarios up to that many.
            foreach (IScenario scenario in _scenarios)
                scenario.ReplicationsRequired = maxReps;

            // Run these new replications.
            if (RunScenarios(context) == false)
                return; // User canceled.

            // Now, set up a loop where we screen out scenarios, and run more replications as necessary.
            while (true)
            {
                DoScreening(context);

                // Are we down to only one scenario yet?
                if (_scenarios.Count <= 1)
                {
                    // We should do something here to announce to the user which scenario won.
                    return; // We're done.
                }

                int replicationLimit = int.Parse(_addIn.ReplicationLimitParameter.Value);
                if (_scenarios[0].ReplicationsCompleted >= replicationLimit)
                {
                    return; // Hit the limit.
                }

                // Create as many batches of replications as we can.
                int numberOfReplications = Math.Max(1, context.NumberOfSimultaneousReplications / _scenarios.Count);
                foreach (IScenario scenario in _scenarios)
                    scenario.ReplicationsRequired += numberOfReplications;

                if (RunScenarios(context) == false)
                    return; // User canceled.
            }
        }

        #endregion

        void Validate(IExperimentationContext context)
        {
            // Make sure things are okay to run.
            if (context.Experiment.Responses.Count < 1)
                throw new InvalidOperationException("The add-in requires there to be at least one response.");

            // Make sure one of them is marked as the primary one.
            foreach (IExperimentResponse response in context.Experiment.Responses)
                if (response.Primary)
                    _primaryResponse = response;
            if (_primaryResponse == null)
                throw new InvalidOperationException("The add-in requires that a primary response be set.");

            if (_primaryResponse.Objective == ResponseObjective.None)
                throw new InvalidOperationException("The add-in requires that the primary response has an Objective of Minimize or Maximize.");

            // Did they enter anything for the Indifference Zone?
            double iz;
            if (!Double.TryParse(_addIn.IndifferenceZoneParameter.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out iz))
                throw new InvalidOperationException("A value is required for Indifference Zone.");
        }

        void DoScreening(IExperimentationContext context)
        {
            // We need to know how many samples we have (how many replications have been run).  Since
            // all scenarios now have the same number of replications. we can just ask the first one.
            int numReplications = _scenarios[0].ReplicationsCompleted;

            // First, we need an array of raw values of the primary response.
            // The first dimension is the scenario, second is the replication.
            double[,] X = new double[_scenarios.Count, numReplications];
            // Initialize it by asking Simio for the individual replication values.
            for (int scenarioIndex = 0; scenarioIndex < _scenarios.Count; scenarioIndex++)
            {
                for (int replicationIndex = 1; replicationIndex <= numReplications; replicationIndex++)
                {
                    double sampleValue = double.NaN;
                    if (_scenarios[scenarioIndex].GetResponseValueForReplication(_primaryResponse, replicationIndex, ref sampleValue))
                        X[scenarioIndex, replicationIndex - 1] = sampleValue;
                    else // If we don't get a value from Simio, we can't continue.
                        throw new ApplicationException("IScenario.GetResponseValueForReplication failed for scenario " + _scenarios[scenarioIndex].Name + " replication " + replicationIndex.ToString());
                }
            }

            // Next, we need the per-scenario sample means for these raw values.
            // Note that these should be the same as the response value that Simio
            // returns from IScenario.GetResponseValue.
            double[] XBAR = new double[_scenarios.Count];
            for (int scenarioIndex = 0; scenarioIndex < _scenarios.Count; scenarioIndex++)
            {
                double sum = 0.0;
                for (int replicationIndex = 0; replicationIndex < numReplications; replicationIndex++)
                    sum += X[scenarioIndex, replicationIndex];
                XBAR[scenarioIndex] = sum / (double)numReplications;
            }

            // Now, we need an array containing the sample variance between each pair of scenarios.
            double[,] SS = new double[_scenarios.Count, _scenarios.Count];
            for (int scenarioIndex1 = 0; scenarioIndex1 < _scenarios.Count; scenarioIndex1++)
            {
                for (int scenarioIndex2 = 0; scenarioIndex2 < _scenarios.Count; scenarioIndex2++)
                {
                    if (scenarioIndex1 != scenarioIndex2)
                    {
                        double sum = 0.0;
                        for (int replicationIndex = 0; replicationIndex < numReplications; replicationIndex++)
                            sum += Math.Pow((X[scenarioIndex1, replicationIndex] - X[scenarioIndex2, replicationIndex] - XBAR[scenarioIndex1] + XBAR[scenarioIndex2]), 2.0);

                        SS[scenarioIndex1, scenarioIndex2] = sum / (double)(numReplications - 1);
                    }
                }
            }

            // Make sure all numbers in these equations are reals (no integers, which caused some truncations to occur)
            double PCS = double.Parse(_addIn.ProbabilityOfCorrectSelectionParameter.Value);
            double Q = 0.5 * (Math.Pow((2.0 * (1.0 - PCS) / (double)(context.Experiment.Scenarios.Count - 1)), (-2.0 / (double)(numReplications - 1))) - 1.0);
            double HH = 2.0 * Q * (double)(numReplications - 1);

            // Then, we need the "whisker" array.
            double IZ = double.Parse(_addIn.IndifferenceZoneParameter.Value);
            double[,] W = new double[_scenarios.Count, _scenarios.Count];
            for (int scenarioIndex1 = 0; scenarioIndex1 < _scenarios.Count; scenarioIndex1++)
            {
                for (int scenarioIndex2 = 0; scenarioIndex2 < _scenarios.Count; scenarioIndex2++)
                {
                    if (scenarioIndex1 != scenarioIndex2)
                    {
                        W[scenarioIndex1, scenarioIndex2] = Math.Max(0.0, (IZ / (double)(2 * numReplications)) * (HH * SS[scenarioIndex1, scenarioIndex2] / (IZ * IZ) - (double)numReplications));
                    }
                }
            }

            // Now (finally!) we can create a new (hopefully smaller) set of scenarios.
            List<IScenario> newList = new List<IScenario>();
            for (int scenarioIndex1 = 0; scenarioIndex1 < _scenarios.Count; scenarioIndex1++)
            {
                bool keep = true;
                for (int scenarioIndex2 = 0; scenarioIndex2 < _scenarios.Count; scenarioIndex2++)
                {
                    if (scenarioIndex1 != scenarioIndex2)
                    {
                        if (_primaryResponse.Objective == ResponseObjective.Maximize)
                        {
                            // Larger values are better.  If this one is substantially smaller than any of the others, throw it out.
                            if (XBAR[scenarioIndex1] < XBAR[scenarioIndex2] - W[scenarioIndex1, scenarioIndex2])
                                keep = false;
                        }
                        else if (_primaryResponse.Objective == ResponseObjective.Minimize)
                        {
                            // Smaller values are better.  If this one is substantially larger than any of the others, throw it out.
                            if (XBAR[scenarioIndex1] > XBAR[scenarioIndex2] + W[scenarioIndex1, scenarioIndex2])
                                keep = false;
                        }
                    }
                }
                if (keep)
                    newList.Add(_scenarios[scenarioIndex1]);
                else
                    _scenarios[scenarioIndex1].Active = false;
            }

            // Update the current list.
            _scenarios.Clear();
            _scenarios.AddRange(newList);
        }

        bool RunScenarios(IExperimentationContext context)
        {
            int replicationsSubmitted = 0;
#if false
            // This is typical code for submitting individual replications to Simio for running.
            foreach (IScenario scenario in _scenarios)
            {
                for (int replicationNumber = scenario.ReplicationsCompleted; replicationNumber < scenario.ReplicationsRequired; replicationNumber++)
                {
                    context.SubmitReplication(scenario, replicationNumber + 1, null);
                    replicationsSubmitted++;
                }
            }
#else
            // However, we're going to submit jobs in replication order.  That is, for all scenarios we'll
            // submit replication k, and then for all scenarios replication k+1, etc.  That way, we won't
            // see all replications for a single scenario start running across multiple processors.
            int lowestReplicationCompleted = int.MaxValue;
            int highestReplicationRequested = int.MinValue;
            // Find the lowest and highest replication number for this set of runs.
            foreach (IScenario scenario in _scenarios)
            {
                if (scenario.ReplicationsCompleted < lowestReplicationCompleted)
                    lowestReplicationCompleted = scenario.ReplicationsCompleted;
                if (scenario.ReplicationsRequired > highestReplicationRequested)
                    highestReplicationRequested = scenario.ReplicationsRequired;
            }
            // Now submit all scenarios for the first replication, then all for the next, etc.
            for (int replicationNumber = lowestReplicationCompleted + 1; replicationNumber <= highestReplicationRequested; replicationNumber++)
            {
                foreach (IScenario scenario in _scenarios)
                {
                    if (scenario.ReplicationsCompleted < replicationNumber && replicationNumber <= scenario.ReplicationsRequired)
                    {
                        context.SubmitReplication(scenario, replicationNumber, null);
                        replicationsSubmitted++;
                    }
                }
            }
#endif

            // As Simio finishes running each replication, it will place the results onto a results queue.  It is this add-in's
            // responsibility to retrieve these results from Simio, do any further processing (i.e. if you want to do something
            // with the raw data), and finally return the results back to Simio.
            int replicationsCompleted = 0;
            bool canceled = false;
            while (replicationsCompleted < replicationsSubmitted)
            {
                // Note that this is a blocking call.  It will not return until a replication is ready for further processing.
                IReplicationResults results = context.WaitForResults();

                // Check for user cancel.
                if (results == null)
                    return false; // The user canceled the entire experiment, so return false to indicate that we did not run to completion.

                // We want to count how many individual replications have come back as either Completed, Canceled or Failed.
                // However, we want to ignore Pending, Running and Idle, because we're only counting replications that have
                // actually been processed.
                switch (results.Status)
                {
                    case ExperimentationStatus.Completed:
                    case ExperimentationStatus.Canceled:
                    case ExperimentationStatus.Failed:
                        replicationsCompleted++;
                        break;
                }

                // IReplicationResults.Status tells us if each replication is starting, ending, or whatever.
                // "Running" means it is just starting up, and we don't really care about that.  But if it
                // is "Completed" or "Failed", we consider it done, so we can advance the progress bar.
                if (results.Status == ExperimentationStatus.Completed || results.Status == ExperimentationStatus.Failed)
                {
                    // Report progress.  This is for controlling the progress bar for each scenario.
                    double percent = (double)replicationsCompleted / replicationsSubmitted;
                    context.ReportProgress((int)(percent * 100), null);
                }
                else if (results.Status == ExperimentationStatus.Canceled)
                    canceled = true;

                // In any case, send the results of this replication back to Simio.
                context.RecordReplicationResults(results);
            }

            // There are no more results, so we can return.
            if (canceled)
                return false;

            return true; // we ran to completion.
        }

        IExperimentResponse _primaryResponse;
        List<IScenario> _scenarios;
    }
}