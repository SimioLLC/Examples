using System;
using System.Collections.Generic;
using System.Text;
using SimioAPI;
using SimioAPI.Extensions;

namespace SimioSelectionRules
{
    [SelectionRuleDefinitionDeprecated]
    public class CampaignUpRuleDefinition : ISelectionRuleDefinition
    {
        #region ISelectionRuleDefinition Members

        /// <summary>
        /// Name of the selection rule. May contain any characters.
        /// </summary>
        public string Name
        {
            get { return "Campaign Up"; }
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
        static readonly Guid MY_ID = new Guid("{F93C75A1-EA41-4E66-90C3-61DE1D7D5D87}");

        /// <summary>
        /// Defines the property schema for the selection rule.
        /// </summary>
        public void DefineSchema(IPropertyDefinitions schema)
        {
            IPropertyDefinition pd;

            pd = schema.AddExpressionProperty("ValueExpression", "Candidate.Entity.Priority");
            pd.Description = "The expression used with the 'Campaign Up' selection rule. " +
                "In the expression, use the keyword 'Candidate' to reference an object in the collection of candidates (e.g., Candidate.Entity.Priority).";
            pd.Required = true;
            pd.DisplayName = "Value Expression";

            pd = schema.AddExpressionProperty("FilterExpression", String.Empty);
            pd.Description = "The expression used to filter the list before the 'Campaign Up' selection rule is applied. " +
                "In the expression, use the keyword 'Candidate' to reference an object in the collection of candidates (e.g., Candidate.Entity.Priority).";
            pd.Required = false;
            pd.DisplayName = "Filter Expression";
        }

        /// <summary>
        /// Creates a new instance of the selection rule.
        /// </summary>
        public ISelectionRule CreateRule(IPropertyReaders properties)
        {
            return new CampaignUpRule(properties);
        }

        #endregion
    }

    public class CampaignUpRule : ISelectionRule
    {
        public CampaignUpRule(IPropertyReaders properties)
        {
            _valueProperty = properties.GetProperty("ValueExpression");
            _filterProperty = properties.GetProperty("FilterExpression");
        }

        IPropertyReader _valueProperty;
        IPropertyReader _filterProperty;

        double _lastValue = Double.NegativeInfinity;

        #region ISelectionRule Members

        /// <summary>
        /// Method called when the selection rule is being used to select an item from a collection of candidates.
        /// </summary>
        public IExecutionContext Select(IEnumerable<IExecutionContext> candidates)
        {
            double nextValue = Double.PositiveInfinity;
            IExecutionContext next = null;

            double startValue = Double.PositiveInfinity;
            IExecutionContext startNext = null;

            foreach (IExecutionContext candidate in candidates)
            {
                bool bProcessItem = true;
                if (String.IsNullOrEmpty(_filterProperty.GetStringValue(candidate)) == false)
                    bProcessItem = (_filterProperty.GetDoubleValue(candidate) > 0);

                if (bProcessItem)
                {
                    double thisValue = _valueProperty.GetDoubleValue(candidate);

                    if (thisValue >= _lastValue)
                    {
                        if (thisValue < nextValue)
                        {
                            nextValue = thisValue;
                            next = candidate;
                        }
                    }
                    else if (thisValue < startValue)
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
