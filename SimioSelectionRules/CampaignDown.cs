using System;
using System.Collections.Generic;
using System.Text;
using SimioAPI;
using SimioAPI.Extensions;

namespace SimioSelectionRules
{
    [SelectionRuleDefinitionDeprecated]
    public class CampaignDownRuleDefinition : ISelectionRuleDefinition
    {
        #region ISelectionRuleDefinition Members

        /// <summary>
        /// Name of the selection rule. May contain any characters.
        /// </summary>
        public string Name
        {
            get { return "Campaign Down"; }
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
        static readonly Guid MY_ID = new Guid("{9A325E2F-6D62-4EE1-8769-A3A67081C0CE}");

        /// <summary>
        /// Defines the property schema for the selection rule.
        /// </summary>
        public void DefineSchema(IPropertyDefinitions schema)
        {
            IPropertyDefinition pd;

            pd = schema.AddExpressionProperty("ValueExpression", "Candidate.Entity.Priority");
            pd.Description = "The expression used with the 'Campaign Down' selection rule. " +
                "In the expression, use the keyword 'Candidate' to reference an object in the collection of candidates (e.g., Candidate.Entity.Priority).";
            pd.Required = true;
            pd.DisplayName = "Value Expression";

            pd = schema.AddExpressionProperty("FilterExpression", String.Empty);
            pd.Description = "The expression used to filter the list before the 'Campaign Down' selection rule is applied. " +
                "In the expression, use the keyword 'Candidate' to reference an object in the collection of candidates (e.g., Candidate.Entity.Priority).";
            pd.Required = false;
            pd.DisplayName = "Filter Expression";
        }

        /// <summary>
        /// Creates a new instance of the selection rule.
        /// </summary>
        public ISelectionRule CreateRule(IPropertyReaders properties)
        {
            return new CampaignDownRule(properties);
        }

        #endregion
    }

    public class CampaignDownRule : ISelectionRule
    {
        public CampaignDownRule(IPropertyReaders properties)
        {
            _valueProperty = properties.GetProperty("ValueExpression");
            _filterProperty = properties.GetProperty("FilterExpression");
        }

        IPropertyReader _valueProperty;
        IPropertyReader _filterProperty;

        double _lastValue = Double.PositiveInfinity;

        #region ISelectionRule Members

        /// <summary>
        /// Method called when the selection rule is being used to select an item from a collection of candidates.
        /// </summary>
        public IExecutionContext Select(IEnumerable<IExecutionContext> candidates)
        {
            double nextValue = Double.NegativeInfinity;
            IExecutionContext next = null;

            double startValue = Double.NegativeInfinity;
            IExecutionContext startNext = null;

            foreach (IExecutionContext candidate in candidates)
            {
                bool bProcessItem = true;
                if (String.IsNullOrEmpty(_filterProperty.GetStringValue(candidate)) == false)
                    bProcessItem = (_filterProperty.GetDoubleValue(candidate) > 0);

                if (bProcessItem)
                {
                    double thisValue = _valueProperty.GetDoubleValue(candidate);

                    if (thisValue <= _lastValue)
                    {
                        if (thisValue > nextValue)
                        {
                            nextValue = thisValue;
                            next = candidate;
                        }
                    }
                    else if (thisValue > startValue)
                    {
                        startValue = thisValue;
                        startNext = candidate;
                    }
                }
            }

            if (next == null)
            {
                _lastValue = startValue;
                return startNext;
            }

            _lastValue = nextValue;
            return next;
        }

        #endregion
    }
}
