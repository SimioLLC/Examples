using System;
using System.Collections.Generic;
using SimioAPI;
using SimioAPI.Extensions;

namespace SimioSelectionRules
{
    #region StandardDispatchingRuleDefinition Class

    public class StandardDispatchingRuleDefinition : ISelectionRuleDefinition
    {
        /// <summary>
        /// Specifies a priority dispatching rule.
        /// </summary>
        public enum DispatchingRule
        {
            FirstInQueue,
            LargestPriorityValue,
            SmallestPriorityValue,
            EarliestDueDate,
            CriticalRatio,
            LeastSetupTime,
            LongestProcessingTime,
            ShortestProcessingTime,
            LeastSlackTime,
            LeastSlackTimePerOperation,
            LeastWorkRemaining,
            FewestOperationsRemaining,
            LongestTimeWaiting,
            ShortestTimeWaiting,
            LargestAttributeValue,
            SmallestAttributeValue,
            CampaignSequenceUp,
            CampaignSequenceDown,
            CampaignSequenceCycle
        }

        #region ISelectionRuleDefinition Members

        /// <summary>
        /// Name of the selection rule. May contain any characters.
        /// </summary>
        public string Name
        {
            get { return "Standard Dispatching Rule"; }
        }

        /// <summary>
        /// Description text for the selection rule.
        /// </summary>
        public string Description
        {
            get { return ""; }
        }

        /// <summary>
        /// An icon to display for the selection rule in the UI.
        /// </summary>
        public System.Drawing.Image Icon
        {
            get { return null; }
        }

        /// <summary>
        /// Guid which uniquely identifies the selection rule.
        /// </summary>
        public Guid UniqueID
        {
            get { return MY_ID; }
        }
        static readonly Guid MY_ID = new Guid("{60F74791-6CCC-453B-8DA7-946990D495D5}");

