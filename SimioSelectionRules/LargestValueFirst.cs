using System;
using System.Collections.Generic;
using SimioAPI;
using SimioAPI.Extensions;

namespace SimioSelectionRules
{
    #region LargestValueFirstRuleDefinition Class

    public class LargestValueFirstRuleDefinition : ISelectionRuleDefinition
    {
        #region ISelectionRuleDefinition Members

        /// <summary>
        /// Name of the selection rule. May contain any characters.
        /// </summary>
        public string Name
        {
            get { return "Largest Value First"; }
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
        static readonly Guid MY_ID = new Guid("{F6DCD09A-9800-41e1-9AC3-F8C90E77CFA8}");

        /// <summary>
        /// Defines the property schema for the selection rule.
        /// </summary>
        public void DefineSchema(IPropertyDefinitions schema)
        {
            IPropertyDefinition pd;

            pd = schema.AddExpressionProperty("DynamicSelectionExpression", "Candidate.Entity.Priority");
            pd.DisplayName = "Value Expression";
            pd.Description = "The expression used to get the value for each candidate. In the expression, use the syntax " +
                "Candidate.[Class].[Attribute] to reference an attribute of the candidates (e.g., Candidate.Entity.Priority).";

            var rgpd = schema.AddRepeatGroupProperty("TieBreakerRules");
            rgpd.DisplayName = "Tie Breaker Rules";
            rgpd.Description = "The tie breaker rules, applied in the order listed.";
            rgpd.Required = false;

            #region Tie Breaker Rules Repeating Property Definitions

            pd = rgpd.PropertyDefinitions.AddEnumProperty("TieBreakerRule", typeof(PriorityComparisonRule));
            pd.DisplayName = "Tie Breaker Rule";
            pd.Description = "The criteria used to select the next entity from the queue.";
            pd.CategoryName = "Rule Configuration";
            pd.SetDefaultString(rgpd.PropertyDefinitions, PriorityComparisonRule.LargestValue.ToString());

            pd = rgpd.PropertyDefinitions.AddExpressionProperty("TieBreakerValueExpression", "Candidate.Entity.Priority");
            pd.DisplayName = "Value Expression";
            pd.Description = "The expression used to get the value for each candidate. In the expression, use the syntax " +
                "Candidate.[Class].[Attribute] to reference an attribute of the candidates (e.g., Candidate.Entity.Priority).";
            pd.CategoryName = "Rule Configuration";

            #endregion

            pd = schema.AddExpressionProperty("FilterExpression", String.Empty);
            pd.DisplayName = "Filter Expression";
            pd.Description = "Optional condition evaluated for each candidate that must be true for the candidate to be selected. In the expression, use the syntax " +
                "Candidate.[Class].[Attribute] to reference an attribute of the candidates (e.g., Candidate.Entity.Priority).";
            pd.Required = false;
        }

        /// <summary>
        /// Creates a new instance of the selection rule.
        /// </summary>
        public ISelectionRule CreateRule(IPropertyReaders properties)
        {
            return new LargestValueFirstRule(properties);
        }

        #endregion
    }

    #endregion

    #region LargestValueFirstRule Class

    public class LargestValueFirstRule : ISelectionRule, ISelectionRuleInitializable, IComparable<ISelectionRule>
    {
        public LargestValueFirstRule(IPropertyReaders properties)
        {
            _valueExpressionProperty = properties.GetProperty("DynamicSelectionExpression");
            _filterExpressionProperty = properties.GetProperty("FilterExpression");
            _tieBreakerRulesRepeatingProperty = properties.GetProperty("TieBreakerRules") as IRepeatingPropertyReader;
        }

        #region ISelectionRule Members

