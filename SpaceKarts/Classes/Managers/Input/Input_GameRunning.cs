using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SpaceKarts.Managers
{
    public class Input_GameRunning : InputManager
    {
        public Input_GameRunning() : base()
        {
            mouseLocked = true;
        }
        

        public new void Update(float deltaTime)
        {
            base.Update(deltaTime);
        }

        bool capturingAngle = false;
        float startAngle;
        
        public override void ProcessInput(float deltaTime)
        {
            var pos = game.camera.position;
            var camera = game.camera;
            var keyState = Keyboard.GetState();
            
            if (keyMappings.Escape.IsDown())
            {
                if (!keysDown.Contains(keyMappings.Escape))
                {
                    keysDown.Add(keyMappings.Escape);
                    
                    game.SwitchGameState(State.MAIN_MENU);
                }
            }
            
            var speed = moveSpeed;
            if (keyState.IsKeyDown(Keys.LeftShift))
                speed *= 3f;

            if(keyMappings.Debug1.IsDown())
            {
                if(!keysDown.Contains(keyMappings.Debug1))
                {
                    keysDown.Add(keyMappings.Debug1);

                    game.bloomEnabled = !game.bloomEnabled;
                }
            }
            if (keyMappings.Debug2.IsDown())
            {
                if (!keysDown.Contains(keyMappings.Debug2))
                {
                    keysDown.Add(keyMappings.Debug2);

                    game.motionBlurEnabled = !game.motionBlurEnabled;
                }
            }
            if (keyMappings.Debug3.IsDown())
            {
                if (!keysDown.Contains(keyMappings.Debug3))
                {
                    keysDown.Add(keyMappings.Debug3);

                    if (game.motionBlurIntensity < 30)
                        game.motionBlurIntensity++;
                    else
                        game.motionBlurIntensity = 5;
                }
            }
            if (keyMappings.Debug0.IsDown())
            {
                if (!keysDown.Contains(keyMappings.Debug0))
                {
                    keysDown.Add(keyMappings.Debug0);

                    camera.isFree = !camera.isFree;
                }
            }
            if (keyMappings.Debug9.IsDown())
            {
                if (!keysDown.Contains(keyMappings.Debug9))
                {
                    keysDown.Add(keyMappings.Debug9);

                    game.Graphics.SynchronizeWithVerticalRetrace = !game.Graphics.SynchronizeWithVerticalRetrace;
                    game.Graphics.ApplyChanges();
                }
            }
            if (camera.isFree)
            {
                if (keyMappings.Accelerate.IsDown() )
                {
                    
                }
            }
            var frontNoY = new Vector3(game.camera.frontDirection.X, 0, game.camera.frontDirection.Z);
            var rightNoY = new Vector3(game.camera.rightDirection.X, 0, game.camera.rightDirection.Z);

            var dirChanged = false;
            var dir = Vector3.Zero;

            if (keyMappings.Accelerate.IsDown())
            {
                dir += frontNoY;
                dirChanged = true;
            }
            if (keyMappings.Brake.IsDown())
            {
                dir -= frontNoY;
                dirChanged = true;
            }
            if (keyMappings.TurnLeft.IsDown())
            {
                dir -= rightNoY;
                dirChanged = true;
            }
            if (keyMappings.TurnRight.IsDown())
            {
                dir += rightNoY;
                dirChanged = true;
            }
            if(keyMappings.Jump.IsDown())
            {
                dir += Vector3.Up;
                dirChanged = true;
            }
            if (keyState.IsKeyDown(Keys.LeftControl))
            {
                dir += Vector3.Down;
                dirChanged = true;
            }

            if (dirChanged)
            {
                if (Vector3.Distance(dir, Vector3.Zero) > 0)
                {
                    dir = Vector3.Normalize(dir);

                    pos += dir * speed * deltaTime;

                    playerPosition = pos;
                }
            }
            
            
        }
        
    }
}
