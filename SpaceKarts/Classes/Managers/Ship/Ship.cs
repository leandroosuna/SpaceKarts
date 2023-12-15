using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpaceKarts.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceKarts.Managers
{
    public class Ship
    {
        Model model;
        Texture2D colorTex;
        Texture2D emissiveTex;
        Texture2D normalTex;
        public SoundEffectInstance engineSound;
        public float enginePitch;

        public Vector3 position;
        public Vector3 frontDirection;
        public Vector3 moveDirection;
        public float pitch;
        public float yaw;
        public float roll = 0;
        public float size = 5;
        AudioEmitter emitter;

        public BoundingBox boxCollider;
        SpaceKarts game;
        BasicModelEffect effect;
        public bool isPlayer = false;

        Matrix scale, rotation, translation;
        public Ship(SpaceKarts game, Model model, Texture2D color, Texture2D normal, Texture2D emmisive, SoundEffectInstance engineSound) : 
            this(game, model, color, normal, emmisive, engineSound, new Vector3(0,1.5f,0), 0f, 0f)
        {

        }
        
        public Ship(SpaceKarts game, Model model, Texture2D color, Texture2D normal, Texture2D emmisive,  SoundEffectInstance engineSound, Vector3 position, float pitch, float yaw)
        {
            this.model = model;
            this.colorTex = color;
            this.normalTex = normal;
            this.emissiveTex = emmisive;
            this.engineSound = engineSound;
            this.position = position;
            this.pitch = pitch;
            this.yaw = yaw;
            this.game = game;
            engineSound.IsLooped = true;
            effect = game.basicModelEffect;
            enginePitch = 0;
            emitter = new AudioEmitter();
            emitter.Velocity = Vector3.One;
            boxCollider = new BoundingBox(position - new Vector3(size), position + new Vector3(size));

        }

        public Ship(SpaceKarts game, Model model, Texture2D color, Texture2D normal, Texture2D emmisive, SoundEffectInstance engineSound, Vector3 position, float pitch, float yaw, bool isPlayer)
        {
            this.model = model;
            this.colorTex = color;
            this.normalTex = normal;
            this.emissiveTex = emmisive;
            this.engineSound = engineSound;
            this.position = position;
            this.pitch = pitch;
            this.yaw = yaw;
            this.game = game;
            engineSound.IsLooped = true;
            effect = game.basicModelEffect;
            enginePitch = 0;
            emitter = new AudioEmitter();
            emitter.Velocity = Vector3.One;
            boxCollider = new BoundingBox(position - new Vector3(size), position + new Vector3(size));
            this.isPlayer = isPlayer;

            scale = Matrix.CreateScale(0.005f);
        }
        Vector3 lastPosition = Vector3.Zero;
        public void Update(float deltaTime)
        {
            if (enginePitch > 0f)
            {
                enginePitch -= deltaTime * 0.1f;
                if (enginePitch < 0f)
                    enginePitch = 0f;
            }
            
            //checkKeyState(deltaTime);
            calculateDirectionFromYawPitch();
            
            emitter.Forward = frontDirection;
            emitter.Up = Vector3.Up;
            emitter.Position = position;
            engineSound.Apply3D(game.audioListener, emitter);
            engineSound.Pitch = enginePitch;

            boxCollider.Min = position - Vector3.One * size;
            boxCollider.Max = position + Vector3.One * size;
            

        }

        public void ForceUpdate(float deltaTime, Vector3 pos, Quaternion quat)
        {
            lastPosition = position;
            position = pos;
            moveDirection = Vector3.Normalize(position - lastPosition);
            if (moveDirection.Length() < 0.001f)
                moveDirection = new Vector3(1,0,0);

            rotation = Matrix.CreateFromQuaternion(quat);
            translation = Matrix.CreateTranslation(pos);
            frontDirection = rotation.Forward;

            emitter.Forward = frontDirection;
            emitter.Up = Vector3.Up;
            emitter.Position = position;
            engineSound.Apply3D(game.audioListener, emitter);
            engineSound.Pitch = enginePitch;

            boxCollider.Min = position - Vector3.One * size;
            boxCollider.Max = position + Vector3.One * size;
        }
        void checkKeyState(float deltaTime)
        {
            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Left))
            {
                yaw += deltaTime * 200;
                yaw %= 360;

            }
            if (keyState.IsKeyDown(Keys.Right))
            {
                yaw -= deltaTime * 200;
                if (yaw < 0)
                    yaw += 360;
            }
            if (keyState.IsKeyDown(Keys.Up))
            {
                position -= frontDirection * deltaTime * 15;
                if(engineSound.State == SoundState.Stopped)
                {
                    engineSound.Play();
                }
                enginePitch += deltaTime * 2.4f;
                if (enginePitch > 1.0)
                    enginePitch = 1.0f;
                engineSound.Pitch = enginePitch;
            }
            if (keyState.IsKeyDown(Keys.Down))
            {
                position+= frontDirection * deltaTime * 15;
                enginePitch -= deltaTime * 1.2f;
                if (enginePitch < 0)
                    enginePitch = 0f;
                engineSound.Pitch = enginePitch;
            }
           
            //else if (keyState.IsKeyDown(Keys.J))
            //{
            //    if(!keysDown.Contains(Keys.J))
            //    {
            //        keysDown.Add(Keys.J);
            //        if (engineInstance.State == SoundState.Playing)
            //            engineInstance.Stop();
            //        else
            //            engineInstance.Play();
            //    }
        }
        void calculateDirectionFromYawPitch()
        {
            var correctedYaw = -MathHelper.ToRadians(yaw) - MathHelper.PiOver2;

            Vector3 tempFront;

            tempFront.X = MathF.Cos(correctedYaw) * MathF.Cos(MathHelper.ToRadians(pitch));
            tempFront.Y = MathF.Sin(MathHelper.ToRadians(correctedYaw));
            tempFront.Z = MathF.Sin(correctedYaw) * MathF.Cos(MathHelper.ToRadians(pitch));

            frontDirection = Vector3.Normalize(tempFront);
        }
        
        public void Draw(float deltaTime)
        {
             
            effect.SetTech("color_tex_normal_emissive");

            effect.SetKA(0.2f);
            effect.SetKD(0.7f);
            effect.SetKS(0.7f);
            effect.SetShininess(30f);

            effect.SetColorTexture(colorTex);
            effect.SetEmissiveTexture(emissiveTex);
            effect.SetNormalTexture(normalTex);
            foreach (var mesh in model.Meshes)
            {
                //var rotation = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(yaw), MathHelper.ToRadians(pitch), MathHelper.ToRadians(roll));
                var world = mesh.ParentBone.Transform * scale * rotation * translation;
                effect.SetWorld(world);
                effect.SetInverseTransposeWorld(Matrix.Invert(Matrix.Transpose(world)));
                
                mesh.Draw();
            }
        }

    }
}
