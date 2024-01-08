using SimioAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimioTravelSteeringBehaviors
{
    public static class PropertyUtilities
    {
        public static bool TryGetPositiveDoubleValue(IExpressionPropertyReader expressionReader, IExecutionContext context, out double theValue)
        {
            theValue = Double.NaN;
            var valueObj = expressionReader.GetExpressionValue(context);
            if (!(valueObj is double))
            {
                context.ExecutionInformation.ReportError(expressionReader as IPropertyReader, "Did not return a double value.");
                return false;
            }

            theValue = (double)valueObj;

            if (theValue <= 0.0 || Double.IsNaN(theValue) || Double.IsInfinity(theValue))
            {
                context.ExecutionInformation.ReportError(expressionReader as IPropertyReader, "Invalid value. Value must be a real number greater than zero.");
                return false;
            }

            return true;
        }
    }
}
