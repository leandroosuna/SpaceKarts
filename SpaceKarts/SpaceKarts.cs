using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

using SpaceKarts.Cameras;
using SpaceKarts.Managers;
using SpaceKarts.Effects;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using Riptide.Utils;
using Riptide;
using BepuPhysics;
using BepuUtilities.Memory;
using SpaceKarts.Physics;
using System.Numerics;
using BepuPhysics.Collidables;

using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Quaternion = Microsoft.Xna.Framework.Quaternion;

using NumericVector3 = System.Numerics.Vector3;
using NumericVector2 = System.Numerics.Vector2;
using NumericQuaternion = System.Numerics.Quaternion;
using Box = BepuPhysics.Collidables.Box;

using BepuUtilities;
using BepuPhysics.Constraints;

namespace SpaceKarts
{
    public class SpaceKarts : Game
    {
        public GraphicsDeviceManager Graphics;
        public SpriteBatch SpriteBatch;
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
        RenderTarget2D tempTarget;
        RenderTarget2D normalTarget;
        RenderTarget2D positionTarget;
        RenderTarget2D positionTarget2;
        bool lastPositionInMainTarget = false;
        RenderTarget2D lightTarget;
        
        RenderTarget2D bloomFilterTarget;
        RenderTarget2D blurHtarget;
        RenderTarget2D blurVtarget;
    

        Model sphere, cube, plane, track, lightSphere, cone, lightCone;
        Texture2D[] trackTex;
        
        public Microsoft.Xna.Framework.Graphics.Effect effect;
        public Camera camera;
        public FullScreenQuad fullScreenQuad;
        public AudioListener audioListener;

        public static SpaceKarts instance;

        public LightsManager lightsManager;
        public InputManager currentInputManager;
        InputManager inputRun;
        InputManager inputMainMenu;

        public Simulation Simulation;
        public BufferPool BufferPool; 
        public SimpleThreadDispatcher ThreadDispatcher;
        public SimpleCarController playerController;

        public Gizmos gizmos;
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

            string hostname = "nix.dynamic-dns.net";
            IPAddress[] ips = Dns.GetHostAddresses(hostname);

            RiptideLogger.Initialize(Console.WriteLine, false);

            string ip = ips[0].ToString();
            Debug.WriteLine("connecting to " + ip);

            NetworkManager.Enabled = false;            
            //NetworkManager.Connect(ip, 9999);

            Exiting += (s, e) => NetworkManager.DisconnectClient();

        }
        
        float gravity = 10;
        BodyHandle boxHandle;
        Matrix boxWorld;
        protected override void Initialize()
        {
            gizmos = new Gizmos();

            var viewport = GraphicsDevice.Viewport;
            screenCenter = new Point(viewport.Width / 2, viewport.Height / 2);
            if (!Graphics.IsFullScreen)
                screenCenter += Window.Position;
            instance = this;
            inputMainMenu = new Input_MainMenu();
            inputRun = new Input_GameRunning();

            SwitchGameState(State.RUN);

            camera = new Camera(viewport.AspectRatio, screenCenter);
            InputManager.Init();

            audioListener = new AudioListener();
            fullScreenQuad = new FullScreenQuad(GraphicsDevice);
            SpriteBatch = new SpriteBatch(GraphicsDevice);



            base.Initialize();
        }
        
