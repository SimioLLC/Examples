using System;
using System.Collections.Generic;
using System.Text;
using SimioAPI;
using SimioAPI.Extensions;

namespace SimioSelectionRules
{
    [SelectionRuleDefinitionDeprecated]
    public class CampaignCycleRuleDefinition : ISelectionRuleDefinition
    {
        #region ISelectionRuleDefinition Members

        /// <summary>
        /// Name of the selection rule. May contain any characters.
        /// </summary>
        public string Name 
        { 
            get { return "Campaign Cycle"; } 
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
        static readonly Guid MY_ID = new Guid("{BDA248B9-5A99-4E0C-81A9-B3DB63092518}");

        /// <summary>
        /// Defines the property schema for the selection rule.
        /// </summary>
        public void DefineSchema(IPropertyDefinitions schema)
        {
            IPropertyDefinition pd;

            pd = schema.AddExpressionProperty("ValueExpression", "Candidate.Entity.Priority");
            pd.Description = "The expression used with the 'Campaign Cycle' selection rule. " +
                "In the expression, use the keyword 'Candidate' to reference an object in the collection of candidates (e.g., Candidate.Entity.Priority).";
            pd.Required = true;
            pd.DisplayName = "Value Expression";

            pd = schema.AddExpressionProperty("FilterExpression", String.Empty);
            pd.Description = "The expression used to filter the list before the 'Campaign Cycle' selection rule is applied. " +
                "In the expression, use the keyword 'Candidate' to reference an object in the collection of candidates (e.g., Candidate.Entity.Priority).";
            pd.Required = false;
            pd.DisplayName = "Filter Expression";
        }

        /// <summary>
        /// Creates a new instance of the selection rule.
        /// </summary>
        public ISelectionRule CreateRule(IPropertyReaders properties)
        {
            return new CampaignCycleRule(properties);
        }

        #endregion
    }

    public class CampaignCycleRule : ISelectionRule
    {
        public CampaignCycleRule(IPropertyReaders properties)
        {
            _valueProperty = properties.GetProperty("ValueExpression");
            _filterProperty = properties.GetProperty("FilterExpression");
        }

        IPropertyReader _valueProperty;
        IPropertyReader _filterProperty;

        enum Direction
        {
            Up,
            Down
        };
        Direction _direction = Direction.Up; // Start by going up

        double _lastValue = Double.NegativeInfinity; // Start with the lowest value possible

        #region ISelectionRule Members

        /// <summary>
        /// Method called when the selection rule is being used to select an item from a collection of candidates.
        /// </summary>
        public IExecutionContext Select(IEnumerable<IExecutionContext> candidates)
        {
            double nextValue = 0;
            switch (_direction)
            {
                case Direction.Up:
                    nextValue = Double.PositiveInfinity;
                    break;
                case Direction.Down:
                    nextValue = Double.NegativeInfinity;
                    break;
            }
            IExecutionContext next = null;

            double reverseValue = 0;
            switch(_direction)
            {
                case Direction.Up:
                    reverseValue = Double.NegativeInfinity;
                    break;
                case Direction.Down:
                    reverseValue = Double.PositiveInfinity;
                    break;
            }
            IExecutionContext reverseNext = null;

            foreach (IExecutionContext candidate in candidates)
            {
                bool bProcessItem = true;
                if (String.IsNullOrEmpty(_filterProperty.GetStringValue(candidate)) == false)
                    bProcessItem = (_filterProperty.GetDoubleValue(candidate) > 0);

                if (bProcessItem)
                {
                    double thisValue = _valueProperty.GetDoubleValue(candidate);

                    switch (_direction)
                    {
                        case Direction.Up:
                            if (thisValue >= _lastValue)
                            {
                                if (thisValue < nextValue)
                                {
                                    nextValue = thisValue;
                                    next = candidate;
                                }
                            }
                            else if (thisValue > reverseValue)
                            {
                                reverseValue = thisValue;
                                reverseNext = candidate;
                            }
                            break;
                        case Direction.Down:
                            if (thisValue <= _lastValue)
                            {
                                if (thisValue > nextValue)
                                {
                                    nextValue = thisValue;
                                    next = candidate;
                                }
                            }
                            else if (thisValue < reverseValue)
                            {
                                reverseValue = thisValue;
                                reverseNext = candidate;
                            }
                            break;
                    }
                }
            }

            if (next == null)
            {
                switch(_direction)
                {
                    case Direction.Up:
                        _direction = Direction.Down; 
                        break;
                    case Direction.Down:
                        _direction = Direction.Up;
                        break;
                }
                _lastValue = reverseValue;
                return reverseNext;
            }

            _lastValue = nextValue;
            return next;
        }

        #endregion
    }
}
