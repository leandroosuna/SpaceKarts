using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SpaceKarts.Cameras;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;
using System.IO;

namespace SpaceKarts.Managers
{
    public abstract class InputManager
    {
        public static List<Key> keysDown = new List<Key>();
        public static KeyboardState keyState; 
        public static MouseState mouseState;
        public static bool MB1Down = false;
        public static bool MB2Down = false;
        public static bool MB3Down = false;

        public Camera camera; 
        public Point screenCenter;
        public Vector2 mouseDelta;

        public Vector3 playerPosition = new Vector3(-6,3.5f,0);
        public SpaceKarts game;

        public float mouseSensitivity = .137f;
        public float mouseSensAdapt = .09f;
        public float moveSpeed = 15f;
        public bool spinning = false;
        public bool mouseLocked = true;
        
        public float crosshairThickness = 1f;
        public float crosshairOffset = 2f;
        public float crosshairLenght = 10f;

        public bool speeding = false;

        System.Drawing.Point center;
        Vector2 delta;
        Vector2 mousePosition;

        public static IConfigurationRoot CFG;
        public static KeyMappings keyMappings;

        public InputManager()
        {
            game = SpaceKarts.getInstance();
            camera = game.camera;
            var windowPos = game.Window.Position;
            screenCenter = game.screenCenter + windowPos;
            center = new System.Drawing.Point(screenCenter.X , screenCenter.Y ) ;

            
        }
        public static void Init()
        {
            var fileCfg = "CFG/input-settings.json";
            var jsonKeys = JsonKeys.LoadFromJson(fileCfg);
            keyMappings = new KeyMappings(jsonKeys);
            keyMappings.Debug0 = new KeyboardKey(Keys.D0);
            keyMappings.Debug1 = new KeyboardKey(Keys.D1);
            keyMappings.Debug2 = new KeyboardKey(Keys.D2);
            keyMappings.Debug3 = new KeyboardKey(Keys.D3);
            keyMappings.Debug9 = new KeyboardKey(Keys.D9);
        }

        public abstract void ProcessInput(float deltaTime);
        public void Update(float deltaTime)
        {
            keyState = Keyboard.GetState();
            mouseState = Mouse.GetState();
            if(mouseState.LeftButton == ButtonState.Released)
                MB1Down = false;
            if(mouseState.RightButton == ButtonState.Released)
                MB2Down = false;
            if (mouseState.MiddleButton == ButtonState.Released)
                MB3Down = false;

            keysDown.RemoveAll(key => !key.IsDown());

            updateMousePositionDelta();
            ProcessInput(deltaTime);
            
        }
        public void updateMousePositionDelta()
        {
            mousePosition.X = System.Windows.Forms.Cursor.Position.X;
            mousePosition.Y = System.Windows.Forms.Cursor.Position.Y;

            delta.X = mousePosition.X - center.X;
            delta.Y = mousePosition.Y - center.Y;

            mouseDelta = delta * mouseSensitivity * mouseSensAdapt;
            if (mouseLocked)
                System.Windows.Forms.Cursor.Position = center;
        }
        
        
        public enum MouseButton
        {
            Left,
            Right,
            Middle
        }

        public class MouseKey : Key
        {
            MouseButton button;
            public MouseKey(MouseButton button)
            {
                this.button = button;
            }

            public override bool IsDown()
            {
                if (button == MouseButton.Left)
                    return mouseState.LeftButton == ButtonState.Pressed;
                else if (button == MouseButton.Right)
                    return mouseState.RightButton == ButtonState.Pressed;
                else if (button == MouseButton.Middle)
                    return mouseState.MiddleButton == ButtonState.Pressed;
                return false;
            }
        }
        public class KeyboardKey: Key
        {
            Keys key;
            public KeyboardKey(Keys key)
            {
                this.key = key;
            }
            public override bool IsDown()
            {
                return keyState.IsKeyDown(key);
            }
        }

        public class ScrollWheel : Key
        {
            static int lastValue;
            static int value;
            static int diff;
            
            public bool direction;
            public int distance;
            public ScrollWheel(bool up)
            {
                lastValue = Mouse.GetState().ScrollWheelValue;
            }

            public override bool IsDown()
            {
                value = mouseState.ScrollWheelValue;
                diff = lastValue - value;
                distance = Math.Abs(diff);
                direction = diff > 0;

                lastValue = value;

                return distance > 0;
            }
        }
        public abstract class Key
        {
            public abstract bool IsDown();
        }
        public class JsonKeys
        {
            public Keys KeyEnter { get; set; }
            public Keys KeyEscape { get; set; }
            public Keys KeyAccelerate { get; set; }
            public Keys KeyTurnLeft { get; set; }
            public Keys KeyBrake { get; set; }
            public Keys KeyTurnRight { get; set; }
            public Keys KeyBoost { get; set; }
            public Keys KeyJump { get; set; }
            public Keys KeyFire { get; set; }
            public Keys KeyShield { get; set; }