        /// <summary>
        /// Defines the property schema for the selection rule.
        /// </summary>
        public void DefineSchema(IPropertyDefinitions schema)
        {
            IPropertyDefinition pd;
            IRepeatGroupPropertyDefinition rgpd;

            pd = schema.AddBooleanProperty("IsRepeatGroup");
            pd.DisplayName = "Repeat Group";
            pd.Description = "Indicates whether a repeat group data structure is used to define the primary dispatching rule and any tie breaker rules.";
            pd.SetDefaultString(schema, "False");

            pd = schema.AddEnumProperty("DispatchingRule", typeof(DispatchingRule));
            pd.DisplayName = "Dispatching Rule";
            pd.Description = "The primary criteria used to select the next item from the queue.\n\n" +
                "Note that using a particular dispatching rule may require some specific model data about the candidates, such as due dates, job routings, expected setup or operation times, etc. " +
                "For a more in-depth discussion on standard dispatching rule usage, please refer to Simio's dynamic selection rule documentation.";
            pd.SetDefaultString(schema, DispatchingRule.FirstInQueue.ToString());
            pd.SwitchPropertyName = "IsRepeatGroup";
            pd.SwitchValues = "False";

            pd = schema.AddExpressionProperty("AttributeValueExpression", "Candidate.Entity.Priority");
            pd.DisplayName = "Attribute Value Expression";
            pd.Description = "The expression used to get the attribute value for each candidate. In the expression, use the syntax " +
                "Candidate.[Class].[Attribute] to reference an attribute of the candidates (e.g., Candidate.Entity.Priority).";
            pd.ParentPropertyName = "DispatchingRule";
            pd.SwitchPropertyName = "DispatchingRule";
            pd.SwitchValues = DispatchingRule.LargestAttributeValue.ToString() + "," + DispatchingRule.SmallestAttributeValue.ToString();

            pd = schema.AddExpressionProperty("CampaignValueExpression", "Candidate.Entity.Priority");
            pd.DisplayName = "Campaign Value Expression";
            pd.Description = "The expression used to get the campaign value for each candidate. In the expression, use the syntax " +
                "Candidate.[Class].[Attribute] to reference an attribute of the candidates (e.g., Candidate.Entity.Priority).";
            pd.ParentPropertyName = "DispatchingRule";
            pd.SwitchPropertyName = "DispatchingRule";
            pd.SwitchValues = DispatchingRule.CampaignSequenceUp.ToString() + "," + DispatchingRule.CampaignSequenceDown.ToString() + "," + DispatchingRule.CampaignSequenceCycle.ToString();

            pd = schema.AddEnumProperty("TieBreakerRule", typeof(DispatchingRule));
            pd.DisplayName = "Tie Breaker Rule";
            pd.Description = "The secondary criteria used to break ties.\n\n" +
                "Note that using a particular dispatching rule may require some specific model data about the candidates, such as due dates, job routings, expected setup or operation times, etc. " +
                "For a more in-depth discussion on standard dispatching rule usage, please refer to Simio's dynamic selection rule documentation.";
            pd.SetDefaultString(schema, DispatchingRule.FirstInQueue.ToString());
            pd.SwitchPropertyName = "IsRepeatGroup";
            pd.SwitchValues = "False";

            pd = schema.AddExpressionProperty("TieBreakerAttributeValueExpression", "Candidate.Entity.Priority");
            pd.DisplayName = "Attribute Value Expression";
            pd.Description = "The expression used to get the attribute value for each candidate. In the expression, use the syntax " +
                "Candidate.[Class].[Attribute] to reference an attribute of the candidates (e.g., Candidate.Entity.Priority).";
            pd.ParentPropertyName = "TieBreakerRule";
            pd.SwitchPropertyName = "TieBreakerRule";
            pd.SwitchValues = DispatchingRule.LargestAttributeValue.ToString() + "," + DispatchingRule.SmallestAttributeValue.ToString();

            pd = schema.AddExpressionProperty("TieBreakerCampaignValueExpression", "Candidate.Entity.Priority");
            pd.DisplayName = "Campaign Value Expression";
            pd.Description = "The expression used to get the campaign value for each candidate. In the expression, use the syntax " +
                "Candidate.[Class].[Attribute] to reference an attribute of the candidates (e.g., Candidate.Entity.Priority).";
            pd.ParentPropertyName = "TieBreakerRule";
            pd.SwitchPropertyName = "TieBreakerRule";
            pd.SwitchValues = DispatchingRule.CampaignSequenceUp.ToString() + "," + DispatchingRule.CampaignSequenceDown.ToString() + "," + DispatchingRule.CampaignSequenceCycle.ToString();

            rgpd = schema.AddRepeatGroupProperty("DispatchingRules");
            rgpd.DisplayName = "Dispatching Rules";
            rgpd.Description = "The primary dispatching rule and any tie breaker rules, applied in the order listed.";
            rgpd.Required = false;
            rgpd.SwitchPropertyName = "IsRepeatGroup";
            rgpd.SwitchValues = "True";

            #region Dispatching Rules Repeating Property Definitions

            pd = rgpd.PropertyDefinitions.AddEnumProperty("RepeatingDispatchingRule", typeof(DispatchingRule));
            pd.DisplayName = "Dispatching Rule";
            pd.Description = "The criteria used to select the next entity from the queue.\n\n" +
                "Note that using a particular dispatching rule may require some specific model data about the candidate entities, such as due dates, job routings, expected setup or operation times, etc. " +
                "For a more in-depth discussion on standard dispatching rule usage, please refer to Simio's dynamic selection rule documentation.";
            pd.CategoryName = "Rule Configuration";
            pd.SetDefaultString(rgpd.PropertyDefinitions, DispatchingRule.FirstInQueue.ToString());

            pd = rgpd.PropertyDefinitions.AddExpressionProperty("RepeatingAttributeValueExpression", "Candidate.Entity.Priority");
            pd.DisplayName = "Attribute Value Expression";
            pd.Description = "The expression used to get the attribute value for each candidate entity. In the expression, use the syntax " +
                "Candidate.[EntityClass].[Attribute] to reference an attribute of the candidate entities (e.g., Candidate.Entity.Priority).";
            pd.CategoryName = "Rule Configuration";
            pd.SwitchPropertyName = "RepeatingDispatchingRule";
            pd.SwitchValues = DispatchingRule.LargestAttributeValue.ToString() + "," + DispatchingRule.SmallestAttributeValue.ToString();

            pd = rgpd.PropertyDefinitions.AddExpressionProperty("RepeatingCampaignValueExpression", "Candidate.Entity.Priority");
            pd.DisplayName = "Campaign Value Expression";
            pd.Description = "The expression used to get the campaign value for each candidate entity. In the expression, use the syntax " +
                "Candidate.[EntityClass].[Attribute] to reference an attribute of the candidate entities (e.g., Candidate.Entity.Priority).";
            pd.CategoryName = "Rule Configuration";
            pd.SwitchPropertyName = "RepeatingDispatchingRule";
            pd.SwitchValues = DispatchingRule.CampaignSequenceUp.ToString() + "," + DispatchingRule.CampaignSequenceDown.ToString() + "," + DispatchingRule.CampaignSequenceCycle.ToString();

            #endregion

            pd = schema.AddExpressionProperty("FilterExpression", String.Empty);
            pd.DisplayName = "Filter Expression";
            pd.Description = "Optional condition evaluated for each candidate that must be true for the candidate to be selected. In the expression, use the syntax " +
                "Candidate.[Class].[Attribute] to reference an attribute of the candidates (e.g., Candidate.Entity.Priority).";
            pd.Required = false;

            pd = schema.AddExpressionProperty("LookAheadWindow", "Infinity");
            pd.DisplayName = "Look Ahead Window (Days)";
            pd.Description = "Only candidate entities whose due dates fall within this specified time window will be considered for selection. " +
                "Note that if there are no candidates whose due dates fall within the look ahead window, then the window will be automatically extended to " +
                "include the candidate(s) with the earliest due date.";

            #region Hidden Expression Properties Used To Determine Selection Priorities

            pd = schema.AddExpressionProperty("PriorityValueExpression", "Candidate.Entity.Priority");
            pd.DisplayName = "Priority Value Expression";
            pd.Description = "The expression used to get the priority value for a candidate entity.";
            pd.Visible = false;

            pd = schema.AddExpressionProperty("DueDateExpression", "Candidate.Entity.Sequence.DueDate");
            pd.DisplayName = "Due Date Expression";
            pd.Description = "The expression used to get the due date value for a candidate entity.";
            pd.Visible = false;

            pd = schema.AddExpressionProperty("CriticalRatioExpression", "Candidate.Entity.Sequence.CriticalRatio");
            pd.DisplayName = "Critical Ratio Expression";
            pd.Description = "The expression used to get the critical ratio value for a candidate entity.";
            pd.Visible = false;

            pd = schema.AddExpressionProperty("ExpectedSetupTimeExpression", "Math.If(Object.Is.Fixed, Fixed.ExpectedSetupTimeFor(Candidate.Entity), " +
                "Object.Is.Node, Math.If(Node.IsInputNode, Node.AssociatedObject.Fixed.ExpectedSetupTimeFor(Candidate.Entity), Math.NaN), Math.NaN)");
            pd.DisplayName = "Expected Setup Time Expression";
            pd.Description = "The expression used to estimate an expected setup time for a candidate entity.";
            pd.Visible = false;

            pd = schema.AddExpressionProperty("ExpectedOperationTimeExpression", "Math.If(Object.Is.Fixed, Fixed.ExpectedOperationTimeFor(Candidate.Entity), " +
                "Object.Is.Node, Math.If(Node.IsInputNode, Node.AssociatedObject.Fixed.ExpectedOperationTimeFor(Candidate.Entity), Math.NaN), Math.NaN)");
            pd.DisplayName = "Expected Operation Time Expression";
            pd.Description = "The expression used to estimate an expected operation time for a candidate entity.";
            pd.Visible = false;

            pd = schema.AddExpressionProperty("SlackTimeExpression", "Candidate.Entity.Sequence.SlackTime");
            pd.DisplayName = "Slack Time Expression";
            pd.Description = "The expression used to get the slack time for a candidate entity.";
            pd.Visible = false;

            pd = schema.AddExpressionProperty("SlackTimePerOperationExpression", "Candidate.Entity.Sequence.SlackTimePerOperation");
            pd.DisplayName = "Slack Time Per Operation Expression";
            pd.Description = "The expression used to get the slack time per operation for a candidate entity.";
            pd.Visible = false;

            pd = schema.AddExpressionProperty("ExpectedWorkRemainingExpression", "Candidate.Entity.Sequence.ExpectedOperationTimeRemaining");
            pd.DisplayName = "Expected Work Remaining Expression";
            pd.Description = "The expression used to estimate the expected work remaining for a candidate entity.";
            pd.Visible = false;

            pd = schema.AddExpressionProperty("NumberOperationsRemainingExpression", "Candidate.Entity.Sequence.NumberJobStepsRemaining");
            pd.DisplayName = "Number Operations Remaining Expression";
            pd.Description = "The expression used to get the number of operations remaining for a candidate entity.";
            pd.Visible = false;

            #endregion
        }

