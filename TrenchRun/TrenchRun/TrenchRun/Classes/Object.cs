using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using DPSF;
using DPSF.ParticleSystems;

namespace AsteroidRun
{

    struct Object
    {
        //Vectors for position and effects
        public Vector3 position, previousPos, effect;
        //Game the Object is being used in
        Game cGame;
        //Speed value
        public float speed;
        //Value for if the Object is active
        public bool isActive;
        //Random object
        Random rnd;
        //Tag for the Object type
        public string mdl;
        //Level of difficulty
        private int level;
        //Assigned MeteorStorm particle effect
        MeteorStorm mstorm;

        /// <summary>
        /// Sets the game the Object is being used in
        /// </summary>
        /// <param name="cg"></param>
        public void setContent(Game cg)
        {
            cGame = cg;
        }

        /// <summary>
        /// Method for Updating the Object
        /// </summary>
        /// <param name="delta"></param>
        public void Update(float delta)
        {
            //Store the previous position
            previousPos = position;
            //Move towards the player
            position.Z += level + (speed *
                        GameConstants.ObjectSpeedAdjustment * delta);
            //If off camera, reset position
            if (position.Z > 10)
            {
                Reset();
            }     
        }

        /// <summary>
        /// Method for resetting the Object values
        /// </summary>
        public void Reset()
        {
            //Use a Factory class to determine effects
            ObjectFactory o = new ObjectFactory();
            //Clear the effect variable
            effect = Vector3.Zero;
            //New Random
            rnd = new Random();
            //Determine random X Coordinate
            position.X = rnd.Next(0, 16) - 8;
            //Set Z coordinate
            position.Z = -100;
            //Choose random Object type
            int a = rnd.Next(0, 8);
            //Based on that, retrieve effects from Factory and set relevant tab
            switch (a)
            {
                case 0:
                    //Example, for type 0, we get the effects of a "Health" object from the Factory
                    effect = o.AssignAttributes("Health");
                    //And set the tag to "Health"
                    mdl = "Health";
                    break;
                case 1:
                    effect = o.AssignAttributes("Damager");
                    mdl = "Damager";
                    break;
                case 2:
                    effect = o.AssignAttributes("Damager");
                    mdl = "Damager";
                    break;
                case 3:
                    effect = o.AssignAttributes("Damager");
                    mdl = "Damager";
                    break;
                case 4:
                    effect = o.AssignAttributes("Damager");
                    mdl = "Damager";
                    break;
                case 5:
                    effect = o.AssignAttributes("Slower");
                    mdl = "Slower";
                    break;
                case 6:
                    effect = o.AssignAttributes("Slower");
                    mdl = "Slower";
                    break;
                case 7:
                    effect = o.AssignAttributes("Collect");
                    mdl = "Collect";
                    break;
            }
        }

        /// <summary>
        /// Method that returns the effect of this Object
        /// </summary>
        /// <returns></returns>
        public Vector3 GetEffect()
        {
            return effect;
        }

        /// <summary>
        /// Method for updating the level of the Object
        /// </summary>
        /// <param name="a"></param>
        public void updateLevel(int a)
        {
            level = a/10;
        }

        /// <summary>
        /// Method for assigning a Particle Effect to the Object
        /// </summary>
        /// <param name="m"></param>
        public void setStorm(MeteorStorm m)
        {
            //assign the particle effect
            mstorm = m;
        }
    }
}

