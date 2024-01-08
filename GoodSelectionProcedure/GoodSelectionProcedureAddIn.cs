using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SimioAPI;
using SimioAPI.Extensions;
using GoodSelectionProcedure.Utilities;
using System.Threading.Tasks;

/**
 * The Good Selection Procedure (GSP)
 *
 * Developed from 
 * Efficient Ranking and Selection in Parallel Computing Environments
 * By Eric C. Ni, Dragos F. Ciocan, Shane G. Henderson, Susan R. Hunter
 *
 * The original paper can be found at:
 * http://arxiv.org/abs/1506.04986
 *
 * Author of the code:
 * Sijia Ma, sm2462@cornell.edu
 */

namespace GoodSelectionProcedure
{
    public class GoodSelectionProcedureAddInDefinition : IExperimentationAddInDefinition
    {
        #region IExperimentationAddInDefinition Members

        /// <summary>
        /// All add-ins must provide a name.  This is displayed in the user-interface for selecting an add-in.
        /// </summary>
        public string Name
        {
            get { return "Select Best Scenario using GSP"; }
        }

        /// <summary>
        /// The optional Description is shown in a tooltip when the user hovers the mouse over the add-in's name.
        /// </summary>
        public string Description
        {
            get { return "Use the Good Selection Procedure for scenario ranking and selection.\nGSP is a parallel scenario selection algorithm with good performance for large-scale problems."; }
        }

        /// <summary>
        /// The optional Icon is displayed next to the add-in's name in the list of add-ins.
        /// </summary>
        public System.Drawing.Image Icon
        {
            get { return Properties.Resources.Icon; }
        }

        /// <summary>
        /// All experimentation add-ins must have their own UniqueID.  If you copy this example for your own purposes, you should
        /// rename it, and change its UniqueID.  You can generate a new Guid by using Microsoft's GuidGen.exe tool or by browsing
        /// to http://www.guidgen.com.
        /// </summary>
        public Guid UniqueID
        {
            get { return MY_ID; }
        }
        static readonly Guid MY_ID = new Guid("{d41b795d-f37e-48ce-b85d-219e29092b5a}");

        /// <summary>
        /// This method is called by Simio whenever it needs to connect this add-in to a Simio experiment.
        /// </summary>
        /// <param name="experiment">The experiment for which this add-in is being created.</param>
        /// <returns>An instance of a class that implements the IExperimentationAddIn interface.</returns>
        public IExperimentationAddIn CreateExperimentationAddIn(IExperiment experiment)
        {
            // The main purpose of this method is to create a new instance of the add-in's IExperimentationAddIn-derived class.
            return new GoodSelectionProcedureAddIn();
        }

        #endregion
    }

    public class GoodSelectionProcedureAddIn : IExperimentationAddIn
    {
        #region IExperimentationAddIn Members

        /// <summary>
        /// Initialize is called by Simio when the add-in is first connected to an experiment by the user,
        /// as well as at project load-time for any experiments that reference this add-in.
        /// </summary>
        /// <param name="context">An object with methods that are useful during add-in initialization.</param>
        public void Initialize(IExperimentationAddInInitializationContext context)
        {
            ProbabilityOfGoodSelectionParameter = context.DefineParameter("Confidence Level", ExperimentParameterType.Real, null, "Confidence level for selecting the best scenario.", Default_ProbabilityOfGoodSelection.ToString());
            IndifferenceZoneParameter = context.DefineParameter("Indifference Zone", ExperimentParameterType.Real, null, "The Indifference Zone defines the smallest meaningful difference that is used for defining the best.  All scenarios that fall within this smallest meaningful difference are close enough that any one of them may be selected as the best.", null);
            ReplicationLimitParameter = context.DefineParameter("Replication Limit", ExperimentParameterType.Integer, null, "The maximum number of replications for each scenario that will be made before automatically terminating the procedure.", Default_ReplicationLimit.ToString()); // add: The procedure is guaranteed to terminate without this limit set.
        }

        internal IExperimentParameter ProbabilityOfGoodSelectionParameter;
        internal IExperimentParameter IndifferenceZoneParameter;
        internal IExperimentParameter ReplicationLimitParameter;

        const double Default_ProbabilityOfGoodSelection = 0.95;
        const int Default_ReplicationLimit = 100;
        // Note there is no default value for IndifferenceZone.

        internal const int n1 = 20; // Need at least this many for any meaningful results.

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
            if (parameter == ProbabilityOfGoodSelectionParameter)
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
                if (Int32.TryParse(proposedValue, out result) == false || result < n1)
                {
                    failureReason = "Replication Limit must be at least " + n1.ToString() + ".";
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
            get { return new GoodSelectionProcedureExperimentRunner(this); }
        }

        #endregion
    }