        /// <summary>
        /// Creates a new instance of the selection rule.
        /// </summary>
        public ISelectionRule CreateRule(IPropertyReaders properties)
        {
            return new StandardDispatchingRule(properties);
        }

        #endregion
    }

    #endregion

    #region StandardDispatchingRule Class

    public class StandardDispatchingRule : ISelectionRule, ISelectionRuleInitializable, ISelectionRuleNotifiable, IComparable<ISelectionRule>
    {
        public StandardDispatchingRule(IPropertyReaders properties)
        {
            _isRepeatGroupProperty = properties.GetProperty("IsRepeatGroup");
            _dispatchingRuleProperty = properties.GetProperty("DispatchingRule");
            _attributeValueExpressionProperty = properties.GetProperty("AttributeValueExpression");
            _campaignValueExpressionProperty = properties.GetProperty("CampaignValueExpression");
            _tieBreakerRuleProperty = properties.GetProperty("TieBreakerRule");
            _tieBreakerAttributeValueExpressionProperty = properties.GetProperty("TieBreakerAttributeValueExpression");
            _tieBreakerCampaignValueExpressionProperty = properties.GetProperty("TieBreakerCampaignValueExpression");
            _dispatchingRulesRepeatingProperty = properties.GetProperty("DispatchingRules") as IRepeatingPropertyReader;
            _filterExpressionProperty = properties.GetProperty("FilterExpression");
            _lookAheadWindowProperty = properties.GetProperty("LookAheadWindow");
            _priorityValueExpressionProperty = properties.GetProperty("PriorityValueExpression");
            _dueDateExpressionProperty = properties.GetProperty("DueDateExpression");
            _criticalRatioExpressionProperty = properties.GetProperty("CriticalRatioExpression");
            _expectedSetupTimeExpressionProperty = properties.GetProperty("ExpectedSetupTimeExpression");
            _expectedOperationTimeExpressionProperty = properties.GetProperty("ExpectedOperationTimeExpression");
            _slackTimeExpressionProperty = properties.GetProperty("SlackTimeExpression");
            _slackTimePerOperationExpressionProperty = properties.GetProperty("SlackTimePerOperationExpression");
            _expectedWorkRemainingExpressionProperty = properties.GetProperty("ExpectedWorkRemainingExpression");
            _numberOperationsRemainingExpressionProperty = properties.GetProperty("NumberOperationsRemainingExpression");
        }

        #region ISelectionRule Members

