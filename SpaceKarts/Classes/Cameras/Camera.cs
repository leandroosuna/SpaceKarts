using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SharpDX.XAudio2;
using SpaceKarts.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceKarts.Cameras
{
    public class Camera
    {
        public Vector3 position;
        public Vector3 frontDirection;
        public Vector3 rightDirection;
        public Vector3 upDirection;

        public Matrix view, projection;
        public Matrix viewProjection;

        public float fieldOfView;
        public float aspectRatio;
        public float nearPlaneDistance;
        public float farPlaneDistance;
        public bool isFree;

        public float yaw;
        public float pitch;
        bool mouseLocked = true;
        Vector2 mouseDelta;
        float mouseSensitivity = .15f;
        float mouseSensAdapt = .06f;
        System.Drawing.Point center;

        public float moveSpeed = 5f;
        bool ignoreFirstFrame = true;

        public BoundingFrustum frustum;

        SpaceKarts game;
        public Camera(float aspectRatio, Point screenCenter)
        {
            game = SpaceKarts.getInstance();
            frustum = new BoundingFrustum(Matrix.Identity);
            fieldOfView = MathHelper.ToRadians(100);
            this.aspectRatio = aspectRatio;
            position = new Vector3(-2, 2, 2);
            nearPlaneDistance = 1; 
            farPlaneDistance = 1000;
            yaw = 310;
            pitch = -36;
            center = new System.Drawing.Point(screenCenter.X, screenCenter.Y);
            isFree = true;
            UpdateCameraVectors();
            CalculateView();
            CalculateProjection();
        }
        public void UpdatePosition(Vector3 position)
        {
            this.position = position;
        }
        public void Update(float deltaTime)
        {
            if(isFree)
            {
                var inputManager = game.currentInputManager;
                mouseDelta = inputManager.mouseDelta;
                var ship = ShipManager.shipList[0];

                position = inputManager.playerPosition;

                yaw += mouseDelta.X;
                if (yaw < 0)
                    yaw += 360;
                yaw %= 360;

                pitch -= mouseDelta.Y;

                if (pitch > 89.0f)
                    pitch = 89.0f;
                else if (pitch < -89.0f)
                    pitch = -89.0f;

                UpdateCameraVectors();
            }
            else
            {
                var car = game.Simulation.Bodies.GetBodyReference(game.playerController.Car.Body);
                var pos = SpaceKarts.NumV3ToV3(car.Pose.Position);
                var numfront = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(0, 0, 1), car.Pose.Orientation);
                var carFront = SpaceKarts.NumV3ToV3(numfront);

                position = pos - carFront * 5 + new Vector3(0, 5, 0);
                frontDirection = Vector3.Normalize((pos + carFront * 2) - position);

                rightDirection = Vector3.Normalize(Vector3.Cross(frontDirection, Vector3.Up));
                upDirection = Vector3.Normalize(Vector3.Cross(rightDirection, frontDirection));
            }
            CalculateView();
            frustum.Matrix = view * projection;
        }
        void UpdateCameraVectors()
        {
            Vector3 tempFront;

            tempFront.X = MathF.Cos(MathHelper.ToRadians(yaw)) * MathF.Cos(MathHelper.ToRadians(pitch));
            tempFront.Y = MathF.Sin(MathHelper.ToRadians(pitch));
            tempFront.Z = MathF.Sin(MathHelper.ToRadians(yaw)) * MathF.Cos(MathHelper.ToRadians(pitch));

            frontDirection = Vector3.Normalize(tempFront);

            rightDirection = Vector3.Normalize(Vector3.Cross(frontDirection, Vector3.Up));
            upDirection = Vector3.Normalize(Vector3.Cross(rightDirection, frontDirection));
        }
        void CalculateView()
        {
            view = Matrix.CreateLookAt(position, position + frontDirection, upDirection);
        }
        void CalculateProjection()
        {
            projection = Matrix.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance);
        }
        public bool frustumContains(BoundingSphere collider)
        {
            return !frustum.Contains(collider).Equals(ContainmentType.Disjoint);
        }
        public bool frustumContains(BoundingBox collider)
        {
            return !frustum.Contains(collider).Equals(ContainmentType.Disjoint);
        }
        public bool frustumContains(Vector3 point)
        {
            return !frustum.Contains(point).Equals(ContainmentType.Disjoint);
        }

        public void ResetToCenter()
        {
            yaw = 310;
            pitch = -36;
            UpdateCameraVectors();
            CalculateView();

        }
        public void SetFree(bool free)
        {
            isFree = free;
            if(isFree)
            {
                game.currentInputManager.playerPosition = position;
                yaw = MathHelper.ToDegrees((float)Math.Atan2(frontDirection.Y, frontDirection.X));
                if (yaw < 0)
                    yaw += 360;
                else if (yaw > 360)
                    yaw -= 360;
                pitch = 0f;
                game.currentInputManager.mouseDelta = Vector2.Zero; 
            }
        }

    }
}