        /// <summary>
        /// Called by Simio to determine which (if any) member of the <paramref name="candidates"/> collection is selected by the rule.
        /// </summary>
        /// <param name="candidates">The collection of candidates to select from.</param>
        /// <returns>The selected member of the <paramref name="candidates"/> collection or null to indicate no selection.</returns>
        public IExecutionContext Select(IEnumerable<IExecutionContext> candidates)
        {
            _selection = null;
            _selectionPriorityComparisonValues = new double[_priorityComparisonRules.Length];
            var candidatePriorityComparisonValues = new double[_priorityComparisonRules.Length];

            foreach (var candidate in candidates)
            {
                //
                // A candidate is eligible if the Filter Expression is not specified or that expression returns True
                //
                bool bCandidateIsEligible = _bFilterExpressionIsSpecified == false || _filterExpressionProperty.GetDoubleValue(candidate) > 0.0;

                if (bCandidateIsEligible == true)
                {
                    //
                    // Check the priority comparison rules and compare the candidate entity to the best-thus-far candidate selection.
                    //
                    for (int i = 0; i < _priorityComparisonRules.Length; i++)
                    {
                        candidatePriorityComparisonValues[i] = _priorityComparisonRules[i].ValueExpressionProperty.GetDoubleValue(candidate);
                    }

                    if (_selection == null)
                    {
                        //
                        // The candidate is the new best.
                        //
                        _selection = candidate;
                        Array.Copy(candidatePriorityComparisonValues, _selectionPriorityComparisonValues, candidatePriorityComparisonValues.Length);
                    }
                    else
                    {
                        for (int i = 0; i < _priorityComparisonRules.Length; i++)
                        {
                            if (_priorityComparisonRules[i].Rule == SimioSelectionRules.PriorityComparisonRule.SmallestValue ?
                                candidatePriorityComparisonValues[i] < _selectionPriorityComparisonValues[i] :
                                candidatePriorityComparisonValues[i] > _selectionPriorityComparisonValues[i])
                            {
                                //
                                // The candidate is the new best.
                                //
                                _selection = candidate;
                                Array.Copy(candidatePriorityComparisonValues, _selectionPriorityComparisonValues, candidatePriorityComparisonValues.Length);
                                break;
                            }

                            // If this candidate loses to the current selection, stop processing rules.
                            if (_priorityComparisonRules[i].Rule == SimioSelectionRules.PriorityComparisonRule.SmallestValue ?
                                candidatePriorityComparisonValues[i] > _selectionPriorityComparisonValues[i] :
                                candidatePriorityComparisonValues[i] < _selectionPriorityComparisonValues[i])
                            {
                                break;
                            }
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
            // Get whether the Filter Expression property is specified and the priority comparison rules
            // used to select an entity from the queue.
            //
            _bFilterExpressionIsSpecified = !String.IsNullOrEmpty(_filterExpressionProperty.GetStringValue(context));
            _priorityComparisonRules = GetPriorityComparisonRules(context);
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
            if (otherRule is LargestValueFirstRule otherLargestValueFirstRule)
            {
                return -1 * otherLargestValueFirstRule.CompareTo(_selectionPriorityComparisonValues);
            }

            return 0;
        }

        /// <summary>
        /// Compares the selection rule with another selection rule of the same type that has the specified priority comparison value,
        /// and returns an integer that indicates whether the current rule precedes, follows, or occurs in the same position in a sort order as the other rule.
        /// </summary>
        /// <param name='priorityComparisonValue'>The other rule's priority comparison value.</param>
        /// <returns> Negative value indicates this rule precedes the other rule, positive value indicates this rule follows the other rule, zero indicates same position.</returns>
        public int CompareTo(double[] priorityComparisonValues)
        {
            int arrayLength = Math.Min(priorityComparisonValues.Length, _selectionPriorityComparisonValues.Length);

            for (int i = 0; i < arrayLength; i++)
            {
                if (_priorityComparisonRules[i].Rule == SimioSelectionRules.PriorityComparisonRule.SmallestValue)
                {
                    if (priorityComparisonValues[i] < _selectionPriorityComparisonValues[i])
                    {
                        return 1;
                    }
                    else if (priorityComparisonValues[i] > _selectionPriorityComparisonValues[i])
                    {
                        return -1;
                    }
                }
                else if (_priorityComparisonRules[i].Rule == SimioSelectionRules.PriorityComparisonRule.LargestValue)
                {
                    if (priorityComparisonValues[i] > _selectionPriorityComparisonValues[i])
                    {
                        return 1;
                    }
                    else if (priorityComparisonValues[i] < _selectionPriorityComparisonValues[i])
                    {
                        return -1;
                    }
                }
            }

            return 0;
        }

        #endregion

        #region Private Members

        private readonly IPropertyReader _valueExpressionProperty;
        private readonly IPropertyReader _filterExpressionProperty;
        private readonly IRepeatingPropertyReader _tieBreakerRulesRepeatingProperty;
        private PriorityComparisonRule[] _priorityComparisonRules;

        private bool _bFilterExpressionIsSpecified;

        private IExecutionContext _selection;
        private double[] _selectionPriorityComparisonValues;

        /// <summary>
        /// Gets the priority comparison rules for selecting an entity from the queue.
        /// </summary>
        /// <param name="context"></param>
        private PriorityComparisonRule[] GetPriorityComparisonRules(IExecutionContext context)
        {
            var priorityComparisonRules = new PriorityComparisonRule[_tieBreakerRulesRepeatingProperty.GetCount(context) + 1];

            priorityComparisonRules[0] = new PriorityComparisonRule(SimioSelectionRules.PriorityComparisonRule.LargestValue, _valueExpressionProperty);

            for (int i = 1; i < priorityComparisonRules.Length; i++)
            {
                using (var tieBreakerRuleProperties = _tieBreakerRulesRepeatingProperty.GetRow(i - 1, context))
                {
                    var tieBreakerRuleProperty = tieBreakerRuleProperties.GetProperty("TieBreakerRule");
                    var tieBreakerValueExpression = tieBreakerRuleProperties.GetProperty("TieBreakerValueExpression");

                    priorityComparisonRules[i] = new PriorityComparisonRule((SimioSelectionRules.PriorityComparisonRule)tieBreakerRuleProperty.GetDoubleValue(context),
                        tieBreakerValueExpression);
                }
            }
            return priorityComparisonRules;
        }

        #region PriorityComparisonRule Struct

        /// <summary>
        /// Represents a priority comparison rule for selecting an entity from the queue.
        /// </summary>
        private struct PriorityComparisonRule
        {
            public PriorityComparisonRule(SimioSelectionRules.PriorityComparisonRule rule, IPropertyReader valueExpressionProperty)
            {
                Rule = rule;
                ValueExpressionProperty = valueExpressionProperty;
            }

            /// <summary>
            /// The priority comparison rule.
            /// </summary>
            public readonly SimioSelectionRules.PriorityComparisonRule Rule;

            /// <summary>
            /// The expression used to get the priority value for each entity in the queue.
            /// </summary>
            public readonly IPropertyReader ValueExpressionProperty;
        }

        #endregion

        #endregion
    }

    #endregion
}