        /// <summary>
        /// Called by Simio to determine which (if any) member of the <paramref name="candidates"/> collection is selected by the rule.
        /// </summary>
        /// <param name="candidates">The collection of candidates to select from.</param>
        /// <returns>The selected member of the <paramref name="candidates"/> collection or null to indicate no selection.</returns>
        public IExecutionContext Select(IEnumerable<IExecutionContext> candidates)
        {
            if (double.IsPositiveInfinity(_lookAheadWindow) == false)
            {
                //
                // If there are no eligible candidates whose due dates fall within the specified look ahead window, then automatically extend
                // the window to include the candidate(s) with the earliest due date.
                //
                bool bExtendLookAheadWindow = true;
                double earliestDueDate = double.PositiveInfinity;

                foreach (var candidate in candidates)
                {
                    //
                    // A candidate is eligible if the Filter Expression is not specified or that expression returns True.
                    //
                    bool bCandidateIsEligible = _bFilterExpressionIsSpecified == false || _filterExpressionProperty.GetDoubleValue(candidate) > 0.0;

                    if (bCandidateIsEligible == true)
                    {
                        double candidateDueDate = _dueDateExpressionProperty.GetDoubleValue(candidate);
                        if (candidateDueDate <= _timeNow + _lookAheadWindow)
                        {
                            bExtendLookAheadWindow = false;
                            break;
                        }
                        else
                            earliestDueDate = Math.Min(earliestDueDate, candidateDueDate);
                    }
                }

                if (bExtendLookAheadWindow == true)
                {
                    _lookAheadWindow = earliestDueDate - _timeNow;
                }
            }

            //
            // Select the best candidate using the specified dispatching rules.
            //
            _selection = null;
            _selectionPriorityComparisonValues = new double[_dispatchingRules.Length];
            var candidatePriorityComparisonValues = new double[_dispatchingRules.Length];
            var candidatePriorityComparisonResults = new PriorityComparisonResult[_dispatchingRules.Length];
            int queueRank = 0;

            foreach (var candidate in candidates)
            {
                //
                // A candidate is eligible if the Filter Expression is not specified or that expression returns True
                // AND the candidate's due date falls within the look ahead window.
                //
                queueRank++;
                bool bCandidateIsEligible = (_bFilterExpressionIsSpecified == false || _filterExpressionProperty.GetDoubleValue(candidate) > 0.0)
                    && (double.IsPositiveInfinity(_lookAheadWindow) == true || _dueDateExpressionProperty.GetDoubleValue(candidate) <= (_timeNow + _lookAheadWindow));

                if (bCandidateIsEligible == true)
                {
                    if (_dispatchingRules.Length == 0)
                    {
                        _selection = candidate;
                        break; // Can break out of loop on first eligible candidate
                    }

                    //
                    // Check the dispatching rules and compare the candidate entity to the best-thus-far candidate selection.
                    //
                    for (int i = 0; i < _dispatchingRules.Length; i++)
                    {
                        candidatePriorityComparisonResults[i] = CompareCandidatePriority(candidate, queueRank, _dispatchingRules[i], ref candidatePriorityComparisonValues);
                    }

                    for (int i = 0; i < _dispatchingRules.Length; i++)
                    {
                        if (candidatePriorityComparisonResults[i] != PriorityComparisonResult.Equal)
                        {
                            if (candidatePriorityComparisonResults[i] == PriorityComparisonResult.Higher)
                            {
                                //
                                // The candidate is the new best.
                                //
                                _selection = candidate;
                                Array.Copy(candidatePriorityComparisonValues, _selectionPriorityComparisonValues, candidatePriorityComparisonValues.Length);
                            }
                            break; // Can break out of loop if the priority comparison result is not a tie
                        }
                    }
                }
            }

            return _selection;
        }

        #endregion

        #region ISelectionRuleInitializable Members

        /// <summary>
        /// Called by Simio whenever the rule's Select method is about to be called, to first perform any rule initialization that is required
        /// such as the initialization of selection parameters.
        /// </summary>
        /// <param name="context">The runtime data context of the object or element using the rule.</param>
        /// <param name="data">Additional rule initialization data provided by the object or element using the rule.</param>
        public void Initialize(IExecutionContext context, ISelectionRuleInitializationData data)
        {
            //
            // Get the queue, dispatching rules, look ahead window, and current simulation time.
            //
            _queue = data.Queue;
            _dispatchingRules = GetDispatchingRules(context);
            _bFilterExpressionIsSpecified = String.IsNullOrEmpty(_filterExpressionProperty.GetStringValue(context)) == false;
            _lookAheadWindow = Math.Max(_lookAheadWindowProperty.GetDoubleValue(context) * 24.0, 0.0);
            _timeNow = context.Calendar.TimeNow;
        }

        #endregion

        #region ISelectionRuleNotifiable Members

        /// <summary>
        /// Called by Simio to notify the selection rule instance of a possibly relevant event.
        /// </summary>
        /// <param name="context">The runtime data context of the object or element using the rule.</param>
        /// <param name="notification">The notification details.</param>
        public void Notify(IExecutionContext context, ISelectionRuleNotification notification)
        {
            switch (notification.Reason)
            {
                case SelectionRuleNotificationReason.SeizeRequestAccepted:
                case SelectionRuleNotificationReason.RouteRequestAccepted:
                case SelectionRuleNotificationReason.StationEntryRequestAccepted:

                    //
                    // Get the dispatching rules for selecting an entity from the queue.
                    //
                    var dispatchingRules = GetDispatchingRules(context);

                    for (int i = 0; i < dispatchingRules.Length; i++)
                    {
                        if (dispatchingRules[i].Rule == StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceUp ||
                           dispatchingRules[i].Rule == StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceDown ||
                           dispatchingRules[i].Rule == StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceCycle)
                        {
                            //
                            // The dispatching rule is a campaign sequence rule, so update the campaign sequence direction (if necessary)
                            // and the priority comparison benchmark.
                            //
                            double selectionPriorityComparisonValue = GetPriorityComparisonValue(notification.Context, 0, dispatchingRules[i]);
                            double priorityComparisonBenchmark = GetPriorityComparisonBenchmark(dispatchingRules[i]);

                            if (dispatchingRules[i].Rule == StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceCycle)
                            {
                                if (selectionPriorityComparisonValue < priorityComparisonBenchmark)
                                    SetCampaignSequenceDirection(dispatchingRules[i], CampaignSequenceDirection.Down);
                                else if (selectionPriorityComparisonValue > priorityComparisonBenchmark)
                                    SetCampaignSequenceDirection(dispatchingRules[i], CampaignSequenceDirection.Up);
                            }

                            SetPriorityComparisonBenchmark(dispatchingRules[i], selectionPriorityComparisonValue);
                        }
                    }
                    break;

                default:

                    break;
            }
        }

        #endregion

        #region IComparable<ISelectionRule> Members

        /// <summary>
        /// Compares the selection rule with another selection rule of the same type and returns an integer that indicates whether the current rule precedes, follows, or occurs
        /// in the same position in a sort order as the other rule.
        /// </summary>
        /// <param name='otherRule'>The other selection rule to compare to.</param>
        /// <returns> Negative value indicates this rule precedes the other rule, positive value indicates this rule follows the other rule, zero indicates same position.</returns>
        public int CompareTo(ISelectionRule otherRule)
        {
            if (otherRule is StandardDispatchingRule otherStandardDispatchingRule)
            {
                return -1 * otherStandardDispatchingRule.CompareTo(_selectionPriorityComparisonValues);
            }

            return 0;
        }