    // A class for storing scenario and the statistics of its primary response
    // such as sample mean, sample variance, batch size and sample size
    public class IScenarioPar
    {
        IScenario _scenario;
        double _fn;
        double _S2;
        int _bsize;
        int _n;
        public IScenarioPar(IScenario _scenario)
        {
            scenario = _scenario;
        }
        public IScenario scenario
        {
            get { return _scenario; }
            set { _scenario = value; }
        }
        // sample mean
        public double Fn
        {
            get { return _fn; }
            set { _fn = value; }
        }
        // sample variance
        public double S2
        {
            get { return _S2; }
            set { _S2 = value; }
        }
        // batch size
        public int bsize
        {
            get { return _bsize; }
            set { _bsize = value; }
        }
        // sample size
        public int n
        {
            get { return _n; }
            set { _n = value; }
        }
        public void init(double _Fn, double _S2, int _bsize, int _n)
        {
            Fn = _Fn;
            S2 = _S2;
            bsize = _bsize;
            n = _n;
        }
        public void update(double _Fn, int _n)
        {
            Fn = _Fn;
            n = _n;
        }
    }

    // This add-in implements the optional (and advanced) IExperimentRunner interface
    // in order to take greater control of the process of running the experiment.
    // The main reason we do this is so we can do scenario screening.
    public class GoodSelectionProcedureExperimentRunner : IExperimentRunner
    {
        double alpha;
        double delta;
        int replicationLimit;
        int n1; // sample size of stage 1
        int batchSize = 50; // Average batch size
        int rbar = 20; // number of batches in stage 2
        int k; // number of scenarios
        double eta, rinott_h;

        double[] T; // estimated run-times; set default to be 1 to skip this part
        bool ifRinottValid;

        IExperimentResponse _primaryResponse;
        List<IScenarioPar> _scenarios;

        public GoodSelectionProcedureExperimentRunner(GoodSelectionProcedureAddIn addIn)
        {
            _addIn = addIn;
            alpha = 1.0 - double.Parse(_addIn.ProbabilityOfGoodSelectionParameter.Value);
            delta = double.Parse(_addIn.IndifferenceZoneParameter.Value);
            replicationLimit = int.Parse(_addIn.ReplicationLimitParameter.Value);
            n1 = GoodSelectionProcedureAddIn.n1;
            rbar = Math.Max(rbar, replicationLimit / batchSize / 10);
        }
        GoodSelectionProcedureAddIn _addIn;

        #region IExperimentRunner Members

        public void Run(IExperimentationContext context)
        {
            Validate(context); // Will throw an exception if something is not acceptable.

            // Make a list of all of the scenarios currently marked as Active.
            // These will be our starting point, and we'll remove scenarios from
            // this list until only one remains.
            _scenarios = new List<IScenarioPar>();
            foreach (IScenario scenario in context.Experiment.Scenarios)
            {
                if (scenario.Active)
                {
                    _scenarios.Add(new IScenarioPar(scenario));
                }
            }

            if (_scenarios.Count <= 1)
            {
                return; // We have nothing to do.
            }
            k = _scenarios.Count;
            eta = EtaFunc.find_eta(n1, alpha / 2, k);
            rinott_h = Rinott.rinott(k, 1.0 - alpha / 2, n1 - 1);

            T = new double[_scenarios.Count];

            // Stage 0
            RunStage0(context);

            // Stage 1
            if (RunStage1(context) == false)
                return;
            if (_scenarios.Count <= 1)
                return;

            // Stage 2
            if (RunStage2(context) == false)
                return;
            if (_scenarios.Count <= 1)
                return;

            // Stage 3
            RunStage3(context);

            return;
        }

        #endregion

        void RunStage0(IExperimentationContext context)
        {
            for (int scenarioIndex = 0; scenarioIndex < k; scenarioIndex++)
            {
                T[scenarioIndex] = 1.0;
            }
        }

        bool RunStage1(IExperimentationContext context)
        {
            int maxReps = n1;
            foreach (IScenarioPar scenario in _scenarios)
            {
                if (scenario.scenario.ReplicationsRequired > maxReps)
                    maxReps = scenario.scenario.ReplicationsRequired;
                if (scenario.scenario.ReplicationsCompleted > maxReps)
                    maxReps = scenario.scenario.ReplicationsCompleted;
            }
            n1 = maxReps;
            // maxReps now tells us how many replications we need them all to have,
            // so bring the rest of the remaining scenarios up to that many.
            foreach (IScenarioPar scenario in _scenarios)
                scenario.scenario.ReplicationsRequired = maxReps;

            // Run these new replications.
            if (RunScenarios(context) == false)
                return false;

            CalcBatchSize(context);
            DoScreening(context);
            return true;
        }

