using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;

namespace AsteroidRun
{
    class MainCamera
    {
        //--------------------------------------------------------------------------------------
        // Added for the creation of a camera
        //--------------------------------------------------------------------------------------
        public Matrix camViewMatrix, camRotationMatrix, projectionMatrix, worldMatrix; //Cameras matrices
        public Vector3 camPosition, camLookat, camTransform; //Position, Lookat and transform of Camera in world
        float camPitch, camYaw; //Defines the amount of rotation and position on Y
        GraphicsDeviceManager graphics;
        public String tag;

        /// <summary>
        /// Constructor for the camera class
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pos"></param>
        /// <param name="look"></param>
        /// <param name="pitch"></param>
        /// <param name="yaw"></param>
        public MainCamera(GraphicsDeviceManager g, Vector3 pos, Vector3 look, float pitch, float yaw)
        {
            //Set the initial variables
            graphics = g;
            camPosition = pos; 
            camLookat = look;
            //Initialize the camera transform
            InitializeTransform(g);
            //Initialize the camera angles
            camPitch = pitch;
            camYaw = yaw;
        }
        
        /// <summary>
        /// Method for creating the camera looks
        /// </summary>
        /// <param name="g"></param>
        private void InitializeTransform(GraphicsDeviceManager g)
        {
            //Create the view matrix
            camViewMatrix = Matrix.CreateLookAt(camPosition, camLookat, Vector3.Up);

            //And the projecton matrix
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45),
                (float)g.GraphicsDevice.Viewport.Width /
                (float)g.GraphicsDevice.Viewport.Height,
                1.0f, 1000.0f);

            //Finally the world matrix
            worldMatrix = Matrix.Identity;


        }        

        /// <summary>
        /// Update the camera
        /// </summary>
        public void camUpdate()
        {
            //Update the matrices so that camera changes are reflected
            camRotationMatrix = Matrix.CreateRotationX(camPitch) * Matrix.CreateRotationY(camYaw);
            camTransform = Vector3.Transform(Vector3.Forward, camRotationMatrix);
            camLookat = camPosition + camTransform;

            camViewMatrix = Matrix.CreateLookAt(camPosition, camLookat, Vector3.Up);
        }
    }
}