        /// <summary>
        /// Compares the selection rule with another selection rule of the same type that has the specified array of priority comparison values,
        /// and returns an integer that indicates whether the current rule precedes, follows, or occurs in the same position in a sort order as the other rule.
        /// </summary>
        /// <param name='priorityComparisonValues'>The other rule's array of priority comparison values.</param>
        /// <returns> Negative value indicates this rule precedes the other rule, positive value indicates this rule follows the other rule, zero indicates same position.</returns>
        public int CompareTo(double[] priorityComparisonValues)
        {
            int arrayLength = Math.Min(priorityComparisonValues.Length, _selectionPriorityComparisonValues.Length);

            for (int i = 0; i < arrayLength; i++)
            {
                var result = ComparePriorityComparisonValues(i, priorityComparisonValues[i], _selectionPriorityComparisonValues[i]);

                switch (result)
                {
                    case PriorityComparisonResult.Higher:
                        return 1;
                    case PriorityComparisonResult.Lower:
                        return -1;
                    case PriorityComparisonResult.Equal:
                        break;
                }
            }

            return 0;
        }

        #endregion

        #region Private Members

        private readonly IPropertyReader _isRepeatGroupProperty;
        private readonly IPropertyReader _dispatchingRuleProperty;
        private readonly IPropertyReader _attributeValueExpressionProperty;
        private readonly IPropertyReader _campaignValueExpressionProperty;
        private readonly IPropertyReader _tieBreakerRuleProperty;
        private readonly IPropertyReader _tieBreakerAttributeValueExpressionProperty;
        private readonly IPropertyReader _tieBreakerCampaignValueExpressionProperty;
        private readonly IRepeatingPropertyReader _dispatchingRulesRepeatingProperty;
        private readonly IPropertyReader _filterExpressionProperty;
        private readonly IPropertyReader _lookAheadWindowProperty;
        private readonly IPropertyReader _priorityValueExpressionProperty;
        private readonly IPropertyReader _dueDateExpressionProperty;
        private readonly IPropertyReader _criticalRatioExpressionProperty;
        private readonly IPropertyReader _expectedSetupTimeExpressionProperty;
        private readonly IPropertyReader _expectedOperationTimeExpressionProperty;
        private readonly IPropertyReader _slackTimeExpressionProperty;
        private readonly IPropertyReader _slackTimePerOperationExpressionProperty;
        private readonly IPropertyReader _expectedWorkRemainingExpressionProperty;
        private readonly IPropertyReader _numberOperationsRemainingExpressionProperty;

        private IQueueState _queue;
        private DispatchingRule[] _dispatchingRules;
        private bool _bFilterExpressionIsSpecified;
        private double _lookAheadWindow = double.NaN;
        private double _timeNow = double.NaN;

        private IExecutionContext _selection;
        private double[] _selectionPriorityComparisonValues;

        /// <summary>
        /// Compares the priority value of a candidate entity to the priority of the best-thus-far candidate selection.
        /// </summary>
        /// <param name='candidate'>The candidate entity.</param>
        /// <param name='queueRank'>The queue rank of the candidate entity.</param>
        /// <param name='dispatchingRule'>The dispatching rule.</param>
        /// <param name='candidatePriorityComparisonValues'>Reference to the array for storing the candidate entity's priority comparison values.</param>
        /// <returns>The priority comparison result indicating whether the candidate entity is higher, equal, or lower in selection priority to the best-thus-far candidate selection.</returns>
        private PriorityComparisonResult CompareCandidatePriority(IExecutionContext candidate, int queueRank, DispatchingRule dispatchingRule,
            ref double[] candidatePriorityComparisonValues)
        {
            //
            // Get the priority comparison value for the candidate entity.
            //
            double candidatePriorityComparisonValue = GetPriorityComparisonValue(candidate, queueRank, dispatchingRule);
            candidatePriorityComparisonValues[dispatchingRule.OrderIndex] = candidatePriorityComparisonValue;

            if (_selection == null)
            {
                return PriorityComparisonResult.Higher;
            }

            //
            // Get the priority comparison value for the best-thus-far candidate selection.
            //
            double selectionPriorityComparisonValue = _selectionPriorityComparisonValues[dispatchingRule.OrderIndex];

            //
            // Compare the two priority comparison values.
            //
            return ComparePriorityComparisonValues(dispatchingRule.OrderIndex, candidatePriorityComparisonValue, selectionPriorityComparisonValue);
        }

