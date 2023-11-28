using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceKarts.Managers
{
    public static class ShipManager
    {
        public static List<Ship> shipList;
        public static List<Ship> shipsToDraw;

        static Model[] models;
        static Texture2D[] colors;
        static Texture2D emissive;

        static SoundEffect engineSound;
        static SpaceKarts game;


        public static void Init()
        {
            game = SpaceKarts.getInstance();
            LoadContent(game.Content, game.basicModelEffect.effect);    

            shipList = new List<Ship>();
            shipList.Add(new Ship(game, models[0], colors[7], emissive, engineSound.CreateInstance()));
            shipsToDraw = new List<Ship>();
        }
        public static void Update(float deltaTime)
        {
            shipList.ForEach(ship => ship.Update(deltaTime));
            shipsToDraw.Clear();
            foreach(var ship in shipList)
            {
                if(game.camera.frustumContains(ship.boxCollider))
                    shipsToDraw.Add(ship);
            }
        }
        public static void Draw(float deltaTime)
        {
            shipsToDraw.ForEach(ship => ship.Draw(deltaTime));
        }

        static void LoadContent(ContentManager content, Effect effect)
        {
            models = new Model[]{
                content.Load<Model>(SpaceKarts.ContentFolder3D + "Ships/A"),
                content.Load<Model>(SpaceKarts.ContentFolder3D + "Ships/B"),
                content.Load<Model>(SpaceKarts.ContentFolder3D + "Ships/C"),
                content.Load<Model>(SpaceKarts.ContentFolder3D + "Ships/D"),
                content.Load<Model>(SpaceKarts.ContentFolder3D + "Ships/E")
            };
            foreach (var s in models)
                SpaceKarts.AssignEffect(s, effect);

            colors = new Texture2D[]{
                content.Load<Texture2D>(SpaceKarts.ContentFolder3D + "Ships/Textures/StarSparrow_Black"),
                content.Load<Texture2D>(SpaceKarts.ContentFolder3D + "Ships/Textures/StarSparrow_Blue"),
                content.Load<Texture2D>(SpaceKarts.ContentFolder3D + "Ships/Textures/StarSparrow_Cyan"),
                content.Load<Texture2D>(SpaceKarts.ContentFolder3D + "Ships/Textures/StarSparrow_Green"),
                content.Load<Texture2D>(SpaceKarts.ContentFolder3D + "Ships/Textures/StarSparrow_Grey"),
                content.Load<Texture2D>(SpaceKarts.ContentFolder3D + "Ships/Textures/StarSparrow_Orange"),
                content.Load<Texture2D>(SpaceKarts.ContentFolder3D + "Ships/Textures/StarSparrow_Purple"),
                content.Load<Texture2D>(SpaceKarts.ContentFolder3D + "Ships/Textures/StarSparrow_Red"),
                content.Load<Texture2D>(SpaceKarts.ContentFolder3D + "Ships/Textures/StarSparrow_White"),
                content.Load<Texture2D>(SpaceKarts.ContentFolder3D + "Ships/Textures/StarSparrow_Yellow"),
            };
            emissive = content.Load<Texture2D>(SpaceKarts.ContentFolder3D + "Ships/Textures/StarSparrow_Emission");

            //mask = new Texture2D[]{
            //    content.Load<Texture2D>(CaleShip.ContentFolder3D + "Ships/Textures/Masks/StarSparrow_Mask1"),
            //    content.Load<Texture2D>(CaleShip.ContentFolder3D + "Ships/Textures/Masks/StarSparrow_Mask2"),
            //    content.Load<Texture2D>(CaleShip.ContentFolder3D + "Ships/Textures/Masks/StarSparrow_Mask3"),
            //    content.Load<Texture2D>(CaleShip.ContentFolder3D + "Ships/Textures/Masks/StarSparrow_Mask4")

            //};
            //shipNormalTex = Content.Load<Texture2D>(CaleShip.ContentFolder3D + "Ships/Textures/StarSparrow_Normal");

            engineSound = content.Load<SoundEffect>(SpaceKarts.ContentFolderAudio + "engine");
        }
    }
}