        protected override void LoadContent()
        {
            BufferPool = new BufferPool();
            gizmos.LoadContent(GraphicsDevice);
            var targetThreadCount = Math.Max(1,
                Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
            ThreadDispatcher = new SimpleThreadDispatcher(targetThreadCount);

            var properties = new CollidableProperty<CarBodyProperties>();
            Simulation = Simulation.Create(BufferPool,
                new CarCallbacks() { Properties = properties },
                new PoseIntegratorCallbacks(new NumericVector3(0, -gravity, 0)),
                new SolveDescription(6, 1));

            //Simulation = Simulation.Create(Bufferpool,)
            var radius = 2;

            var boxShape = new Box(radius, radius, radius);
            var boxInertia = boxShape.ComputeInertia(.5f);
            var boxIndex = Simulation.Shapes.Add(boxShape);
            var position = new NumericVector3(5, 10, 5);

            var bodyDescription = BodyDescription.CreateDynamic(position, boxInertia,
                new CollidableDescription(boxIndex, 0.1f), new BodyActivityDescription(0.01f));

            boxHandle = Simulation.Bodies.Add(bodyDescription);


            var builder = new CompoundBuilder(BufferPool, Simulation.Shapes, 1);
            builder.Add(new Box(4f, 1f, 4.73f), RigidPose.Identity, 10);
            //builder.Add(new Box(7f, 2f, 2.5f), new NumericVector3(0, 0.65f, -0.35f), 0.5f);
            builder.BuildDynamicCompound(out var children, out var bodyInertia, out _);
            builder.Dispose();
            
            var bodyShape = new Compound(children);
            
            var bodyShapeIndex = Simulation.Shapes.Add(bodyShape);
            var wheelShape = new Cylinder(0.4f, .18f);
            var wheelInertia = wheelShape.ComputeInertia(0.1f);
            var wheelShapeIndex = Simulation.Shapes.Add(wheelShape);

          

            //floor collider
            var floorHeight = 4;
            Simulation.Statics.Add(new StaticDescription(new NumericVector3(0, -floorHeight / 2f, 0),
               Simulation.Shapes.Add(new Box(2000, floorHeight, 2000))));

            const float x = 2f;
            const float y = -0.1f;
            const float frontZ = 1.7f;
            const float backZ = -1.7f;
            const float wheelBaseWidth = x * 2;
            const float wheelBaseLength = frontZ - backZ;


            playerController = new SimpleCarController(SimpleCar.Create(Simulation, properties, new NumericVector3(0, 10, 0), bodyShapeIndex, bodyInertia, 0.5f, wheelShapeIndex, wheelInertia, 2f,
                new NumericVector3(-x, y, frontZ), new NumericVector3(x, y, frontZ), new NumericVector3(-x, y, backZ), new NumericVector3(x, y, backZ), new NumericVector3(0, -1, 0), 0.25f,
                new SpringSettings(5f, 0.7f), QuaternionEx.CreateFromAxisAngle(NumericVector3.UnitZ, MathF.PI * 0.5f)),
                forwardSpeed: 250, forwardForce: 1000, zoomMultiplier: 2, backwardSpeed: 150, backwardForce: 1000, idleForce: 0.25f, brakeForce: 200, steeringSpeed: 5f, maximumSteeringAngle: MathF.PI * 0.55f,
                wheelBaseLength: wheelBaseLength, wheelBaseWidth: wheelBaseWidth, ackermanSteering: 1);

            

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
            colorTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, 
                SurfaceFormat.HalfVector4, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            tempTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false,
                SurfaceFormat.HalfVector4, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);

            normalTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, 
                SurfaceFormat.HalfVector4, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            positionTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, 
                SurfaceFormat.Vector4, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            positionTarget2 = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false,
                SurfaceFormat.Vector4, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            lightTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, 
                SurfaceFormat.Vector4, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);

            bloomFilterTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, 
                SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            blurHtarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, 
                SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            blurVtarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, 
                SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);

            
            //byte[] testBuffer;
            //ShakePacket sp = new ShakePacket();
            //sp.id = 20;
            //testBuffer = udpClient.createHandShakeDataPacket(sp);


            //_ = udpClient.Send(testBuffer);

            //GameDataPacket gdp = new GameDataPacket();
            //gdp.position = new Vector3(1, 2, 3);

            //gdp.id = 2;

            //testBuffer = udpClient.createGameDataPacket(gdp);

            //_ = udpClient.Send(testBuffer);
            //engineInstance.Play();
        }
        float deltaTimeU;
        bool canImpulse = true;
        public float boost = 0;
        float timeU = 0f;
        protected override void Update(GameTime gameTime)
        {
            var timeStepDuration = (1 / 171f);
            Simulation.Timestep(timeStepDuration, ThreadDispatcher);

            deltaTimeU = (float)gameTime.ElapsedGameTime.TotalSeconds;
            currentInputManager.Update(deltaTimeU);
            NetworkManager.UpdateClient();


            camera.Update(deltaTimeU);

            lightsManager.Update(deltaTimeU);

            //ShipManager.Update(deltaTimeU);
            
            audioListener.Position = camera.position;
            audioListener.Forward = camera.frontDirection;
            audioListener.Up = camera.upDirection;


            var refe = Simulation.Bodies.GetBodyReference(boxHandle);


            if (!refe.Awake)
                refe.Awake = true;
            timeU += deltaTimeU;

            //refe.Pose.Position = new NumericVector3(10f * MathF.Sin(timeU), 0, 10f *MathF.Cos(timeU));
            if (Keyboard.GetState().IsKeyDown(Keys.B) && canImpulse)
            {
                canImpulse = false;
                if (!refe.Awake)
                    refe.Awake = true;
                refe.ApplyImpulse(new NumericVector3(0.5f, 6f, 0), new NumericVector3(-0.5f, 0, -0.5f));
            }
            if (Keyboard.GetState().IsKeyUp(Keys.B))
                canImpulse = true;


            var pose = refe.Pose;
            var position = pose.Position;
            var quaternion = pose.Orientation;
            boxWorld =
                Matrix.CreateScale(new Vector3(1f,1f, 1f)) *
                Matrix.CreateFromQuaternion(NumQuatToQuat(quaternion)) *
                Matrix.CreateTranslation(NumV3ToV3(position));

            boxPositionStr = "BP " + (int)position.X + "," + (int)position.Y + "," + (int)position.Z;



            float steeringSum = 0;
            if (InputManager.keyMappings.TurnLeftAlt.IsDown())
            {
                steeringSum += 1;
                //Debug.WriteLine("left");
            }
            if (InputManager.keyMappings.TurnRightAlt.IsDown())
            {
                steeringSum -= 1;
            }
            var targetSpeedFraction = InputManager.keyMappings.AccelerateAlt.IsDown() ? 1f : InputManager.keyMappings.BrakeAlt.IsDown() ? -1f : 0;


            if (InputManager.keyMappings.Boost.IsDown())
            {
                boost += deltaTimeU *1.5f;
                if (boost > 1)
                    boost = 1;
            }
            boost -= deltaTimeU * 0.1f;
            if(boost < 0) 
                boost = 0;
            boxPositionStr += " Boost " + boost;
            //For control purposes, we'll match the fixed update rate of the simulation. Could decouple it- this dt isn't
            //vulnerable to the same instabilities as the simulation itself with variable durations.
            playerController.Update(Simulation, timeStepDuration, steeringSum, targetSpeedFraction, false, InputManager.keyMappings.BrakeAlt.IsDown());

            var b = playerController.Car.Body;
            var car = Simulation.Bodies.GetBodyReference(b).Pose;
            var carPos = car.Position;
            var carQuaternion = car.Orientation;

            //ShipManager.shipList[0].position = carPos;
            boxPositionStr += " CP " + (int)carPos.X + "," + (int)carPos.Y + "," + (int)carPos.Z;
            ShipManager.Update(deltaTimeU);

            ShipManager.PlayerUpdate(deltaTimeU, car.Position, car.Orientation);

            gizmos.UpdateViewProjection(camera.view, camera.projection);

            base.Update(gameTime);
        }
        double time = 0f;
        double frameTime;
        int fps;
        float deltaTimeD;

        string boxPositionStr;
        public bool debugRTs = false;
        public bool debugGizmos = true;
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

            //gizmos.DrawCube(boxWorld, Color.Magenta);
            if (debugGizmos)
            {
                gizmos.DrawCube(new Vector3(0, 5, 0), new Vector3(2, 2, 2),Color.Magenta);


                var refe = Simulation.Bodies.GetBodyReference(boxHandle);
                var pos = refe.Pose.Position;
                var or = refe.Pose.Orientation;
                var world = Matrix.CreateScale(2f) 
                    * Matrix.CreateFromQuaternion(NumQuatToQuat(or))
                    * Matrix.CreateTranslation(NumV3ToV3(pos));

                gizmos.DrawCube(world, Color.Yellow);

                var car = playerController.Car;
                refe = Simulation.Bodies.GetBodyReference(car.Body);
                pos = refe.Pose.Position;
                or = refe.Pose.Orientation;
                world = Matrix.CreateScale(4f, 1f, 4.73f)
                    * Matrix.CreateFromQuaternion(NumQuatToQuat(or))
                    * Matrix.CreateTranslation(NumV3ToV3(pos));
                
                gizmos.DrawCube(world, Color.White);

                refe = Simulation.Bodies.GetBodyReference(car.BackLeftWheel.Wheel);
                pos = refe.Pose.Position;
                or = refe.Pose.Orientation;
                world = Matrix.CreateScale(.4f, .18f, .4f)
                    * Matrix.CreateFromQuaternion(NumQuatToQuat(or))
                    * Matrix.CreateTranslation(NumV3ToV3(pos));
                gizmos.DrawCylinder(world, Color.White);
                
                refe = Simulation.Bodies.GetBodyReference(car.BackRightWheel.Wheel);
                pos = refe.Pose.Position;
                or = refe.Pose.Orientation;
                world = Matrix.CreateScale(.4f, .18f, .4f)
                    * Matrix.CreateFromQuaternion(NumQuatToQuat(or))
                    * Matrix.CreateTranslation(NumV3ToV3(pos));
                gizmos.DrawCylinder(world, Color.White);
                
                refe = Simulation.Bodies.GetBodyReference(car.FrontLeftWheel.Wheel);
                pos = refe.Pose.Position;
                or = refe.Pose.Orientation;
                world = Matrix.CreateScale(.4f, .18f, .4f)
                    * Matrix.CreateFromQuaternion(NumQuatToQuat(or))
                    * Matrix.CreateTranslation(NumV3ToV3(pos));
                gizmos.DrawCylinder(world, Color.White);

                refe = Simulation.Bodies.GetBodyReference(car.FrontRightWheel.Wheel);
                pos = refe.Pose.Position;
                or = refe.Pose.Orientation;
                world = Matrix.CreateScale(.4f, .18f, .4f)
                    * Matrix.CreateFromQuaternion(NumQuatToQuat(or))
                    * Matrix.CreateTranslation(NumV3ToV3(pos));
                gizmos.DrawCylinder(world, Color.White);

                gizmos.Draw();
            }
            base.Draw(gameTime);
        }

        //Ray CalculateCursorRay(Vector2 cursorPosition, Matrix viewMatrix, Matrix projectionMatrix)
        //{
        //    Vector3 nearSource = new Vector3(cursorPosition, 0f);
        //    Vector3 farSource = new Vector3(cursorPosition, 1f);

        //    Vector3 nearPoint = GraphicsDevice.Viewport.Unproject(nearSource, projectionMatrix, viewMatrix, Matrix.Identity);
        //    Vector3 farPoint = GraphicsDevice.Viewport.Unproject(farSource, projectionMatrix, viewMatrix, Matrix.Identity);

        //    Vector3 direction = farPoint - nearPoint;
        //    direction.Normalize();

        //    return new Ray(nearPoint, direction);
        //}
        public bool bloomEnabled = false;
        public bool motionBlurEnabled = false;
        public int motionBlurIntensity = 5;
        void DrawRun(float deltaTime)
        {
            //Init effects
            var view = camera.view;
            var projection = camera.projection;
            basicModelEffect.SetView(view);
            basicModelEffect.SetProjection(projection);
            deferredEffect.SetView(view);
            deferredEffect.SetProjection(projection);
            deferredEffect.SetCameraPosition(camera.position);
            deferredEffect.effect.Parameters["inverseViewProjection"]?.SetValue(Matrix.Invert(view * projection));
            basicModelEffect.effect.Parameters["zNear"]?.SetValue(camera.nearPlaneDistance);
            basicModelEffect.effect.Parameters["zFar"]?.SetValue(camera.farPlaneDistance);

            //string rayS = "";
            //Ray ray = new Ray(camera.position, camera.frontDirection);

            //(bool collided, Vector3 hitPosition) = lightsManager.RayIntersects(ray);

            //if (collided)
            //{
            //    rayS += "HIT " + (int)hitPosition.X + "," + (int)hitPosition.Y + "," + (int)hitPosition.Z;

            //}

            // GBuffer pass:
            // - ColorTarget RGB = Color ,          A = KD, A==0: lighting disabled         
            // - NormalTaget RGB = normal [0-1],    A = KS                                  
            // - PosTarget RGB = position float4    A = Shininess/20                           
            // - filterTarget RGB = bloomfilter     A = depth (not in use, might be useful) 
            GraphicsDevice.SetRenderTargets(colorTarget, normalTarget, lastPositionInMainTarget ? positionTarget : positionTarget2, bloomFilterTarget);
            // Clearing not required when using RenderTargetUsage.DiscardContents
            //GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1, 0);
            lastPositionInMainTarget = !lastPositionInMainTarget;

            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            //TODO: Draw scene abstraction
            //drawTrack();
            drawPlane();
            drawBox();
            ShipManager.Draw(deltaTime);
            lightsManager.DrawLightGeo();

            //Draw light volumes, blur bloom target if bloom enabled in order to optimize fullscreen quad calls
            GraphicsDevice.SetRenderTargets(lightTarget, blurHtarget, blurVtarget);
            GraphicsDevice.BlendState = BlendState.Additive;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;

            deferredEffect.SetColorMap(colorTarget);
            deferredEffect.SetNormalMap(normalTarget);
            deferredEffect.SetPositionMap(lastPositionInMainTarget ? positionTarget2 : positionTarget);
            //not necessary to set it every frame but required if we use A value for something (like depth)
            //if (bloomEnabled)
            deferredEffect.SetBloomFilter(bloomFilterTarget);

            lightsManager.Draw();

            //draw to the screen integrating scene color, lights and other effects
            deferredEffect.SetLightMap(lightTarget);
            deferredEffect.SetScreenSize(new Vector2(screenWidth, screenHeight));

            var ep = (int)(ShipManager.shipList[0].enginePitch * 100);
            var esp = (int)(ShipManager.shipList[0].engineSound.Pitch * 100);

            if (!bloomEnabled && !motionBlurEnabled)
            {
                deferredEffect.SetTech("integrate");
            }
            if (bloomEnabled && !motionBlurEnabled)
            {
                deferredEffect.SetBlurH(blurHtarget);
                deferredEffect.SetBlurV(blurVtarget);
              
                deferredEffect.SetTech("integrate_bloom");
            }
            if (!bloomEnabled && motionBlurEnabled)
            {
                deferredEffect.effect.Parameters["motionBlurIntensity"]?.SetValue(motionBlurIntensity);

                deferredEffect.effect.Parameters["prevPositionMap"]?.SetValue(lastPositionInMainTarget ? positionTarget : positionTarget2);


                deferredEffect.SetTech("integrate_motion_blur");
                deferredEffect.effect.Parameters["bloomPassBefore"]?.SetValue(false);
            }
            if (bloomEnabled && motionBlurEnabled)
            {
                GraphicsDevice.SetRenderTarget(tempTarget);
                GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                deferredEffect.SetTech("integrate_bloom");
                fullScreenQuad.Draw(deferredEffect.effect);

                deferredEffect.SetColorMap(tempTarget);

                deferredEffect.effect.Parameters["motionBlurIntensity"]?.SetValue(motionBlurIntensity);
                deferredEffect.effect.Parameters["prevPositionMap"]?.SetValue(lastPositionInMainTarget ? positionTarget : positionTarget2);
                deferredEffect.effect.Parameters["bloomPassBefore"]?.SetValue(true);
                deferredEffect.SetTech("integrate_motion_blur");

            }

            // Render to Screen
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            fullScreenQuad.Draw(deferredEffect.effect);

            // Targets on screen corners
            if (debugRTs)
            { 
                var rec = new Rectangle(0,0,screenWidth, screenHeight);

                SpriteBatch.Begin(blendState: BlendState.Opaque);

                SpriteBatch.Draw(colorTarget, Vector2.Zero, rec, Color.White, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);
                SpriteBatch.Draw(normalTarget, new Vector2(0, screenHeight - screenHeight / 4), rec, Color.White, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);
                SpriteBatch.Draw(lastPositionInMainTarget ? positionTarget2 : positionTarget, 
                    new Vector2(screenWidth - screenWidth / 4, 0), rec, Color.White, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);
                SpriteBatch.Draw(lightTarget, new Vector2(screenWidth - screenWidth / 4, screenHeight - screenHeight / 4), rec, Color.White, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);

                SpriteBatch.End();
            }
            // Some info on top left
            SpriteBatch.Begin();
            var lightsCount = lightsManager.lightsToDraw.Count;
            //SpriteBatch.DrawString(Font,"Motion Blur lvl " + motionBlurIntensity+ " FPS: " + fps+" LC "+ lightsCount + " "+ rayS , Vector2.Zero, Color.White);
            SpriteBatch.DrawString(Font, "FPS " + fps + " " + boxPositionStr, Vector2.Zero, Color.White);
            SpriteBatch.End();

            deferredEffect.SetPrevView(camera.view);
            deferredEffect.SetPrevProjection(camera.projection);


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
            basicModelEffect.SetTech("color_tex");
            basicModelEffect.SetLightEnabled(true);
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
                    //camera.ResetToCenter();
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
        public static void AssignEffect(Model m, Microsoft.Xna.Framework.Graphics.Effect e)
        {
            foreach (var mesh in m.Meshes)
                foreach (var meshPart in mesh.MeshParts)
                    meshPart.Effect = e;
        }
        void drawBox()
        {
            basicModelEffect.SetLightEnabled(true);
            basicModelEffect.SetTech("color_solid");
            basicModelEffect.SetColor(Color.DarkCyan.ToVector3());

            foreach (var mesh in cube.Meshes)
            {
                //var w = mesh.ParentBone.Transform * Matrix.CreateScale(0.01f) * Matrix.CreateTranslation(0, 0, 0);
                var w = boxWorld;
                basicModelEffect.SetWorld(w);
                basicModelEffect.SetInverseTransposeWorld(Matrix.Invert(Matrix.Transpose(w)));
                mesh.Draw();
            }
        }
        void drawPlane()
        {
            basicModelEffect.SetLightEnabled(true);
            basicModelEffect.SetTech("color_solid");
            basicModelEffect.SetColor(Color.Gray.ToVector3());
            foreach (var mesh in plane.Meshes)
            {
                var w = mesh.ParentBone.Transform * Matrix.CreateScale(1f) * Matrix.CreateTranslation(0, 0, 0);
                basicModelEffect.SetWorld(w);
                basicModelEffect.SetInverseTransposeWorld(Matrix.Invert(Matrix.Transpose(w)));
                mesh.Draw();
            }
        }

        //List<Triangle> ExtractTriangles(Microsoft.Xna.Framework.Graphics.Model model)
        //{
        //    List<Triangle> triangles = new List<Triangle>();

        //    foreach(var mesh in model.Meshes)
        //    {
        //        foreach (var meshPart in mesh.MeshParts)
        //        {
        //            var readData = new Vector3[4];
        //            var vertexStride = VertexPositionColor.VertexDeclaration.VertexStride;
        //            var offset = 0;
        //            var startIndex = 0;
        //            var elementCount = 4;

        //            meshPart.VertexBuffer.GetData(offset, readData, startIndex, elementCount, vertexStride);

        //            for (int i = 0; i * 3< meshPart.IndexBuffer.IndexCount;i++)
        //            {

        //                var bu = (var data, )
        //            }
        //        }
        //    }
        //    //for (int i = 0; i * 3 < indices.length; i++)
        //    //{
        //    //    Triangle triangle = null;

        //    //    Vertex p = new Vertex(vertices.get(indices[3 * i]));
        //    //    Vertex p1 = new Vertex(vertices.get(indices[3 * i + 1]));
        //    //    Vertex p2 = new Vertex(vertices.get(indices[3 * i + 2]));

        //    //    triangle = new Triangle(p, p1, p2);

        //    //    triangles.add(triangle);
        //    //}

        //}

        public static Vector3 NumV3ToV3(NumericVector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
        public static NumericVector3 V3ToNumV3(Vector3 v)
        {
            return new NumericVector3(v.X, v.Y, v.Z);
        }
        public static Quaternion NumQuatToQuat(NumericQuaternion quat)
        {
            return new Quaternion(quat.X, quat.Y, quat.Z, quat.W);
        }

        protected override void UnloadContent()
        {
            Simulation.Dispose();

            BufferPool.Clear();

            ThreadDispatcher.Dispose();
        }
    }
    public enum State
    {
        MAIN_MENU,
        RUN,
        PAUSE,
        OPTIONS
    }
    

}