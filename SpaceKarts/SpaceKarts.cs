using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using SpaceKarts.Cameras;
using SpaceKarts.Managers;
using SpaceKarts.Effects;

namespace SpaceKarts
{
    public class SpaceKarts : Game
    {
        GraphicsDeviceManager Graphics;
        SpriteBatch SpriteBatch;
        SpriteFont Font;
        IConfigurationRoot CFG;
        
        public const string ContentFolderEffects = "Effects/";
        public const string ContentFolder3D = "Models/";
        public const string ContentFolderFonts = "Fonts/";
        public const string ContentFolderTextures = "Tex/";
        public const string ContentFolderAudio = "Audio/";

        public Point screenCenter;
        public DeferredEffect deferredEffect;
        public BasicModelEffect basicModelEffect;
        public int screenWidth;
        public int screenHeight;

        RenderTarget2D colorTarget;
        RenderTarget2D normalTarget;
        RenderTarget2D positionTarget;
        RenderTarget2D lightTarget;
        
        RenderTarget2D prevPositionTarget;
        RenderTarget2D bloomFilterTarget;
        RenderTarget2D blurHtarget;
        RenderTarget2D blurVtarget;
    

        Model sphere, cube, plane, track, lightSphere, cone, lightCone;
        Texture2D[] trackTex;
        
        public Effect effect;
        public Camera camera;
        public FullScreenQuad fullScreenQuad;
        public AudioListener audioListener;

        public static SpaceKarts instance;

        public LightsManager lightsManager;
        public InputManager currentInputManager;
        InputManager inputRun;
        InputManager inputMainMenu;

        public State gameState;

        public SpaceKarts(IConfigurationRoot appCfg)
        {
            
            CFG = appCfg;
            Graphics = new GraphicsDeviceManager(this);
            Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";

            var w = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            var h = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            if (bool.Parse(CFG["AutoAdjust"]))
            {
                Graphics.IsFullScreen = true;
                Graphics.PreferredBackBufferWidth = w;
                Graphics.PreferredBackBufferHeight = h;
            }
            else 
            {
                Graphics.IsFullScreen = bool.Parse(CFG["Fullscreen"]);
                Window.IsBorderless = bool.Parse(CFG["Borderless"]);
                
                Graphics.PreferredBackBufferWidth = int.Parse(CFG["ScreenWidth"]);
                Graphics.PreferredBackBufferHeight = int.Parse(CFG["ScreenHeight"]);
                Window.Position = new Point(w / 2 - Graphics.PreferredBackBufferWidth/2, h / 2 - Graphics.PreferredBackBufferHeight/2);
            }
            Graphics.ApplyChanges();
            // vsync & frame limit
            IsFixedTimeStep = bool.Parse(CFG["FPSLimit"]);
            Graphics.SynchronizeWithVerticalRetrace = bool.Parse(CFG["Vsync"]);
            var fpsMax = int.Parse(CFG["FPSMax"]);
            TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / fpsMax);

            Graphics.ApplyChanges();
            IsMouseVisible = bool.Parse(CFG["MouseVisible"]);

            
        }
        