            public Keys KeyAltAccelerate { get; set; }
            public Keys KeyAltTurnLeft { get; set; }
            public Keys KeyAltBrake { get; set; }
            public Keys KeyAltTurnRight { get; set; }
            public Keys KeyAltBoost { get; set; }
            public Keys KeyAltJump { get; set; }
            public Keys KeyAltFire { get; set; }
            public Keys KeyAltShield { get; set; }

            public static JsonKeys LoadFromJson(string filePath)
            {
                // Read JSON file content
                string jsonContent = File.ReadAllText(filePath);

                // Deserialize JSON to KeyMappings object
                JsonKeys jsonKeys = JsonConvert.DeserializeObject<JsonKeys>(jsonContent);


                return jsonKeys;
            }
            public void SaveToJson(string filePath)
            {
                string jsonContent = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, jsonContent);
            }

            public void UpdateKey(string propertyName, Keys newKey)
            {
                // Use reflection to find the property by name
                var property = typeof(KeyMappings).GetProperty(propertyName);

                if (property != null && property.PropertyType == typeof(Keys))
                {
                    // Set the new key value
                    property.SetValue(this, newKey);
                }
                else
                {
                    throw new ArgumentException("Invalid property name or type.");
                }
            }
            // Modify key mappings (e.g., user presses a key in the game)
            //keyMappings.UpdateKey("KeyAccelerate", Keys.Space);

            // Save the updated key mappings to JSON file
            //keyMappings.SaveToJson("path/to/your/keymappings.json");
        }
        public class KeyMappings
        {
            public Key Enter;
            public Key Escape;

            public Key Accelerate;
            public Key Brake;
            public Key TurnLeft;
            public Key TurnRight;
            public Key Boost;
            public Key Jump;
            public Key Fire;
            public Key Shield;

            public Key AccelerateAlt;
            public Key BrakeAlt;
            public Key TurnLeftAlt;
            public Key TurnRightAlt;
            public Key BoostAlt;
            public Key JumpAlt;
            public Key FireAlt;
            public Key ShieldAlt;

            public List<Key> MappedKeys;

            public Key Debug1,Debug2,Debug3,Debug0, Debug9;

            public Key convertKey(Keys key)
            {
                //Keys.F20 = MB1
                //Keys.F21 = MB2
                //Keys.F22 = MB3
                //Keys.F23 = Scroll Down
                //Keys.F24 = Scroll Up

                if (key == Keys.F20)
                {
                    return new MouseKey(MouseButton.Left);    
                }
                if (key == Keys.F21)
                {
                    return new MouseKey(MouseButton.Right);
                }
                if (key == Keys.F22)
                {
                    return new MouseKey(MouseButton.Middle);
                }
                if (key == Keys.F23)
                {
                    return new ScrollWheel(true);
                }
                if (key == Keys.F24)
                {
                    return new ScrollWheel(false);
                }
                //if(key == Keys.None)
                //{
                    
                //}
                return new KeyboardKey(key);
            }
            public KeyMappings(JsonKeys keys)
            {
                Enter = convertKey(keys.KeyEnter);
                Escape = convertKey(keys.KeyEscape);

                Accelerate = convertKey(keys.KeyAccelerate);
                Brake = convertKey(keys.KeyBrake);
                TurnLeft = convertKey(keys.KeyTurnLeft);
                TurnRight = convertKey(keys.KeyTurnRight);
                Boost = convertKey(keys.KeyBoost);
                Jump = convertKey(keys.KeyJump);
                Fire = convertKey(keys.KeyFire);
                Shield = convertKey(keys.KeyShield);

                AccelerateAlt = convertKey(keys.KeyAltAccelerate);
                BrakeAlt = convertKey(keys.KeyAltBrake);
                TurnLeftAlt = convertKey(keys.KeyAltTurnLeft);
                TurnRightAlt = convertKey(keys.KeyAltTurnRight);
                BoostAlt = convertKey(keys.KeyAltBoost);
                JumpAlt = convertKey(keys.KeyAltJump);
                FireAlt = convertKey(keys.KeyAltFire);
                ShieldAlt = convertKey(keys.KeyAltShield);
                MappedKeys = new List<Key>();
                MappedKeys.AddRange(new List<Key>() {
                    Enter, Escape,
                    Accelerate, Brake, TurnLeft, TurnRight, Boost, Jump, Fire, Shield,
                    AccelerateAlt, BrakeAlt, TurnLeftAlt, TurnRightAlt, BoostAlt, JumpAlt, FireAlt, ShieldAlt});

        }
            
        }
    }
}
