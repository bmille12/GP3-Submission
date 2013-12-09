using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using DPSF;
using DPSF.ParticleSystems;

namespace AsteroidRun
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        //Game constants
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        //Models and their transforms
        private Model mdlShip, mdlCollect, mdlDamager, mdlSlower, mdlHealth;
        private Matrix[] mdlShipTransforms, mdlDamagerTransforms, mdlCollectTransforms, mdlSlowerTransforms, mdlHealthTransforms;
        //Array for Objects
        private Object[] objectList = new Object[GameConstants.NumObjects];
        //Dictionaries for textures and Object models
        private Dictionary<String, Texture2D> texList = new Dictionary<String, Texture2D>();
        private Dictionary<string, Model> Models = new Dictionary<string, Model>();
        private Dictionary<string, Matrix[]> ModelTrans = new Dictionary<string, Matrix[]>();

        //Model Collision
        private BoundingSphere PlayerBox;

        //Model Positions
        private Vector3 mdlPosition;
        private float tiltRotation;

        //Player Stats: Health, Score, then Horizontal Speed 
        //Stats stored as Vector3 for ease of change during gameplay
        //Health and score self explanatory, Horizontal Speed will effect how fast player moves left/right
        private Vector3 mdlStats;

        //Camera Values
        private MainCamera mainCamera, secCamera, camera;
        private BasicEffect basicEffect;

        //Extra Values
        private float timer, conTimer, isTilting, timePlayed;
        private int level, conVal, conLevel;
        private string chcd;
        Random r = new Random();

        //Various state variables
        public int state;
        KeyboardState previousKeyState, keyboardState;

        //Background Values
        Texture2D backgroundTexture, nebulaTexture, guiTexture;
        Texture2D[] conTexts = new Texture2D[3];
        int screenWidth, screenHeight;
        Rectangle screenRectangle;
        private SpriteFont font;

        //Sounds
        Song[] gameTheme = new Song[4];
        SoundEffect[] gameFX = new SoundEffect[4];
        bool muted;

        //Particle System
        EngineFire mcParticleSystem = null;
        //MeteorStorm[] mcMeteorStorm = new MeteorStorm[7];
        private MeteorStorm[] mcMeteorStorms = new MeteorStorm[GameConstants.NumObjects];


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            //Set the state and level to begining values
            state = 0;
            level = 1;
            //Set window title
            Window.Title = "Asteroid Run";
            //Initialise the Cameras
            //This is the main camera
            mainCamera = new MainCamera(graphics, new Vector3(0.0f, 2.0f, 10.0f), Vector3.Zero, 0, 0);
            mainCamera.tag = "Main";
            //This is a secondary, top-down camera
            secCamera = new MainCamera(graphics, new Vector3(0, 40, 6), new Vector3(0, -10, 0), -90, 135.1f);
            secCamera.tag = "Secondary";
            //Set the current camera to the Main one
            camera = mainCamera;
            //Initialise the effect
            IntitializeEffect();
            //Set the initial properties for the Objects
            for (int i = 0; i < objectList.Length; i++)
            {
                objectList[i].setContent(this);
            }
            //Initialise the Meteor particle effects
            for (int i = 0; i < mcMeteorStorms.Length; i++)
            {
                mcMeteorStorms[i] = new MeteorStorm(this);
                mcMeteorStorms[i].AutoInitialize(this.GraphicsDevice, this.Content, null);
            }
            //Initial reset for objects, this will be handled by the Objects themselves after initialisation
            ResetObjects();
            //Set Player stats to default value (100 health, 0 score, 0.3 horizontal speed)
            mdlStats = new Vector3(100, 0, 0.3f);
            //Now set the state to the Start screen state
            state = 2;
            //Set mouse to be visible
            this.IsMouseVisible = true;
            base.Initialize();
        }

        /// <summary>
        /// Initialises the effect for models to use
        /// </summary>
        private void IntitializeEffect()
        {
            //Set up the projection and world matrices
            basicEffect = new BasicEffect(graphics.GraphicsDevice);
            basicEffect.Projection = camera.projectionMatrix;
            basicEffect.World = camera.worldMatrix;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            //Load the textures that are used for Backgrounds/GUI
            backgroundTexture = Content.Load<Texture2D>(".\\Textures\\background");
            nebulaTexture = Content.Load<Texture2D>(".\\Textures\\nebula");
            guiTexture = Content.Load<Texture2D>(".\\Textures\\GUI");
            loadCon();
            //Load the sounds for the game themes
            for (int i = 0; i < 4; i++)
            {
                gameTheme[i] = Content.Load<Song>(".\\Sound\\theme" + (i + 1));
            }
            //Load the SFX
            gameFX[0] = Content.Load<SoundEffect>(".\\Sound\\collectsound");
            gameFX[1] = Content.Load<SoundEffect>(".\\Sound\\damagersound");
            gameFX[2] = Content.Load<SoundEffect>(".\\Sound\\healthsound");
            gameFX[3] = Content.Load<SoundEffect>(".\\Sound\\slowersound");
            //Load the Particle System for the Engines
            mcParticleSystem = new EngineFire(this);
            mcParticleSystem.AutoInitialize(this.GraphicsDevice, this.Content, null);
            //Define the screen dimensions
            screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
            screenRectangle = new Rectangle(0, 0, screenWidth, screenHeight);
            //Load model on a seperate thread
            //Create new thread first
            Thread t = new Thread(LoadModels);
            //Start the thread
            t.Start();
            //Load the game font
            font = Content.Load<SpriteFont>(".\\Fonts\\GameFont");
        }

        /// <summary>
        /// Method for specifically loading 3D Models
        /// </summary>
        protected void LoadModels()
        {
            //Load the player ship and set the transforms
            mdlShip = Content.Load<Model>(".\\Models\\Ship");
            mdlShipTransforms = SetupEffectTransformDefaults(mdlShip);
            //Load the Object models and their transforms
            mdlSlower = Content.Load<Model>(".\\Models\\Slower");
            mdlSlowerTransforms = SetupEffectTransformDefaults(mdlSlower);
            mdlHealth = Content.Load<Model>(".\\Models\\Health");
            mdlHealthTransforms = SetupEffectTransformDefaults(mdlHealth);
            mdlDamager = Content.Load<Model>(".\\Models\\Damager");
            mdlDamagerTransforms = SetupEffectTransformDefaults(mdlDamager);
            mdlCollect = Content.Load<Model>(".\\Models\\Collect");
            mdlCollectTransforms = SetupEffectTransformDefaults(mdlCollect);
            //Add the Objects to the list of Models, do the same for the List of their transforms
            Models.Add("mdlHealth", mdlHealth);
            Models.Add("mdlSlower", mdlSlower);
            Models.Add("mdlDamager", mdlDamager);
            Models.Add("mdlCollect", mdlCollect);
            ModelTrans.Add("mdlHealthTransforms", mdlHealthTransforms);
            ModelTrans.Add("mdlSlowerTransforms", mdlSlowerTransforms);
            ModelTrans.Add("mdlDamagerTransforms", mdlDamagerTransforms);
            ModelTrans.Add("mdlCollectTransforms", mdlCollectTransforms);
            //Set the initial player position
            mdlPosition = new Vector3(0, -1.2f, -0.4f);
        }

        /// <summary>
        /// Check inout for GamePad
        /// </summary>
        private void CheckGamePad()
        {
            // Get the current gamepad state.
            GamePadState currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.IsConnected == true)
            {
                if (state == 0)
                {
                    //If Dpad left is pressed
                    if (currentState.DPad.Left == ButtonState.Pressed)
                    {
                        //Set timer to ten, this defines how quickly the tilt will happen
                        timer = 10;
                        //Set the tilt value, this is how much it will tilt
                        isTilting = 0.7f;
                        if (mdlPosition.X > -4.5f)
                        {
                            //Move left
                            mcParticleSystem.updatePosition(mdlStats.Z * -1);
                            mdlPosition.X -= mdlStats.Z;
                        }
                    }
                    //If stick left is pressed
                    if (currentState.ThumbSticks.Left.X < 0)
                    {
                        //Set timer to ten, this defines how quickly the tilt will happen
                        timer = 10;
                        //Set the tilt value, this is how much it will tilt
                        isTilting = 0.7f;
                        if (mdlPosition.X > -4.5f)
                        {
                            //Move left
                            mcParticleSystem.updatePosition(mdlStats.Z * -1);
                            mdlPosition.X -= mdlStats.Z;
                        }
                    }
                    //If stick right is pressed
                    if (currentState.ThumbSticks.Left.X > 0)
                    {
                        //Set timer to ten, this defines how quickly the tilt will happen
                        timer = 10;
                        //Set the tilt value, this is how much it will tilt
                        isTilting = -0.7f;
                        if (mdlPosition.X < 4.5f)
                        {
                            //Move right, take particle effect with us
                            mcParticleSystem.updatePosition(mdlStats.Z);
                            mdlPosition.X += mdlStats.Z;
                        }
                    }
                    //If dpad right is pressed
                    if (currentState.DPad.Right == ButtonState.Pressed)
                    {
                        //Set timer to ten, this defines how quickly the tilt will happen
                        timer = 10;
                        //Set the tilt value, this is how much it will tilt
                        isTilting = -0.7f;
                        if (mdlPosition.X < 4.5f)
                        {
                            //Move right, take particle effect with us
                            mcParticleSystem.updatePosition(mdlStats.Z);
                            mdlPosition.X += mdlStats.Z;
                        }
                    }
                    //If start is pressed
                    if (currentState.Buttons.Start == ButtonState.Pressed)
                    {
                        //Set state to Pause state
                        state = 1;
                        //Need to hide the particle effects from being drawn
                        mcParticleSystem.Visible = false;
                        for (int i = 0; i < mcMeteorStorms.Length; i++)
                        {
                            mcMeteorStorms[i].Visible = false;
                        }
                    }
                    //Allows the player to mute the sounds
                    if (currentState.Buttons.Y == ButtonState.Pressed)
                    {
                        //Check for currently muted state
                        if (MediaPlayer.IsMuted == false)
                        {
                            //If not muted, mute sounds
                            MediaPlayer.IsMuted = true;
                            muted = true;
                        }
                        else
                        {
                            //If muted, unmute sounds
                            MediaPlayer.IsMuted = false;
                            muted = false;
                        }
                    }
                    //Switch to the secondary camera
                    if (currentState.Buttons.LeftShoulder == ButtonState.Pressed)
                    {
                        //Check Main camera is already active first
                        if (camera == mainCamera)
                        {
                            camera = secCamera;
                        }
                    }

                    //Switch to the main camera
                    if (currentState.Buttons.RightShoulder == ButtonState.Pressed)
                    {
                        //Check the secondary camera is already active first
                        if (camera == secCamera)
                        {
                            camera = mainCamera;
                        }
                    }
                }
                if (state == 1)
                {
                    //If Back button is pressed, unpause game
                    if (currentState.Buttons.Start == ButtonState.Pressed)
                    {
                        //Set state to inplay state
                        state = 0;
                        //Make particle systems visible again
                        mcParticleSystem.Visible = true;
                        for (int i = 0; i < mcMeteorStorms.Length; i++)
                        {
                            mcMeteorStorms[i].Visible = true;
                        }
                    }
                }
                if (state == 2 || state == 3)
                {
                    //If Start is pressed, begin game
                    if (currentState.Buttons.Start == ButtonState.Pressed)
                    {
                        //Set state to inplay state
                        state = 0;
                        //Setup starting game values
                        ResetGame();
                        //Set particle system to visible
                        mcParticleSystem.Visible = true;
                    }
                }
            }
        }

        /// <summary>
        /// Method for handling the keyboard input
        /// </summary>
        private void CheckInput(GameTime g)
        {
            //Get keyboard state and save the previous state
            previousKeyState = keyboardState;
            keyboardState = Keyboard.GetState();
            //Cheat codes!
            if (chcd.Contains("CALEDONIAN") == true && conLevel != 1)
            {
                //Initiate death mode...
                MediaPlayer.Stop();
                conLevel = 1;
                UpdatePlayer(new Vector3(900,0,0.3f));
                MediaPlayer.Play(gameTheme[3]);
                level = 50;
                chcd = "";
            }
            if (chcd.Contains("FEISAR") == true && conLevel != 1)
            {
                //Heal player
                mdlStats = new Vector3(100, mdlStats.Y, 0.3f);
                chcd = "";
            }
            
            if (chcd.Contains("BRYANISKING") == true && conLevel != 1)
            {
                //Add 1k health and some speed boost
                mdlStats = new Vector3(1000, mdlStats.Y, 0.5f);
                chcd = "";
            }
            //Ensure a key is pressed before running the rest of method, save processing time when no key is pressed
            if (keyboardState != null)
            {

                // Strafe right.
                if (keyboardState.IsKeyDown(Keys.Right))
                {
                    //Set timer to ten, this defines how quickly the tilt will happen
                    timer = 10;
                    //Set the tilt value, this is how much it will tilt
                    isTilting = -0.7f;
                    if (mdlPosition.X < 4.5f)
                    {
                        //Move right, take particle effect with us
                        mcParticleSystem.updatePosition(mdlStats.Z);
                        mdlPosition.X += mdlStats.Z;
                    }
                }

                //Strafe left
                if (keyboardState.IsKeyDown(Keys.Left))
                {
                    //Set timer to ten, this defines how quickly the tilt will happen
                    timer = 10;
                    //Set the tilt value, this is how much it will tilt
                    isTilting = 0.7f;
                    if (mdlPosition.X > -4.5f)
                    {
                        //Move left
                        mcParticleSystem.updatePosition(mdlStats.Z * -1);
                        mdlPosition.X -= mdlStats.Z;
                    }
                }

                //Strafe left
                if (keyboardState.IsKeyDown(Keys.A))
                {
                    //Set timer to ten, this defines how quickly the tilt will happen
                    timer = 10;
                    //Set the tilt value, this is how much it will tilt
                    isTilting = 0.7f;
                    if (mdlPosition.X > -4.5f)
                    {
                        //Move left
                        mcParticleSystem.updatePosition(mdlStats.Z * -1);
                        mdlPosition.X -= mdlStats.Z;
                    }
                }

                // Strafe right.
                if (keyboardState.IsKeyDown(Keys.D))
                {
                    //Set timer to ten, this defines how quickly the tilt will happen
                    timer = 10;
                    //Set the tilt value, this is how much it will tilt
                    isTilting = -0.7f;
                    if (mdlPosition.X < 4.5f)
                    {
                        //Move right
                        mcParticleSystem.updatePosition(mdlStats.Z);
                        mdlPosition.X += mdlStats.Z;
                    }
                }

                //Switch to the secondary camera
                if (keyboardState.IsKeyDown(Keys.F2))
                {
                    //Check Main camera is already active first
                    if (camera == mainCamera)
                    {
                        camera = secCamera;
                    }
                }

                //Switch to the main camera
                if (keyboardState.IsKeyDown(Keys.F1))
                {
                    //Check the secondary camera is already active first
                    if (camera == secCamera)
                    {
                        camera = mainCamera;
                    }
                }

                //Allows the player to mute the sounds
                if (keyboardState.IsKeyDown(Keys.M) && keyboardState != previousKeyState)
                {
                    //Check for currently muted state
                    if (MediaPlayer.IsMuted == false)
                    {
                        //If not muted, mute sounds
                        MediaPlayer.IsMuted = true;
                        muted = true;
                    }
                    else
                    {
                        //If muted, unmute sounds
                        MediaPlayer.IsMuted = false;
                        muted = false;
                    }
                }
                //Gather some inputs for cheat codes
                if (keyboardState != previousKeyState)
                {
                    Keys[] a = keyboardState.GetPressedKeys();
                    if (a.Length != 0)
                    {
                        chcd += a[0].ToString();
                    }
                }
                //Kill switch, probably only useful in debugging
                if (keyboardState.IsKeyDown(Keys.F10) && keyboardState != previousKeyState)
                {
                    UpdatePlayer(new Vector3(-10000,0,0));
                }

                //Allows the player to Pause the game
                if (keyboardState.IsKeyDown(Keys.P) && keyboardState != previousKeyState)
                {
                    //Set state to Pause state
                    state = 1;
                    //Need to hide the particle effects from being drawn
                    mcParticleSystem.Visible = false;
                    for (int i = 0; i < mcMeteorStorms.Length; i++)
                    {
                        mcMeteorStorms[i].Visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Set up the model transforms
        /// </summary>
        private Matrix[] SetupEffectTransformDefaults(Model myModel)
        {
            Matrix[] absoluteTransforms = new Matrix[myModel.Bones.Count];
            myModel.CopyAbsoluteBoneTransformsTo(absoluteTransforms);
            SetupCameraViews(myModel);
            return absoluteTransforms;
        }

        /// <summary>
        /// Setup the Effects for the models
        /// </summary>
        /// <param name="myModel"></param>
        private void SetupCameraViews(Model myModel)
        {
            foreach (ModelMesh mesh in myModel.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.Projection = camera.projectionMatrix;
                    effect.View = camera.camViewMatrix;
                }
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // Destroy the particle effects
            mcParticleSystem.Destroy();
            for (int i = 0; i < mcMeteorStorms.Length; i++)
            {
                mcMeteorStorms[i].Destroy();
            }
            //Stop the BG music playing
            MediaPlayer.Stop();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            //Check for Gamepad input - outside state code as state handled in the method
            if (GamePad.GetState(PlayerIndex.One).IsConnected == true)
            {
                CheckGamePad();
            }
            //Check game state (0=in play, 1=pause, 2=start screen, 3=end screen
            switch (state)
            {
                //If in play
                case 0:
                    //Increase score and time played
                    mdlStats.Y++;
                    timePlayed++;
                    //When time played hits a certain point, increase level
                    if (timePlayed > 90)
                    {
                        level++;
                        //Update all objects with new level, increasing difficulty
                        for (int i = 0; i < objectList.Length; i++)
                        {
                            objectList[i].updateLevel(level);
                        }
                        //reset time played
                        timePlayed = 0;
                    }
                    //Update the required values for the particle system
                    mcParticleSystem.SetWorldViewProjectionMatrices(Matrix.Identity, camera.camViewMatrix, camera.projectionMatrix);
                    mcParticleSystem.SetCameraPosition(camera.camPosition);
                    mcParticleSystem.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

                    //Check for game input on keyboard
                    CheckInput(gameTime);
                    //Check for collisions
                    CheckCollision();
                    //Update all objects
                    UpdateObjects(gameTime);
                    //Update the ship tilting value
                    UpdateTilt();
                    //Update the camera
                    camera.camUpdate();
                    base.Update(gameTime);
                    break;

                    //If paused
                case 1:
                    //Check for input
                    CheckPauseInput();
                    break;

                    //If on Start screen
                case 2:
                    //Check for input
                    CheckStartInput();
                    break;

                    //if on end screen
                case 3:
                    //Check for input
                    CheckEndInput();
                    break;
            }
        }

        /// <summary>
        /// Method for checking input when game is paused
        /// </summary>
        void CheckPauseInput()
        {
            //Get keyboard state
            previousKeyState = keyboardState;
            keyboardState = Keyboard.GetState();
            //Ensure a key is pressed before running the rest of method, save processing time when no key is pressed
            if (keyboardState != null)
            {
                //If P key is pressed, unpause game
                if (keyboardState.IsKeyDown(Keys.P) && keyboardState != previousKeyState)
                {
                    //Set state to inplay state
                    state = 0;
                    //Make particle systems visible again
                    mcParticleSystem.Visible = true;
                    for (int i = 0; i < mcMeteorStorms.Length; i++)
                    {
                        mcMeteorStorms[i].Visible = true;
                    }
                }
            }
        }

        /// <summary>
        /// Method for checking input while game is on start screen
        /// </summary>
        void CheckStartInput()
        {
            //Get keyboard state
            previousKeyState = keyboardState;
            keyboardState = Keyboard.GetState();
            //Ensure a key is pressed before running the rest of method, save processing time when no key is pressed
            if (keyboardState != null)
            {
                //If S is pressed, begin game
                if (keyboardState.IsKeyDown(Keys.S) && keyboardState != previousKeyState)
                {
                    //Set state to inplay state
                    state = 0;
                    //Setup starting game values
                    ResetGame();
                    //Set particle system to visible
                    mcParticleSystem.Visible = true;
                }
            }
        }

        /// <summary>
        /// Method for checking input while on ending screem
        /// </summary>
        void CheckEndInput()
        {
            //Get keyboard state
            previousKeyState = keyboardState;
            keyboardState = Keyboard.GetState();
            //Ensure a key is pressed before running the rest of method, save processing time when no key is pressed
            if (keyboardState != null)
            {
                //If R key is pressed, restart game
                if (keyboardState.IsKeyDown(Keys.R) || keyboardState.IsKeyDown(Keys.A) )
                {
                    if (keyboardState != previousKeyState)
                    {
                        //Set state to inplay state
                        state = 0;
                        //Reset game initial values
                        ResetGame();
                        //Set particle system to visible
                        mcParticleSystem.Visible = true;
                    }
                }
            }
        }

        /// <summary>
        /// Update ship tilt values
        /// This is used when ship is tilting during strafes
        /// </summary>
        protected void UpdateTilt()
        {
            //Decrement timer
            timer -= 1;
            //If timer hits zero, untilt
            if (timer <= 0) { tiltRotation = 0; }
            else
            {
                //Otherwise keep tilting
                tiltRotation = (float)Math.Sin(timer / 30) * isTilting;
            }
        }

        /// <summary>
        /// Method that Updates all Objects
        /// </summary>
        /// <param name="g"></param>
        protected void UpdateObjects(GameTime g)
        {
            float timeDelta = (float)g.ElapsedGameTime.TotalSeconds;
            //For all Objects in the game
            for (int i = 0; i < GameConstants.NumObjects; i++)
            {
                //update each objects
                objectList[i].Update(timeDelta);
                //If the Object is of type Damager
                if (objectList[i].mdl == "Damager")
                {
                    //Update the Meteor particle system assigned to it
                    mcMeteorStorms[i].setPosition(objectList[i].position);
                    mcMeteorStorms[i].SetWorldViewProjectionMatrices(Matrix.Identity, camera.camViewMatrix, camera.projectionMatrix);
                    mcMeteorStorms[i].SetCameraPosition(camera.camPosition);
                    mcMeteorStorms[i].Update((float)g.ElapsedGameTime.TotalSeconds);
                    if (mcMeteorStorms[i].Emitter.PositionData.Position.Z == -100)
                    {
                        //And update the position of the particle effect
                        objectList[i].setStorm(mcMeteorStorms[i]);
                        mcMeteorStorms[i].Visible = true;
                    }
                }
            }
        }

        /// <summary>
        /// Method for updating player stats
        /// Stats are stored in a Vector3 for efficiency
        /// </summary>
        /// <param name="a"></param>
        protected void UpdatePlayer(Vector3 a)
        {
            //Update stats with parameter a
            mdlStats += a;
            //If health hits zero
            if (mdlStats.X < 1)
            {
                //Stop the BG music
                MediaPlayer.Stop();
                //Set the particle systems to invisible
                mcParticleSystem.Visible = false;
                for (int i = 0; i < mcMeteorStorms.Length; i++)
                {
                    mcMeteorStorms[i].Visible = false;
                }
                //Set state to End state
                state = 3;
            }
            //When slowing the ship down, we check to ensure speed wont hit zero
            if (mdlStats.Z < 0.1f)
            {
                //If it would, we lower health instead
                mdlStats.Z = 0.1f;
                mdlStats.X -= 5;
            }
        }

        /// <summary>
        /// Method for collision detection
        /// </summary>
        protected void CheckCollision()
        {
            //vector3 for BoundingSphere size
            Vector3 max = new Vector3(0.3f, 0.3f, 1f);
            //Set the players boundingSphere
            // ---Named "Playerbox" due to earlier iteration being a Box, not sphere
            PlayerBox = new BoundingSphere(mdlPosition, 1);

            //Iterate through all objects
            for (int i = 0; i < objectList.Length; i++)
            {
                //Ensure object is active, and close to player
                //By doing this, we save processing time rather than checking objects that couldnt
                //possibly collide with the player
                if (objectList[i].isActive && objectList[i].position.Z < 10)
                {
                    //Assign a boundingsphere to the Object
                    BoundingSphere objectSphereA = new BoundingSphere(objectList[i].position, 1);

                    //If it collides with the player
                    if (objectSphereA.Intersects(PlayerBox))
                    {
                        //Get the effect of the Object
                        UpdatePlayer(objectList[i].GetEffect());
                        //Determine object type as well, in order to play correct sound
                        if (objectList[i].mdl == "Collect")
                        {
                            if (muted == false)
                                gameFX[0].Play();
                        }
                        if (objectList[i].mdl == "Damager")
                        {
                            mcMeteorStorms[i].setPosition(new Vector3(0, -1, 10));
                            if (muted == false)
                                gameFX[1].Play();
                        }
                        if (objectList[i].mdl == "Health")
                        {
                            if (muted == false)
                                gameFX[2].Play();
                        }
                        if (objectList[i].mdl == "Slower")
                        {
                            if (muted == false)
                                gameFX[3].Play();
                        }
                        //Reset Object - by recycling, we save processing time that would be used
                        //to destroy and reload required resources
                        objectList[i].Reset();
                    }
                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            //Clear the screen
            GraphicsDevice.Clear(Color.Black);

            //Check the game state
            switch (state)
            {
                    //If in play
                case 0:

                    //Set rasterizer states
                    RasterizerState rs = new RasterizerState();
                    rs.CullMode = CullMode.CullCounterClockwiseFace;
                    graphics.GraphicsDevice.RasterizerState = rs;

                    //Draw and 2D textures on the screen
                    DrawScenery();
                    if (conLevel == 1)
                        drawCon();

                    //2D drawing will change some of the Rendering and Blend states of the Graphics Device
                    //So we will need to reset these to ensure 3D rendering can continue without being affected
                    GraphicsDevice.BlendState = BlendState.Opaque;
                    GraphicsDevice.DepthStencilState = DepthStencilState.Default;

                    //Draw the engine fire particles
                    mcParticleSystem.Draw();

                    //Set up Ship transform and draw it
                    Matrix modelTransform = Matrix.CreateRotationZ(tiltRotation) * Matrix.CreateTranslation(mdlPosition);
                    DrawModel(mdlShip, modelTransform, mdlShipTransforms, "Ship");

                    //Iterate through all Objects
                    for (int i = 0; i < GameConstants.NumObjects; i++)
                    {
                        //If it is active
                        if (objectList[i].isActive)
                        {
                            //Gets object properties and draws the relevant model using string concatenation
                            string obj = "mdl" + objectList[i].mdl; //i.e String is now mdlHealth
                            string objTran = "mdl" + objectList[i].mdl + "Transforms";// i.e string is now mldHealthTransforms
                            //Ensure properties have been loaded correctly
                            if (obj != "mdl" && objTran != "mdlTransforms")
                            {
                                //If it is a Damager
                                if (objectList[i].mdl == "Damager")
                                {
                                    //Draw the meteor particle system as well
                                    mcMeteorStorms[i].Draw();
                                }
                                //Draw the correct model, models are stored in a Dictionary
                                Matrix objectTransform = Matrix.CreateScale(GameConstants.ObjectScalar) * Matrix.CreateTranslation(objectList[i].position);
                                DrawModel(Models[obj], objectTransform, ModelTrans[objTran], objectList[i].mdl);
                            }
                        }
                        //Draw all text, including displaying the score and health values
                        spriteBatch.Begin();
                        spriteBatch.Draw(guiTexture, screenRectangle, Color.White);
                        DrawText("Health: ", new Vector2(25, 70), Color.White, spriteBatch);
                        DrawText(mdlStats.X.ToString(), new Vector2(125, 70), Color.White, spriteBatch);
                        DrawText("Score: ", new Vector2(575, 70), Color.White, spriteBatch);
                        DrawText(mdlStats.Y.ToString(), new Vector2(675, 70), Color.White, spriteBatch);
                        spriteBatch.End();
                    }
                    break;

                    //If state is paused
                case 1:

                    //Draw the Paused content
                    DrawPause();
                    break;

                    //If state is Starting
                case 2:
                    //Draw the starting content
                    DrawStart();
                    break;

                    //If state is ending
                case 3:
                    //Draw the ending content
                    DrawEnd();
                    break;
            }

            base.Draw(gameTime);
        }

        /// <summary>
        /// Method for drawing the background scenery
        /// </summary>
        private void DrawScenery()
        {
            //Uses a new spritebatch
            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteBatch.Begin();
            //Draws the "Starry" background image
            spriteBatch.Draw(backgroundTexture, screenRectangle, Color.White);
            //Draws a "nebula" which includes transparency on top of the starry background
            spriteBatch.Draw(nebulaTexture, screenRectangle, Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Method for drawing the Pause content
        /// </summary>
        private void DrawPause()
        {
            //Uses a new sprite batch
            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteBatch.Begin();
            //Clear the screen
            GraphicsDevice.Clear(Color.Black);
            //Draw the text on the screen
            DrawText("PAUSED", new Vector2((screenWidth / 2) - 40, screenHeight / 2), Color.White, spriteBatch);
            spriteBatch.End();
        }

        /// <summary>
        /// Method for drawing the Start content
        /// </summary>
        private void DrawStart()
        {
            //Uses a new sprite batch
            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteBatch.Begin();
            //Clear the screen
            GraphicsDevice.Clear(Color.Black);
            //Display all the Starting text
            //Needed to be centred manually
            DrawText("YOU ARE FLEEING FOR NO APPARENT REASON", new Vector2((screenWidth / 2) - 220, screenHeight / 2), Color.White, spriteBatch);
            DrawText("USE LEFT+RIGHT OR A+D OR DPAD TO DODGE THE INCOMING ASTEROIDS", new Vector2((screenWidth / 2) - 305, (screenHeight / 2) + 15), Color.White, spriteBatch);
            DrawText("DODGE ASTEROIDS WHICH WILL DAMAGE YOUR SHIP", new Vector2((screenWidth / 2) - 250, (screenHeight / 2) + 30), Color.White, spriteBatch);
            DrawText("DODGE STARS WHICH WILL SLOW YOU DOWN", new Vector2((screenWidth / 2) - 220, (screenHeight / 2) + 45), Color.White, spriteBatch);
            DrawText("COLLECT HEALTH PACKS TO HEAL", new Vector2((screenWidth / 2) - 160, (screenHeight / 2) + 60), Color.White, spriteBatch);
            DrawText("COLLECT THE 'GOLDEN STATUE OF HOPEFUL GAIN' FOR A SCORE BOOST", new Vector2((screenWidth / 2) - 320, (screenHeight / 2) + 75), Color.White, spriteBatch);
            DrawText("USE P/START TO PAUSE AND M TO MUTE SOUNDS", new Vector2((screenWidth / 2) - 215, (screenHeight / 2) + 90), Color.White, spriteBatch);
            DrawText("F1 AND F2 WILL SWITCH CAMERAS", new Vector2((screenWidth / 2) - 175, (screenHeight / 2) + 105), Color.White, spriteBatch);
            DrawText("PRESS S/START TO BEGIN", new Vector2((screenWidth / 2) - 110, (screenHeight / 2) + 155), Color.White, spriteBatch);
            spriteBatch.End();
        }

        /// <summary>
        /// Method for drawing the End content
        /// </summary>
        private void DrawEnd()
        {
            //Uses a new sprite batch
            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteBatch.Begin();
            //Clear the screen
            GraphicsDevice.Clear(Color.Black);
            //Draw the End text on the screen
            //Needs to be centred manually
            DrawText("YOU HAVE FAILED TO DODGE THE ASTEROIDS", new Vector2((screenWidth / 2) - 180, screenHeight / 2), Color.White, spriteBatch);
            DrawText("YOU HAVE DIED A PAINFUL DEATH", new Vector2((screenWidth / 2) - 150, (screenHeight / 2) +15), Color.White, spriteBatch);
            DrawText("SCORE: " + mdlStats.Y, new Vector2((screenWidth / 2) - 50, (screenHeight / 2) + 30), Color.White, spriteBatch);
            DrawText("PRESS R/START TO RESTART", new Vector2((screenWidth / 2) - 105, (screenHeight / 2) + 45), Color.White, spriteBatch);
            spriteBatch.End();
        }

        /// <summary>
        /// Simple method for drawing text on the screen
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="pos"></param>
        /// <param name="col"></param>
        /// <param name="sb"></param>
        private void DrawText(String txt, Vector2 pos, Color col, SpriteBatch sb)
        {
            //Draws text 
            sb.DrawString(font, txt, pos, col);
        }

        /// <summary>
        /// Method for resetting the Objects in the game
        /// Only used at the start of the game
        /// Handled in the Object class rest of the time
        /// </summary>
        private void ResetObjects()
        {
            //Values for reset coordinates
            float xStart;
            float zStart;
            //Random used for determing x coordinate
            Random rnd = new Random();
            for (int i = 0; i < GameConstants.NumObjects; i++)
            {
                //Set the coordinates
                xStart = rnd.Next(1, 10);
                //Z coordinate is always -100
                zStart = -100;
                objectList[i].position = new Vector3(xStart, -1, zStart);
                //Update
                objectList[i].speed = GameConstants.ObjectMinSpeed +
                   (float)rnd.NextDouble() * GameConstants.ObjectMaxSpeed;
                objectList[i].isActive = true;
                objectList[i].updateLevel(level);
            }

        }

        /// <summary>
        /// Method for resetting the Game values
        /// This is called when starting the game at any time
        /// </summary>
        private void ResetGame()
        {
            //Unmute
            muted = false;
            //set initial player stats and position
            mdlStats = new Vector3(100, 0, 0.3f);
            mdlPosition = new Vector3(0, -1.2f, -0.4f);
            //Set level to default
            level = 1;
            conLevel = 0;
            chcd = "";
            //Clear the time played
            timePlayed = 0;
            //Set the particle effects to visible
            mcParticleSystem.setPosition(new Vector3(mdlPosition.X, mdlPosition.Y + 0.18f, mdlPosition.Z + 2));
            for (int i = 0; i < mcMeteorStorms.Length; i++)
            {
                mcMeteorStorms[i].Visible = true;
            }
            //Reset all objects and check for collisions to ensure there were no problems
            ResetObjects();
            CheckCollision();
            //Finally, play the background music
            //Lets choose 1 of the 3 available - random song every play
            MediaPlayer.Play(gameTheme[r.Next(3)]);
            //And set it to repeat
            MediaPlayer.IsRepeating = true;
        }

        /// <summary>
        /// Method for drawing a 3D model on the screen
        /// </summary>
        /// <param name="model"></param>
        /// <param name="modelTransform"></param>
        /// <param name="absoluteBoneTransforms"></param>
        /// <param name="type"></param>
        public void DrawModel(Model model, Matrix modelTransform, Matrix[] absoluteBoneTransforms, String type)
        {

            this.basicEffect.View = camera.camViewMatrix;

            //Draw the model, a model can have multiple meshes, so loop
            foreach (ModelMesh mesh in model.Meshes)
            {
                //This is where the mesh orientation is set
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.View = camera.camViewMatrix;
                    effect.World = absoluteBoneTransforms[mesh.ParentBone.Index] * modelTransform;

                }
                //Draw the mesh, will use the effects set above.
                mesh.Draw();
            }
        }

        /// <summary>
        /// Method for loading text background content
        /// </summary>
        private void loadCon()
        {
            //Load the content
            conTexts[0] = Content.Load<Texture2D>(".\\Content\\red");
            conTexts[1] = Content.Load<Texture2D>(".\\Content\\purple");
            conTexts[2] = Content.Load<Texture2D>(".\\Content\\green");
        }

        /// <summary>
        /// Method for drawing the extra content
        /// </summary>
        private void drawCon()
        {
            //Timer for switching between screens
            conTimer++;
            if (conTimer > 20)
            {
                //Set to next screen
                conVal++;
                //Reset timer
                conTimer = 0;
                //If reached the end of the available screens, start at the begining
                if (conVal > 2) { conVal = 0; }
            }
            //Start a new sprite batch
            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteBatch.Begin();
            //Draw the extra content
            spriteBatch.Draw(conTexts[conVal], screenRectangle, Color.White);
            spriteBatch.End();
        }
    }
}