        /// <summary>
        /// Gets the priority comparison value for a specified candidate entity and dispatching rule.
        /// </summary>
        /// <param name='candidate'>The candidate entity.</param>
        /// <param name='queueRank'>The queue rank of the candidate entity.</param>
        /// <param name='dispatchingRule'>The dispatching rule.</param>
        /// <returns>The priority comparison value.</returns>
        private double GetPriorityComparisonValue(IExecutionContext candidate, int queueRank, DispatchingRule dispatchingRule)
        {
            switch (dispatchingRule.Rule)
            {
                case StandardDispatchingRuleDefinition.DispatchingRule.FirstInQueue:
                    return _queue != null ? queueRank : 0.0;

                case StandardDispatchingRuleDefinition.DispatchingRule.LargestPriorityValue:
                    return _priorityValueExpressionProperty.GetDoubleValue(candidate) * -1.0;

                case StandardDispatchingRuleDefinition.DispatchingRule.SmallestPriorityValue:
                    return _priorityValueExpressionProperty.GetDoubleValue(candidate);

                case StandardDispatchingRuleDefinition.DispatchingRule.EarliestDueDate:
                    return _dueDateExpressionProperty.GetDoubleValue(candidate);

                case StandardDispatchingRuleDefinition.DispatchingRule.CriticalRatio:
                    return _criticalRatioExpressionProperty.GetDoubleValue(candidate);

                case StandardDispatchingRuleDefinition.DispatchingRule.LeastSetupTime:
                    return _expectedSetupTimeExpressionProperty.GetDoubleValue(candidate);

                case StandardDispatchingRuleDefinition.DispatchingRule.LongestProcessingTime:
                    return _expectedOperationTimeExpressionProperty.GetDoubleValue(candidate) * -1.0;

                case StandardDispatchingRuleDefinition.DispatchingRule.ShortestProcessingTime:
                    return _expectedOperationTimeExpressionProperty.GetDoubleValue(candidate);

                case StandardDispatchingRuleDefinition.DispatchingRule.LongestTimeWaiting:
                    return _queue != null ? _queue.TimeWaiting(candidate) * -1.0 : double.NaN;

                case StandardDispatchingRuleDefinition.DispatchingRule.ShortestTimeWaiting:
                    return _queue != null ? _queue.TimeWaiting(candidate) : double.NaN;

                case StandardDispatchingRuleDefinition.DispatchingRule.LeastSlackTime:
                    return _slackTimeExpressionProperty.GetDoubleValue(candidate);

                case StandardDispatchingRuleDefinition.DispatchingRule.LeastSlackTimePerOperation:
                    return _slackTimePerOperationExpressionProperty.GetDoubleValue(candidate);

                case StandardDispatchingRuleDefinition.DispatchingRule.LeastWorkRemaining:
                    return _expectedWorkRemainingExpressionProperty.GetDoubleValue(candidate);

                case StandardDispatchingRuleDefinition.DispatchingRule.FewestOperationsRemaining:
                    return _numberOperationsRemainingExpressionProperty.GetDoubleValue(candidate);

                case StandardDispatchingRuleDefinition.DispatchingRule.LargestAttributeValue:
                    return dispatchingRule.EvaluateAttributeValueExpression(candidate) * -1.0;

                case StandardDispatchingRuleDefinition.DispatchingRule.SmallestAttributeValue:
                    return dispatchingRule.EvaluateAttributeValueExpression(candidate);

                case StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceUp:
                case StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceDown:
                case StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceCycle:
                    return dispatchingRule.EvaluateCampaignValueExpression(candidate);

                default:
                    return double.NaN;
            }
        }

        /// <summary>
        /// Gets the current priority comparison benchmark for a specified dispatching rule.
        /// </summary>
        /// <param name='dispatchingRule'>The dispatching rule.</param>
        /// <returns>The priority comparison benchmark.</returns>
        private double GetPriorityComparisonBenchmark(DispatchingRule dispatchingRule)
        {
            if (_dispatchingRuleOrderIndexToPriorityComparisonBenchmarkMap.TryGetValue(dispatchingRule.OrderIndex, out double priorityComparisonBenchmark) == true)
                return priorityComparisonBenchmark;

            if (dispatchingRule.Rule == StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceUp ||
                dispatchingRule.Rule == StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceCycle)
                return double.NegativeInfinity;

            if (dispatchingRule.Rule == StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceDown)
                return double.PositiveInfinity;

            return double.NaN;
        }

        /// <summary>
        /// Sets the current priority comparison benchmark for a specified dispatching rule.
        /// </summary>
        /// <param name='dispatchingRule'>The dispatching rule.</param>
        /// <param name='value'>The priority comparison benchmark.</param>
        private void SetPriorityComparisonBenchmark(DispatchingRule dispatchingRule, double value)
        {
            if (_dispatchingRuleOrderIndexToPriorityComparisonBenchmarkMap.ContainsKey(dispatchingRule.OrderIndex) == true)
                _dispatchingRuleOrderIndexToPriorityComparisonBenchmarkMap[dispatchingRule.OrderIndex] = value;
            else
                _dispatchingRuleOrderIndexToPriorityComparisonBenchmarkMap.Add(dispatchingRule.OrderIndex, value);
        }

        /// <summary>
        /// Dictionary used to store and lookup the current priority comparison benchmark at a specified dispatching rule order index.
        /// </summary>
        private readonly Dictionary<int, double> _dispatchingRuleOrderIndexToPriorityComparisonBenchmarkMap = new Dictionary<int, double>();

        /// <summary>
        /// Gets the current campaign sequence direction for a specified dispatching rule.
        /// </summary>
        /// <param name='dispatchingRule'>The dispatching rule.</param>
        /// <returns>The campaign sequence direction.</returns>
        private CampaignSequenceDirection GetCampaignSequenceDirection(DispatchingRule dispatchingRule)
        {
            if (_dispatchingRuleOrderIndexToCampaignSequenceDirectionMap.TryGetValue(dispatchingRule.OrderIndex, out CampaignSequenceDirection campaignSequenceDirection) == true)
                return campaignSequenceDirection;

            if (dispatchingRule.Rule == StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceUp ||
                dispatchingRule.Rule == StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceCycle)
                return CampaignSequenceDirection.Up;

            if (dispatchingRule.Rule == StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceDown)
                return CampaignSequenceDirection.Down;

            return CampaignSequenceDirection.None;
        }

        /// <summary>
        /// Sets the current campaign sequence direction for a specified dispatching rule.
        /// </summary>
        /// <param name='dispatchingRule'>The dispatching rule.</param>
        /// <param name='direction'>The campaign sequence direction.</param>
        private void SetCampaignSequenceDirection(DispatchingRule dispatchingRule, CampaignSequenceDirection direction)
        {
            if (_dispatchingRuleOrderIndexToCampaignSequenceDirectionMap.ContainsKey(dispatchingRule.OrderIndex) == true)
                _dispatchingRuleOrderIndexToCampaignSequenceDirectionMap[dispatchingRule.OrderIndex] = direction;
            else
                _dispatchingRuleOrderIndexToCampaignSequenceDirectionMap.Add(dispatchingRule.OrderIndex, direction);
        }