        protected override void Initialize()
        {
            var viewport = GraphicsDevice.Viewport;
            screenCenter = new Point(viewport.Width / 2, viewport.Height / 2);
            if (!Graphics.IsFullScreen)
                screenCenter += Window.Position;
            instance = this;
            inputMainMenu = new Input_MainMenu();
            inputRun = new Input_GameRunning();

            SwitchGameState(State.MAIN_MENU);

            camera = new Camera(viewport.AspectRatio, screenCenter);
            InputManager.Init();

            audioListener = new AudioListener();
            fullScreenQuad = new FullScreenQuad(GraphicsDevice);
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            

            Font = Content.Load<SpriteFont>(ContentFolderFonts + "tahoma/15");
            deferredEffect = new DeferredEffect("deferred");
            basicModelEffect = new BasicModelEffect("basic");

            sphere = Content.Load<Model>(ContentFolder3D + "Basic/sphere");
            cube = Content.Load<Model>(ContentFolder3D + "Basic/cube");
            plane = Content.Load<Model>(ContentFolder3D + "Basic/plane");
            lightSphere = Content.Load<Model>(ContentFolder3D + "Basic/lightSphere");
            lightCone = Content.Load<Model>(ContentFolder3D + "Basic/cone");


            AssignEffect(sphere, basicModelEffect.effect);
            AssignEffect(cube, basicModelEffect.effect);
            AssignEffect(plane, basicModelEffect.effect);

            AssignEffect(lightSphere, deferredEffect.effect);
            AssignEffect(lightCone, deferredEffect.effect);

            LightVolume.Init(sphere, lightSphere, lightCone, cube);
            //effect = Content.Load<Effect>(ContentFolderEffects + "basic");
            track = Content.Load<Model>(ContentFolder3D + "Track/track");
            trackTex = new Texture2D[]{
                Content.Load<Texture2D>(ContentFolder3D + "Track/Textures/Grass01"),
                Content.Load<Texture2D>(ContentFolder3D + "Track/Textures/Tarmac_spec"),
                Content.Load<Texture2D>(ContentFolder3D + "Track/Textures/TarmacWornAC_maps")
            };
            AssignEffect(track, basicModelEffect.effect);

            
            ShipManager.Init();

            lightsManager = new LightsManager();

            var pl1 = new PointLight(new Vector3(-5, 4, -5), 15f, new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f));
            var pl2 = new PointLight(new Vector3(40, 4, 40), 69f, Color.Cyan.ToVector3(), Color.Cyan.ToVector3());
            var pl3 = new PointLight(new Vector3(-8, 4, -5), 15f, new Vector3(0.5f, 0.3f, 0.2f), new Vector3(0.5f, 0.3f, 0.2f));
            pl1.hasLightGeo = true;
            pl2.hasLightGeo = true;
            pl3.hasLightGeo = true;

            lightsManager.ambientLight = new AmbientLight(new Vector3(20, 50, 20), Vector3.One, Vector3.One, Vector3.One);
            
            lightsManager.register(pl1);
            lightsManager.register(pl2);
            lightsManager.register(pl3);



            screenWidth = GraphicsDevice.Viewport.Width;
            screenHeight = GraphicsDevice.Viewport.Height;