        bool RunStage2(IExperimentationContext context)
        {
            bool allReachLimit;
            for (int r = 0; r < rbar; r++)
            {
                allReachLimit = true;
                foreach (IScenarioPar scenario in _scenarios)
                {
                    if (scenario.scenario.ReplicationsRequired + scenario.bsize <= replicationLimit)
                    {
                        allReachLimit = false;
                        scenario.scenario.ReplicationsRequired += scenario.bsize;
                    }
                    else
                    {
                        scenario.scenario.ReplicationsRequired = replicationLimit;
                    }

                }
                if (allReachLimit)
                {
                    return true;
                }
                if (RunScenarios(context) == false)
                    return false;
                DoScreening(context);
                if (_scenarios.Count <= 1)
                    return true;
            }
            return true;
        }

        void RunStage3(IExperimentationContext context)
        {
            ifRinottValid = true;
            foreach (IScenarioPar scenario in _scenarios)
            {
                int N_rinott = (int)Math.Ceiling(rinott_h * rinott_h / delta / delta * scenario.S2);
                if (N_rinott > scenario.scenario.ReplicationsCompleted)
                {
                    if (N_rinott > replicationLimit)
                    {
                        ifRinottValid = false;
                        scenario.scenario.ReplicationsRequired = replicationLimit;
                    }
                    else
                    {
                        scenario.scenario.ReplicationsRequired = N_rinott;
                    }
                }
            }
            if (RunScenarios(context) == false)
                return;

            SelectBest(context);
        }

        void CalcBatchSize(IExperimentationContext context)
        {
            // First, we need an array of raw values of the primary response.
            // The first dimension is the scenario, second is the replication.
            double[,] X = new double[_scenarios.Count, n1];
            // Initialize it by asking Simio for the individual replication values.
            for (int scenarioIndex = 0; scenarioIndex < k; scenarioIndex++)
            {
                for (int replicationIndex = 1; replicationIndex <= n1; replicationIndex++)
                {
                    double sampleValue = double.NaN;
                    if (_scenarios[scenarioIndex].scenario.GetResponseValueForReplication(_primaryResponse, replicationIndex, ref sampleValue))
                        X[scenarioIndex, replicationIndex - 1] = sampleValue;
                    else // If we don't get a value from Simio, we can't continue.
                        throw new ApplicationException("IScenario.GetResponseValueForReplication failed for scenario " + _scenarios[scenarioIndex].scenario.Name + " replication " + replicationIndex.ToString());
                }
            }
            double avgST = 0.0;
            double[] Xbar = new double[k];
            double[] S2 = new double[k];

            for (int scenarioIndex = 0; scenarioIndex < k; scenarioIndex++)
            {
                double sum = 0.0;
                double sumSq = 0.0;
                for (int replicationIndex = 0; replicationIndex < n1; replicationIndex++)
                {
                    sum += X[scenarioIndex, replicationIndex];
                    sumSq += X[scenarioIndex, replicationIndex] * X[scenarioIndex, replicationIndex];
                }
                Xbar[scenarioIndex] = sum / (double)n1;
                S2[scenarioIndex] = (sumSq - sum * sum / n1) / (double)(n1 - 1);
                avgST += Math.Sqrt(S2[scenarioIndex] / T[scenarioIndex]);
            }
            avgST /= (double)k;
            int parIndex = 0;
            foreach (IScenarioPar scenario in _scenarios)
            {
                int bsize = (int)Math.Ceiling((double)batchSize * Math.Sqrt(S2[parIndex] / T[parIndex]) / avgST);
                scenario.init(Xbar[parIndex], S2[parIndex], bsize, n1);
                parIndex++;
            }
        }

        void SelectBest(IExperimentationContext context)
        {
            double Xbar = double.NaN;
            _scenarios[0].scenario.GetResponseValue(_primaryResponse, ref Xbar);
            double bestXbar = Xbar;
            int bestIndex = 0;
            for (int scenarioIndex = 0; scenarioIndex < _scenarios.Count; scenarioIndex++)
            {
                _scenarios[scenarioIndex].scenario.GetResponseValue(_primaryResponse, ref Xbar);
                if (_primaryResponse.Objective == ResponseObjective.Maximize)
                {
                    if (Xbar > bestXbar)
                    {
                        bestXbar = Xbar;
                        bestIndex = scenarioIndex;
                    }
                }
                else if (_primaryResponse.Objective == ResponseObjective.Minimize)
                {
                    if (Xbar < bestXbar)
                    {
                        bestXbar = Xbar;
                        bestIndex = scenarioIndex;
                    }
                }
            }
            if (ifRinottValid)
            {
                for (int scenarioIndex = 0; scenarioIndex < _scenarios.Count; scenarioIndex++)
                {
                    if (scenarioIndex != bestIndex)
                    {
                        _scenarios[scenarioIndex].scenario.Active = false;
                    }
                }
            }
            return;
        }

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
            int nGroups = 1;
            double Xbar = double.NaN;
            if (_scenarios.Count > 100)
            {
                nGroups = (int)Math.Sqrt(_scenarios.Count);
            }
            List<IScenarioPar>[] scenarioGroups = new List<IScenarioPar>[nGroups];
            int GroupId = 0;
            // Assign groups
            for (int i = 0; i < nGroups; i++)
            {
                scenarioGroups[i] = new List<IScenarioPar>();
            }
            foreach (IScenarioPar scenario in _scenarios)
            {
                scenario.scenario.GetResponseValue(_primaryResponse, ref Xbar);
                scenario.update(Xbar, scenario.scenario.ReplicationsCompleted);
                scenarioGroups[GroupId].Add(scenario);
                GroupId = (GroupId + 1) % nGroups;
            }
            ConcurrentBag<IScenarioPar> bestBag = new ConcurrentBag<IScenarioPar>();