        /// <summary>
        /// Dictionary used to store and lookup the current campaign sequence direction at a specified dispatching rule order index.
        /// </summary>
        private readonly Dictionary<int, CampaignSequenceDirection> _dispatchingRuleOrderIndexToCampaignSequenceDirectionMap = new Dictionary<int, CampaignSequenceDirection>();

        /// <summary>
        /// Compares two specified priority comparison values at a specified dispatching rule order index.
        /// </summary>
        /// <param name='dispatchingRuleOrderIndex'>The dispatching rule order index.</param>
        /// <param name='priorityComparisonValue1'>The first priority comparison value to compare.</param>
        /// <param name='priorityComparisonValue2'>The second priority comparison value to compare.</param>
        /// <returns>The priority comparison result indicating whether the first value is higher, equal, or lower in selection priority to the second value.</returns>
        private PriorityComparisonResult ComparePriorityComparisonValues(int dispatchingRuleOrderIndex, double priorityComparisonValue1, double priorityComparisonValue2)
        {
            var dispatchingRule = _dispatchingRules[dispatchingRuleOrderIndex];

            switch (dispatchingRule.Rule)
            {
                case StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceUp:

                    //
                    // Campaign Sequence Up Rule - Selects the operation with a campaign value that is greater than or equal to the campaign value of the last selected operation (referred to here as the 'benchmark').
                    // If no waiting operation has the same or larger value, then selects the operation with the smallest campaign value to start a new campaign.
                    //
                    double priorityComparisonBenchmark = GetPriorityComparisonBenchmark(dispatchingRule);

                    if ((priorityComparisonValue2 < priorityComparisonBenchmark && priorityComparisonValue1 >= priorityComparisonBenchmark) ||
                        (priorityComparisonValue2 >= priorityComparisonBenchmark && priorityComparisonValue1 >= priorityComparisonBenchmark && priorityComparisonValue1 < priorityComparisonValue2) ||
                        (priorityComparisonValue2 < priorityComparisonBenchmark && priorityComparisonValue1 < priorityComparisonValue2))
                        return PriorityComparisonResult.Higher;
                    else if (priorityComparisonValue1 == priorityComparisonValue2)
                        return PriorityComparisonResult.Equal;
                    else
                        return PriorityComparisonResult.Lower;

                case StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceDown:

                    //
                    // Campaign Sequence Down Rule - Selects the operation with a campaign value that is less than or equal to the campaign value of the last selected operation (referred to here as the 'benchmark').
                    // If no waiting operation has the same or smaller value, then selects the operation with the largest campaign value to start a new campaign.
                    //
                    priorityComparisonBenchmark = GetPriorityComparisonBenchmark(dispatchingRule);

                    if ((priorityComparisonValue2 > priorityComparisonBenchmark && priorityComparisonValue1 <= priorityComparisonBenchmark) ||
                        (priorityComparisonValue2 <= priorityComparisonBenchmark && priorityComparisonValue1 <= priorityComparisonBenchmark && priorityComparisonValue1 > priorityComparisonValue2) ||
                        (priorityComparisonValue2 > priorityComparisonBenchmark && priorityComparisonValue1 > priorityComparisonValue2))
                        return PriorityComparisonResult.Higher;
                    else if (priorityComparisonValue1 == priorityComparisonValue2)
                        return PriorityComparisonResult.Equal;
                    else
                        return PriorityComparisonResult.Lower;

                case StandardDispatchingRuleDefinition.DispatchingRule.CampaignSequenceCycle:

                    //
                    // Campaign Sequence Cycle Rule - Alternates back an forth between an increasing campaign and a decreasing campaign. If the campaign is increasing, it will continue to increase until no operations
                    // remain with the same or larger campaign value. When this occurs, the rule then switches to a decreasing campaign and begins selecting operations that have the same or smaller campaign value.
                    // When all such operations are exhausted, it then returns to an increasing campaign strategy. And so forth.
                    //
                    priorityComparisonBenchmark = GetPriorityComparisonBenchmark(dispatchingRule);
                    var campaignSequenceDirection = GetCampaignSequenceDirection(dispatchingRule);

                    if (campaignSequenceDirection == CampaignSequenceDirection.Up &&
                        ((priorityComparisonValue2 < priorityComparisonBenchmark && priorityComparisonValue1 >= priorityComparisonBenchmark) ||
                        (priorityComparisonValue2 >= priorityComparisonBenchmark && priorityComparisonValue1 >= priorityComparisonBenchmark && priorityComparisonValue1 < priorityComparisonValue2) ||
                        (priorityComparisonValue2 < priorityComparisonBenchmark && priorityComparisonValue1 > priorityComparisonValue2)))
                        return PriorityComparisonResult.Higher;
                    else if (campaignSequenceDirection == CampaignSequenceDirection.Down &&
                        ((priorityComparisonValue2 > priorityComparisonBenchmark && priorityComparisonValue1 <= priorityComparisonBenchmark) ||
                        (priorityComparisonValue2 <= priorityComparisonBenchmark && priorityComparisonValue1 <= priorityComparisonBenchmark && priorityComparisonValue1 > priorityComparisonValue2) ||
                        (priorityComparisonValue2 > priorityComparisonBenchmark && priorityComparisonValue1 < priorityComparisonValue2)))
                        return PriorityComparisonResult.Higher;
                    else if (priorityComparisonValue1 == priorityComparisonValue2)
                        return PriorityComparisonResult.Equal;
                    else
                        return PriorityComparisonResult.Lower;

                default:

                    //
                    // Dispatching rule that is not a campaign sequence rule.
                    //
                    if (priorityComparisonValue1 < priorityComparisonValue2)
                        return PriorityComparisonResult.Higher;
                    else if (priorityComparisonValue1 == priorityComparisonValue2)
                        return PriorityComparisonResult.Equal;
                    else
                        return PriorityComparisonResult.Lower;
            }
        }