            //test SurfaceFormat.Color for color,normal. 4th target ?
            colorTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24Stencil8);
            normalTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24Stencil8);
            positionTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.Vector4, DepthFormat.Depth24Stencil8);
            lightTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.Vector4, DepthFormat.Depth24Stencil8);

            bloomFilterTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24Stencil8);
            blurHtarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24Stencil8);
            blurVtarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24Stencil8);
            
            prevPositionTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24Stencil8);


            //engineInstance.Play();
        }
        float deltaTimeU;
        protected override void Update(GameTime gameTime)
        {

            deltaTimeU = (float)gameTime.ElapsedGameTime.TotalSeconds;
            currentInputManager.Update(deltaTimeU);

            camera.Update(deltaTimeU);

            lightsManager.Update(deltaTimeU);

            ShipManager.Update(deltaTimeU);
            
            audioListener.Position = camera.position;
            audioListener.Forward = camera.frontDirection;
            audioListener.Up = camera.upDirection;

            base.Update(gameTime);
        }
        double time = 0f;
        double frameTime;
        int fps;
        float deltaTimeD;
        
        protected override void Draw(GameTime gameTime)
        {
            deltaTimeD = (float)gameTime.ElapsedGameTime.TotalSeconds;
            time += deltaTimeD;
            time %= 0.12;
            if (time <= .025)
            {
                fps = (int)(1 / deltaTimeD);
                frameTime = deltaTimeD * 1000;
            }

            switch (gameState)
            {
                case State.MAIN_MENU: DrawMenu(deltaTimeD); break;
                case State.RUN: DrawRun(deltaTimeD); break;
            }

            
            base.Draw(gameTime);
        }
        public bool bloomEnabled = false;
        public bool motionBlurEnabled = false;
        public int motionBlurIntensity = 4;
        void DrawRun(float deltaTime)
        {

            basicModelEffect.SetView(camera.view);
            basicModelEffect.SetProjection(camera.projection);
            deferredEffect.SetView(camera.view);
            deferredEffect.SetProjection(camera.projection);
            deferredEffect.SetCameraPosition(camera.position);

            
            GraphicsDevice.SetRenderTarget(prevPositionTarget);
            SpriteBatch.Begin();
            SpriteBatch.Draw(positionTarget, Vector2.Zero, Color.White);
            SpriteBatch.End();

            if (bloomEnabled)
                GraphicsDevice.SetRenderTargets(colorTarget, normalTarget, positionTarget, bloomFilterTarget);
            else
                GraphicsDevice.SetRenderTargets(colorTarget, normalTarget, positionTarget);

            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            drawTrack();
            ShipManager.Draw(deltaTime);
            lightsManager.DrawLightGeo();

            //lights pass
            if(bloomEnabled)
                GraphicsDevice.SetRenderTargets(lightTarget, blurHtarget, blurVtarget);
            else
                GraphicsDevice.SetRenderTargets(lightTarget);

            GraphicsDevice.BlendState = BlendState.Additive;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;

            deferredEffect.SetColorMap(colorTarget);
            deferredEffect.SetNormalMap(normalTarget);
            deferredEffect.SetPositionMap(positionTarget);
            if (bloomEnabled)
                deferredEffect.SetBloomFilter(bloomFilterTarget);

            lightsManager.Draw();

            //draw to the screen integrating scene color, lights and other effects
            //GraphicsDevice.SetRenderTarget(null);
            //GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1, 0);
            

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            deferredEffect.SetLightMap(lightTarget);
            deferredEffect.SetScreenSize(new Vector2(screenWidth, screenHeight));

            var ep = (int)(ShipManager.shipList[0].enginePitch * 100);
            var esp = (int)(ShipManager.shipList[0].engineSound.Pitch * 100);

            

            if (motionBlurEnabled)
            {
                deferredEffect.effect.Parameters["motionBlurIntensity"].SetValue(motionBlurIntensity);
                deferredEffect.effect.Parameters["prevPositionMap"].SetValue(prevPositionTarget);

                deferredEffect.SetTech("integrate_motion_blur");
            }
            else
            {
                if (bloomEnabled)
                {
                    deferredEffect.SetTech("integrate_bloom");
                    deferredEffect.SetBlurH(blurHtarget);
                    deferredEffect.SetBlurV(blurVtarget);
                }
                else
                    deferredEffect.SetTech("integrate");
            }

            //deferredEffect.CurrentTechnique = deferredEffect.Techniques["integrate"];
            fullScreenQuad.Draw(deferredEffect.effect);

            var rec = new Rectangle(0,0,screenWidth, screenHeight);
            SpriteBatch.Begin(blendState:BlendState.Opaque);

            SpriteBatch.Draw(colorTarget, Vector2.Zero, rec, Color.White, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);
            SpriteBatch.Draw(normalTarget, new Vector2(0, screenHeight- screenHeight/4), rec, Color.White, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);
            SpriteBatch.Draw(positionTarget, new Vector2(screenWidth - screenWidth/ 4, 0), rec, Color.White, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);
            SpriteBatch.Draw(lightTarget, new Vector2(screenWidth - screenWidth / 4, screenHeight - screenHeight / 4), rec, Color.White, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);

            SpriteBatch.End();
            
            SpriteBatch.Begin();
            SpriteBatch.DrawString(Font,"Motion Blur lvl " + motionBlurIntensity+ " FPS: " + fps, Vector2.Zero, Color.White);
            SpriteBatch.End();



        }
        void DrawMenu(float deltaTime)
        {
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1, 0);

            var str = "Main Menu, Enter / Click to Start";
            SpriteBatch.Begin();
            SpriteBatch.DrawString(Font, str, new Vector2(GraphicsDevice.Viewport.Width / 2 - Font.MeasureString(str).Length() / 2, 0), Color.White, 0, new Vector2(0, 0), 1f, SpriteEffects.None, 0f);
            SpriteBatch.End();
        }
        void drawTrack()
        {
            if(bloomEnabled)
                basicModelEffect.SetTech("colorTex_lightEn_bloomEn");
            else
                basicModelEffect.SetTech("colorTex_lightEn");

            //basicModelEffect.effect.Parameters["tiling"].SetValue(Vector2.One * 1f);
            basicModelEffect.SetKA(0.2f);
            basicModelEffect.SetKD(0.7f);
            basicModelEffect.SetKS(0.7f);
            basicModelEffect.SetShininess(30f);
            //effect.Parameters["filter"].SetValue(1);
            var mesh = track.Meshes[0];
            var ST = Matrix.CreateScale(1f) * Matrix.CreateTranslation(0, 0, 0);
            var world = mesh.ParentBone.Transform * ST;
            basicModelEffect.SetWorld(world);
            basicModelEffect.SetInverseTransposeWorld(Matrix.Invert(Matrix.Transpose(world)));
            basicModelEffect.SetColorTexture(trackTex[0]);
            mesh.Draw();

            mesh = track.Meshes[1];
            world = mesh.ParentBone.Transform * ST;
            basicModelEffect.SetWorld(world);
            basicModelEffect.SetInverseTransposeWorld(Matrix.Invert(Matrix.Transpose(world)));
            basicModelEffect.SetColorTexture(trackTex[1]);
            mesh.Draw();

            mesh = track.Meshes[2];
            world = mesh.ParentBone.Transform * ST;
            basicModelEffect.SetWorld(world);
            basicModelEffect.SetInverseTransposeWorld(Matrix.Invert(Matrix.Transpose(world)));
            basicModelEffect.SetColorTexture(trackTex[1]);
            mesh.Draw();

            mesh = track.Meshes[3];
            world = mesh.ParentBone.Transform * ST;
            basicModelEffect.SetWorld(world);
            basicModelEffect.SetInverseTransposeWorld(Matrix.Invert(Matrix.Transpose(world)));
            basicModelEffect.SetColorTexture(trackTex[2]);
            mesh.Draw();

        }
        
        public void SwitchGameState(State newState)
        {
            gameState = newState;
            switch (gameState)
            {
                case State.MAIN_MENU:
                    currentInputManager = inputMainMenu;
                    IsMouseVisible = true;
                    break;
                case State.RUN:
                    currentInputManager = inputRun;
                    camera.ResetToCenter();
                    IsMouseVisible = bool.Parse(CFG["MouseVisible"]);
                    break;
                case State.PAUSE:
                    break;
                case State.OPTIONS:
                    break;

            }

        }
        public static SpaceKarts getInstance()
        {
            return instance;
        }
        public static void AssignEffect(Model m, Effect e)
        {
            foreach (var mesh in m.Meshes)
                foreach (var meshPart in mesh.MeshParts)
                    meshPart.Effect = e;
        }
        //void drawPlane()
        //{


        //    foreach (var mesh in plane.Meshes)
        //    {
        //        var w = mesh.ParentBone.Transform * Matrix.CreateScale(0.01f) * Matrix.CreateTranslation(0, 0, 0);
        //        basicModelEffect.SetWorld(w);
        //        basicModelEffect.SetInverseTransposeWorld(Matrix.Invert(Matrix.Transpose(w)));
        //        //basicModelEffect.SetInverseTransposeWorld(w);
        //        mesh.Draw();
        //    }
        //}
    }
    public enum State
    {
        MAIN_MENU,
        RUN,
        PAUSE,
        OPTIONS
    }
}