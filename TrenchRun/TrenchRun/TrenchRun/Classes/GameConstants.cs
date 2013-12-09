using System;
using System.Collections.Generic;
using System.Text;

namespace AsteroidRun
{
    static class GameConstants
    {
        //camera constants
        public const float CameraHeight = 25000.0f;
        public const float PlayfieldSizeX = 100f;
        public const float PlayfieldSizeZ = 300f;
        //Object constants
        public const int NumObjects = 7;
        public const float ObjectMinSpeed = 3.0f;
        public const float ObjectMaxSpeed = 10.0f;
        public const float ObjectSpeedAdjustment = 2.5f;
        public const float ObjectScalar = 0.01f;
        //collision constants
        public const float ObjectBoundingSphereScale = 0.025f;  //50% size
        public const float ShipBoundingSphereScale = 0.5f;  //50% size

    }
}