        /// <summary>
        /// Represents a single dispatching rule for selecting an entity from the queue.
        /// </summary>
        private struct DispatchingRule
        {
            public DispatchingRule(StandardDispatchingRuleDefinition.DispatchingRule rule, int orderIndex, IExecutionContext context,
                IRepeatingPropertyReader dispatchingRulesRepeatingProperty)
            {
                Rule = rule;
                OrderIndex = orderIndex;
                _context = context;
                _dispatchingRulesRepeatingProperty = dispatchingRulesRepeatingProperty;
                _attributeValueExpressionProperty = null;
                _campaignValueExpressionProperty = null;
            }
            public DispatchingRule(StandardDispatchingRuleDefinition.DispatchingRule rule, int orderIndex, IExecutionContext context,
                IPropertyReader attributeValueExpressionProperty, IPropertyReader campaignValueExpressionProperty)
            {
                Rule = rule;
                OrderIndex = orderIndex;
                _context = context;
                _dispatchingRulesRepeatingProperty = null;
                _attributeValueExpressionProperty = attributeValueExpressionProperty;
                _campaignValueExpressionProperty = campaignValueExpressionProperty;
            }
            private readonly IExecutionContext _context;
            private readonly IRepeatingPropertyReader _dispatchingRulesRepeatingProperty;
            private readonly IPropertyReader _attributeValueExpressionProperty;
            private readonly IPropertyReader _campaignValueExpressionProperty;

            /// <summary>
            /// The priority dispatching rule.
            /// </summary>
            public readonly StandardDispatchingRuleDefinition.DispatchingRule Rule;

            /// <summary>
            /// The index in the ordered list of dispatching rules.
            /// </summary>
            public readonly int OrderIndex;

            /// <summary>
            /// Evaluates the attribute value expression for the dispatching rule.
            /// </summary>
            public double EvaluateAttributeValueExpression(IExecutionContext candidate)
            {
                if (_dispatchingRulesRepeatingProperty != null)
                {
                    using (var dispatchingRuleProperties = _dispatchingRulesRepeatingProperty.GetRow(OrderIndex, _context))
                    {
                        var attributeValueExpressionProperty = dispatchingRuleProperties.GetProperty("RepeatingAttributeValueExpression");
                        return attributeValueExpressionProperty.GetDoubleValue(candidate);
                    }
                }
                else
                    return _attributeValueExpressionProperty.GetDoubleValue(candidate);
            }

            /// <summary>
            /// Evaluates the campaign value expression for the dispatching rule.
            /// </summary>
            public double EvaluateCampaignValueExpression(IExecutionContext candidate)
            {
                if (_dispatchingRulesRepeatingProperty != null)
                {
                    using (var dispatchingRuleProperties = _dispatchingRulesRepeatingProperty.GetRow(OrderIndex, _context))
                    {
                        var campaignValueExpressionProperty = dispatchingRuleProperties.GetProperty("RepeatingCampaignValueExpression");
                        return campaignValueExpressionProperty.GetDoubleValue(candidate);
                    }
                }
                else
                    return _campaignValueExpressionProperty.GetDoubleValue(candidate);
            }

            public static readonly DispatchingRule Nothing = new DispatchingRule(StandardDispatchingRuleDefinition.DispatchingRule.FirstInQueue, -1, null, null, null);
        }

        /// <summary>
        /// Gets the dispatching rules for selecting an entity from the queue.
        /// </summary>
        /// <param name="context">The runtime data context of the object or element using the rule.</param>
        private DispatchingRule[] GetDispatchingRules(IExecutionContext context)
        {
            DispatchingRule[] dispatchingRules;
            bool bIsRepeatGroup = _isRepeatGroupProperty.GetDoubleValue(context) == 1;

            if (bIsRepeatGroup == true)
            {
                dispatchingRules = new DispatchingRule[_dispatchingRulesRepeatingProperty.GetCount(context)];

                for (int i = 0; i < dispatchingRules.Length; i++)
                {
                    using (var dispatchingRuleProperties = _dispatchingRulesRepeatingProperty.GetRow(i, context))
                    {
                        var dispatchingRuleProperty = dispatchingRuleProperties.GetProperty("RepeatingDispatchingRule");

                        dispatchingRules[i] = new DispatchingRule((StandardDispatchingRuleDefinition.DispatchingRule)dispatchingRuleProperty.GetDoubleValue(context), i, context,
                            _dispatchingRulesRepeatingProperty);
                    }
                }
            }
            else
            {
                dispatchingRules = new DispatchingRule[2];

                dispatchingRules[0] = new DispatchingRule((StandardDispatchingRuleDefinition.DispatchingRule)_dispatchingRuleProperty.GetDoubleValue(context), 0, context,
                    _attributeValueExpressionProperty, _campaignValueExpressionProperty);

                dispatchingRules[1] = new DispatchingRule((StandardDispatchingRuleDefinition.DispatchingRule)_tieBreakerRuleProperty.GetDoubleValue(context), 1, context,
                    _tieBreakerAttributeValueExpressionProperty, _tieBreakerCampaignValueExpressionProperty);
            }

            return dispatchingRules;
        }

        /// <summary>
        /// Specifies a priority comparison result.
        /// </summary>
        private enum PriorityComparisonResult
        {
            Higher,
            Equal,
            Lower
        }

        /// <summary>
        /// Specifies a campaign sequence direction.
        /// </summary>
        private enum CampaignSequenceDirection
        {
            None,
            Up,
            Down
        }

        #endregion
    }

    #endregion
}
