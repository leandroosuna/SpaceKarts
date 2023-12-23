using System;
using System.Diagnostics;
using System.Numerics;
using BepuPhysics;

namespace SpaceKarts.Physics
{
    public struct SimpleCarController
    {
        public SimpleCar Car;

        private float steeringAngle;

        public readonly float SteeringAngle { get { return steeringAngle; } }

        public float SteeringSpeed;
        public float MaximumSteeringAngle;

        public float ForwardSpeed;
        public float ForwardForce;
        public float ZoomMultiplier;
        public float BackwardSpeed;
        public float BackwardForce;
        public float IdleForce;
        public float BrakeForce;
        public float WheelBaseLength;
        public float WheelBaseWidth;
        /// <summary>
        /// Fraction of Ackerman steering angle to apply to wheels. Using 0 does not modify the the steering angle at all, leaving the wheels pointed exactly along the steering angle, while 1 uses the full Ackerman angle.
        /// </summary>
        public float AckermanSteering;

        //Track the previous state to force wakeups if the constraint targets have changed.
        private float previousTargetSpeed;
        private float previousTargetForce;

        public SimpleCarController(SimpleCar car,
            float forwardSpeed, float forwardForce, float zoomMultiplier, float backwardSpeed, float backwardForce, float idleForce, float brakeForce,
            float steeringSpeed, float maximumSteeringAngle, float wheelBaseLength, float wheelBaseWidth, float ackermanSteering)
        {
            Car = car;
            ForwardSpeed = forwardSpeed;
            ForwardForce = forwardForce;
            ZoomMultiplier = zoomMultiplier;
            BackwardSpeed = backwardSpeed;
            BackwardForce = backwardForce;
            IdleForce = idleForce;
            BrakeForce = brakeForce;
            SteeringSpeed = steeringSpeed;
            MaximumSteeringAngle = maximumSteeringAngle;
            WheelBaseLength = wheelBaseLength;
            WheelBaseWidth = wheelBaseWidth;
            AckermanSteering = ackermanSteering;

            steeringAngle = 0;
            previousTargetForce = 0;
            previousTargetSpeed = 0;
        }

        public bool Update(Simulation simulation, float dt, float targetSteeringAngle, float targetSpeedFraction, bool boost, bool brake)
        {
            var steeringAngleDifference = targetSteeringAngle - steeringAngle;
            var maximumChange = SteeringSpeed * dt;
            var steeringAngleChange = MathF.Min(maximumChange, MathF.Max(-maximumChange, steeringAngleDifference));
            var previousSteeringAngle = steeringAngle;
            
            steeringAngle = MathF.Min(MaximumSteeringAngle, MathF.Max(-MaximumSteeringAngle, steeringAngle + steeringAngleChange));
            float leftSteeringAngle = 0;
            float rightSteeringAngle = 0;
            
            var refe = simulation.Bodies.GetBodyReference(Car.Body);
            var velocity = refe.Velocity.Linear;
            var frontDirection = SpaceKarts.QuaternionToFrontDirection(refe.Pose.Orientation);

            var dot = Vector3.Dot(velocity, frontDirection);
            var changeDirection =  (dot < 0 && targetSpeedFraction < 0) || (dot > 0 && targetSpeedFraction > 0);

            //steeringAngle *= 
            if (steeringAngle != previousSteeringAngle)
            {

                float steeringAngleAbs = MathF.Abs(steeringAngle);

                if (AckermanSteering > 0 && steeringAngleAbs > 1e-6)
                {
                    float turnRadius = MathF.Abs(WheelBaseLength * MathF.Tan(MathF.PI * 0.5f - steeringAngleAbs));
                    var wheelBaseHalfWidth = WheelBaseWidth * 0.5f;
                    if (steeringAngle > 0)
                    {
                        rightSteeringAngle = MathF.Atan(WheelBaseLength / (turnRadius - wheelBaseHalfWidth));
                        rightSteeringAngle = steeringAngle + (rightSteeringAngle - steeringAngleAbs) * AckermanSteering;
                        
                        leftSteeringAngle = MathF.Atan(WheelBaseLength / (turnRadius + wheelBaseHalfWidth));
                        leftSteeringAngle = steeringAngle + (leftSteeringAngle - steeringAngleAbs) * AckermanSteering;
                    }
                    else
                    {
                        rightSteeringAngle = MathF.Atan(WheelBaseLength / (turnRadius + wheelBaseHalfWidth));
                        rightSteeringAngle = steeringAngle - (rightSteeringAngle - steeringAngleAbs) * AckermanSteering;

                        leftSteeringAngle = MathF.Atan(WheelBaseLength / (turnRadius - wheelBaseHalfWidth));
                        leftSteeringAngle = steeringAngle - (leftSteeringAngle - steeringAngleAbs) * AckermanSteering;
                    }
                }
                else
                {
                    leftSteeringAngle = steeringAngle;
                    rightSteeringAngle = steeringAngle;
                }

                //By guarding the constraint modifications behind a state test, we avoid waking up the car every single frame.
                //(We could have also used the ApplyDescriptionWithoutWaking function and then explicitly woke the car up when changes occur.)
                Car.Steer(simulation, Car.FrontLeftWheel, leftSteeringAngle);
                Car.Steer(simulation, Car.FrontRightWheel, rightSteeringAngle);
            }
            float newTargetSpeed, newTargetForce;
            bool allWheels;


            var braking = brake || changeDirection;
            if (braking)
            {
                newTargetSpeed = 0;
                newTargetForce = BrakeForce;

            }
            else if (targetSpeedFraction > 0)
            {
                newTargetForce = ForwardForce;
                newTargetSpeed = targetSpeedFraction * ForwardSpeed;
            }
            else if (targetSpeedFraction < 0)
            {
                newTargetForce = BackwardForce;
                newTargetSpeed = targetSpeedFraction * BackwardSpeed;
            }
            else
            {
                newTargetForce = IdleForce;
                newTargetSpeed = 0;
            }
            
            if (previousTargetSpeed != newTargetSpeed || previousTargetForce != newTargetForce)
            {
                previousTargetSpeed = newTargetSpeed;
                previousTargetForce = newTargetForce;

                var targetSpeedL = newTargetSpeed ;
                var targetSpeedR = newTargetSpeed ;

                
                
                Car.SetSpeed(simulation, Car.FrontLeftWheel, targetSpeedL, braking? newTargetForce * 1.5f : newTargetForce * .2f);
                Car.SetSpeed(simulation, Car.FrontRightWheel, targetSpeedR, braking? newTargetForce * 1.5f : newTargetForce * .2f);
                
                Car.SetSpeed(simulation, Car.BackLeftWheel, targetSpeedL, newTargetForce );
                Car.SetSpeed(simulation, Car.BackRightWheel, targetSpeedR, newTargetForce );
                
            }
            if(boost)
            {
                var impulse = Vector3.Normalize(refe.Velocity.Linear) * 2f;

                refe.ApplyImpulse(impulse, Vector3.Zero);
            }
            return braking;
        }
    }
}