            // Find the top ones in each of the groups
            Parallel.ForEach(scenarioGroups, (group) =>
            {
                double bestXbar = group[0].Fn;
                IScenarioPar bestScenario = group[0];
                foreach (IScenarioPar scenario in group)
                {
                    if (_primaryResponse.Objective == ResponseObjective.Maximize)
                    {
                        if (scenario.Fn > bestXbar)
                        {
                            bestXbar = scenario.Fn;
                            bestScenario = scenario;
                        }
                    }
                    else if (_primaryResponse.Objective == ResponseObjective.Minimize)
                    {
                        if (scenario.Fn < bestXbar)
                        {
                            bestXbar = scenario.Fn;
                            bestScenario = scenario;
                        }
                    }
                }
                bestBag.Add(bestScenario);
            });
            List<IScenarioPar> bestList = bestBag.ToList();

            // Screening
            ConcurrentBag<IScenarioPar> newBag = new ConcurrentBag<IScenarioPar>();
            Parallel.ForEach(scenarioGroups, (group) =>
            {
                foreach (IScenarioPar scenario1 in group)
                {
                    bool keep = true;

                    // In-group comparison
                    foreach (IScenarioPar scenario2 in group)
                    {
                        if (Object.ReferenceEquals(scenario1, scenario2))
                            continue;
                        keep = CompareScenarios(scenario1, scenario2);
                        if (!keep)
                            break;
                    }
                    if (!keep)
                    {
                        scenario1.scenario.Active = false;
                        continue;
                    }
                    // Compare with the top ones
                    foreach (IScenarioPar scenario2 in bestList)
                    {
                        if (Object.ReferenceEquals(scenario1, scenario2))
                            continue;
                        keep = CompareScenarios(scenario1, scenario2);
                        if (!keep)
                            break;
                    }
                    if (keep)
                        newBag.Add(scenario1);
                    else
                        scenario1.scenario.Active = false;
                }
            });

            // Update the current list;
            _scenarios.Clear();
            _scenarios.AddRange(newBag);
            k = _scenarios.Count;

        }

        bool CompareScenarios(IScenarioPar scenario1, IScenarioPar scenario2)
        {
            int n1_rbar = n1 + rbar * scenario1.bsize;
            int n2_rbar = n1 + rbar * scenario2.bsize;
            double tau_rbar = 1.0 / (scenario1.S2 / n1_rbar + scenario2.S2 / n2_rbar);
            double a = eta * Math.Sqrt((n1 - 1) * tau_rbar);

            double tau = 1.0 / (scenario1.S2 / scenario1.n + scenario2.S2 / scenario2.n);
            double Y = tau * (scenario1.Fn - scenario2.Fn);
            if (_primaryResponse.Objective == ResponseObjective.Maximize)
            {
                if (Y < -a)
                {
                    return false;
                }
            }
            else if (_primaryResponse.Objective == ResponseObjective.Minimize)
            {
                if (Y > a)
                {
                    return false;
                }
            }
            return true;
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
            foreach (IScenarioPar scenario in _scenarios)
            {
                if (scenario.scenario.ReplicationsCompleted < lowestReplicationCompleted)
                    lowestReplicationCompleted = scenario.scenario.ReplicationsCompleted;
                if (scenario.scenario.ReplicationsRequired > highestReplicationRequested)
                    highestReplicationRequested = scenario.scenario.ReplicationsRequired;
            }
            // Now submit all scenarios for the first replication, then all for the next, etc.
            for (int replicationNumber = lowestReplicationCompleted + 1; replicationNumber <= highestReplicationRequested; replicationNumber++)
            {
                foreach (IScenarioPar scenario in _scenarios)
                {
                    if (scenario.scenario.ReplicationsCompleted < replicationNumber && replicationNumber <= scenario.scenario.ReplicationsRequired)
                    {
                        context.SubmitReplication(scenario.scenario, replicationNumber, null);
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
    }
}
