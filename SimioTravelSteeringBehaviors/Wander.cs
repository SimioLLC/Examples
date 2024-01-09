using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SimioAPI;
using SimioAPI.Extensions;

namespace SimioTravelSteeringBehaviors
{
#if DEBUG
    public class WanderDefinition : ITravelSteeringBehaviorDefinition
    {
        #region ITravelSteeringBehaviorDefinition Members

        /// <summary>
        /// The name of the travel steering behavior. May contain any characters.
        /// </summary>
        public string Name
        {
            get { return "MyWander"; }
        }

        /// <summary>
        /// Description text for the travel steering behavior.
        /// </summary>
        public string Description
        {
            get { return "Randomly wander around the world space without stopping"; }
        }

        /// <summary>
        /// Guid which uniquely identifies the travel steering behavior.
        /// </summary>
        public Guid UniqueID
        {
            get { return MY_ID; }
        }
        static readonly Guid MY_ID = new Guid("{B14224D1-1B88-418F-B529-B8D8FD513AD2}"); // Jan2024/danH

        /// <summary>
        /// Defines the property schema for the travel steering behavior.
        /// </summary>
        public void DefineSchema(IPropertyDefinitions propertyDefinitions)
        {
            var wanderStrength = propertyDefinitions.AddExpressionProperty("WanderStrength", "1.0") as IExpressionPropertyDefinition;
            wanderStrength.Description = "A value indicating the magnitude of allowed direction changes.";
            wanderStrength.Required = true;
            wanderStrength.DisplayName = "Wander Strength";

            var wanderRate = propertyDefinitions.AddExpressionProperty("WanderRate", "0.5");
            wanderRate.Description = "A value indicating the magnitude of the random displacement of the movement every update.";
            wanderRate.Required = true;
            wanderRate.DisplayName = "Wander Rate";

            var updateTimeInterval = propertyDefinitions.AddExpressionProperty("UpdateTimeInterval", "0.5") as IExpressionPropertyDefinition;
            updateTimeInterval.UnitType = SimioUnitType.Time;
            (updateTimeInterval.GetDefaultUnit(propertyDefinitions) as ITimeUnit).Time = TimeUnit.Seconds;
            updateTimeInterval.Description = "How often does the direction change.";
            updateTimeInterval.Required = true;
            updateTimeInterval.DisplayName = "Update Time Interval";
        }

        /// <summary>
        /// Creates a new instance of the travel steering behavior.
        /// </summary>
        public ITravelSteeringBehavior CreateTravelSteeringBehavior(IPropertyReaders properties, ITravelSteeringBehaviorCreationContext executionContext)
        {
            return new Wander(properties);
        }

        #endregion
    }

    public class Wander : ITravelSteeringBehavior
    {
        readonly IExpressionPropertyReader _wanderStrength;
        readonly IExpressionPropertyReader _wanderRate;
        readonly IExpressionPropertyReader _updateTimeInterval;
        public Wander(IPropertyReaders properties)
        {
            _wanderStrength = properties.GetProperty("WanderStrength") as IExpressionPropertyReader;
            _wanderRate = properties.GetProperty("WanderRate") as IExpressionPropertyReader;
            _updateTimeInterval = properties.GetProperty("UpdateTimeInterval") as IExpressionPropertyReader;
        }

        #region ITravelSteeringBehavior Members

        BaseSteeringBehaviors.WanderInfo _wanderInfo;
        public void Steer(ITravelSteeringBehaviorContext context)
        {
            DoSteer(context);
        }

        const double TIME_TO_CHANGE_DIRECTION = 0.5 / 3600.0; // 0.5 seconds

        private void DoSteer(ITravelSteeringBehaviorContext context)
        {
            //
            // Don't try and steer something with no forward velocity
            //
            if (context.EntityData.Velocity <= 0.0)
            {
                context.MovementFinished();
                return;
            }

            var wanderStrength = 0.0;
            if (PropertyUtilities.TryGetPositiveDoubleValue(_wanderStrength, context, out wanderStrength) == false)
                return;

            var wanderRate = 0.0;
            if (PropertyUtilities.TryGetPositiveDoubleValue(_wanderRate, context, out wanderRate) == false)
                return;

            var timeInterval = 0.0;
            if (PropertyUtilities.TryGetPositiveDoubleValue(_updateTimeInterval, context, out timeInterval) == false)
                return;

            var entityDirectionalVelocity = context.EntityData.UnitDirection * context.EntityData.Velocity;

            _wanderInfo = BaseSteeringBehaviors.Wander(_wanderInfo, entityDirectionalVelocity, wanderStrength, wanderRate, context.Random);

            // Steering direction is in m / h right now. We need to change that into acceleration however
            //  because we are changing from one directional velocity to another over time
            var acceleration = _wanderInfo.SteeringDirection / TIME_TO_CHANGE_DIRECTION;

            context.SetPreferredMovement(new TravelSteeringMovement()
            {
                Direction = context.EntityData.UnitDirection,
                Velocity = context.EntityData.Velocity,
                OrientInTheSameDirection = true,
                Acceleration = acceleration.ComputeLength(),
                AccelerationDuration = TIME_TO_CHANGE_DIRECTION,
                AccelerationDirection = acceleration
            });

            context.Calendar.ScheduleEvent(context.Calendar.TimeNow + timeInterval, null, _ => DoSteer(context));
        }

        public void OnTravelCancelling(IExecutionContext context)
        {
        }

        public void OnTravelSuspended()
        {
        }

        public void OnTravelResumed()
        {
        }

        #endregion
    }
#endif
}
