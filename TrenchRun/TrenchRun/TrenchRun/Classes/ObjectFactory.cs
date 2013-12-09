using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace AsteroidRun
{
    class ObjectFactory
    {
        /// <summary>
        /// Simple ObjectFactory for creating a set off Effect variables
        /// And then returning them for the Object to use
        /// </summary>

        //Vector for Effect
        Vector3 effect;

        /// <summary>
        /// Method for assigning the attributes to be returns
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public Vector3 AssignAttributes(String txt)
        {
            //Based on what is needed, set the Effect Vector
            if (txt == "Health")
            {
                //Is a health pack
                effect.X = 15;
            }
            if (txt == "Damager")
            {
                //Is a Damager
                effect.X = -15;
            }
            if (txt == "Slower")
            {
                //Is a Slower
                effect.Z = -0.1f;
            }
            if (txt == "Collect")
            {
                //Is a Collect
                effect.Y = 1000;
            }
            //And simply return it
            return effect;
        }
    }
}
