using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SpaceKarts.Managers
{
    public class Input_MainMenu : InputManager
    {
        public Input_MainMenu() : base()
        {
            mouseLocked = false;
        }
        

        public new void Update(float deltaTime)
        {
            
            base.Update(deltaTime);
        }

        public override void ProcessInput(float deltaTime)
        {
            if (keyMappings.Escape.IsDown())
            {
                if (!keysDown.Contains(keyMappings.Escape))
                    game.Exit();
            }
            if (keyMappings.Enter.IsDown()) { 
                keysDown.Add(keyMappings.Enter);
                game.SwitchGameState(State.RUN);
            }
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                if(!MB1Down)
                {
                    MB1Down = true;
                    //game.SwitchGameState(State.RUN);
                }
            }
            if (mouseState.RightButton == ButtonState.Pressed)
            {
                
            }


        }

    }
}
