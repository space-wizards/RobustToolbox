using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using ClientInterfaces;
using ClientServices.Map;
using ClientServices.Map.Tiles;
using SS13_Shared;

namespace ClientServices.Lighting
{
    /// <summary>
    ///  <para>Describes Colors of 4 Vertices.</para>
    /// </summary>
    public struct SpriteLightDefinition
    {
        public Vector3D VertexColLowerLeft;
        public Vector3D VertexColLowerRight;
        public Vector3D VertexColUpperLeft;
        public Vector3D VertexColUpperRight;
        //No need for a constructor. Struct members automatically init to their default values. In this case 0.

        public void ClampByAmbient(int ambientLightLevel)
        {

            VertexColLowerLeft.X = Math.Max(Math.Min(VertexColLowerLeft.X, 254),ambientLightLevel);
            VertexColLowerLeft.Y = Math.Max(Math.Min(VertexColLowerLeft.Y, 254),ambientLightLevel);
            VertexColLowerLeft.Z = Math.Max(Math.Min(VertexColLowerLeft.Z, 254),ambientLightLevel);

            VertexColLowerRight.X = Math.Max(Math.Min(VertexColLowerRight.X, 254),ambientLightLevel);
            VertexColLowerRight.Y = Math.Max(Math.Min(VertexColLowerRight.Y, 254),ambientLightLevel);
            VertexColLowerRight.Z = Math.Max(Math.Min(VertexColLowerRight.Z, 254),ambientLightLevel);

            VertexColUpperLeft.X = Math.Max(Math.Min(VertexColUpperLeft.X, 254),ambientLightLevel);
            VertexColUpperLeft.Y = Math.Max(Math.Min(VertexColUpperLeft.Y, 254),ambientLightLevel);
            VertexColUpperLeft.Z = Math.Max(Math.Min(VertexColUpperLeft.Z, 254),ambientLightLevel);

            VertexColUpperRight.X = Math.Max(Math.Min(VertexColUpperRight.X, 254),ambientLightLevel);
            VertexColUpperRight.Y = Math.Max(Math.Min(VertexColUpperRight.Y, 254),ambientLightLevel);
            VertexColUpperRight.Z = Math.Max(Math.Min(VertexColUpperRight.Z, 254), ambientLightLevel);
        }
    }

    public class LightManager : ILightManager
    {
        public int ambientBrightness = 55;

        /// <summary>
        ///  <para>Applies Color Definitions to the Vertices of a Sprite.</para>
        /// </summary>
        private void ApplyDefinitionToSprite(Sprite sprite, SpriteLightDefinition lightInfo)
        {
            sprite.SetSpriteVertexColor(VertexLocations.LowerLeft,
                System.Drawing.Color.FromArgb((int)(lightInfo.VertexColLowerLeft.Length * 0.575f),
                (int)lightInfo.VertexColLowerLeft.X,
                (int)lightInfo.VertexColLowerLeft.Y,
                (int)lightInfo.VertexColLowerLeft.Z));

            sprite.SetSpriteVertexColor(VertexLocations.LowerRight,
                System.Drawing.Color.FromArgb((int)(lightInfo.VertexColLowerRight.Length * 0.575f),
                (int)lightInfo.VertexColLowerRight.X,
                (int)lightInfo.VertexColLowerRight.Y,
                (int)lightInfo.VertexColLowerRight.Z));

            sprite.SetSpriteVertexColor(VertexLocations.UpperLeft,
                System.Drawing.Color.FromArgb((int)(lightInfo.VertexColUpperLeft.Length * 0.575f),
                (int)lightInfo.VertexColUpperLeft.X,
                (int)lightInfo.VertexColUpperLeft.Y,
                (int)lightInfo.VertexColUpperLeft.Z));

            sprite.SetSpriteVertexColor(VertexLocations.UpperRight,
                System.Drawing.Color.FromArgb((int)(lightInfo.VertexColUpperRight.Length * 0.575f),
                (int)lightInfo.VertexColUpperRight.X,
                (int)lightInfo.VertexColUpperRight.Y,
                (int)lightInfo.VertexColUpperRight.Z));
        }

        /// <summary>
        ///  <para>Calculates Effect of Light on Given Vertex of Sprite.</para>
        /// </summary>
        private Vector3D CalculateVertexLight(Sprite sprite, Light light, VertexLocations vertex, Vector2D screenOffset)
        {
            Vector2D vertexPos = sprite.GetSpriteVertexPosition(vertex);
            Vector2D lightPos = light.position - screenOffset; //Making sure that they're in the same space.

            var distance = (vertexPos - lightPos).Length;
            var lightIntensity = (float)Math.Max((light.range - (distance/1.5)) * light.Brightness, 0);

            if (lightIntensity == 0) return Vector3D.Zero; //Must be zero or the ambient light would increase with the number of lights. (Thats bad)

            var lightColor =
                new Vector3D(
                    light.color.R,
                    light.color.G,
                    light.color.B);

            lightColor.Normalize();

            lightColor *= lightIntensity;
            //lightColor += new Vector3D(light.brightness, light.brightness, light.brightness);

            return lightColor;
        }

        /// <summary>
        ///  <para>Applies List of lights to given sprite.</para>
        /// </summary>
        public void ApplyLightsToSprite(List<ILight> lights, Sprite sprite, Vector2D screenOffset)
        {
            sprite.UpdateAABB(); //Just to be safe that the verts are in the right pos. Might want to remove this when its handled reliably by the objects.

            var lightsInRange = from Light l in lights where (l.position - sprite.Position - screenOffset).Length <= (l.range * 4) select l;
            Vector3D defaultLight = new Vector3D(ambientBrightness, ambientBrightness, ambientBrightness);

            SpriteLightDefinition lightInfo = new SpriteLightDefinition();
            lightInfo.VertexColLowerLeft += SquareVertexLight(defaultLight);
            lightInfo.VertexColLowerRight += SquareVertexLight(defaultLight);
            lightInfo.VertexColUpperLeft += SquareVertexLight(defaultLight);
            lightInfo.VertexColUpperRight += SquareVertexLight(defaultLight);

            float LLIntensity = defaultLight.Length;
            float LRIntensity = defaultLight.Length;
            float ULIntensity = defaultLight.Length;
            float URIntensity = defaultLight.Length;

            foreach (Light currentLight in lightsInRange)
            {
                //Calculate light color incident on tile
                var ll = CalculateVertexLight(sprite, currentLight, VertexLocations.LowerLeft, screenOffset);
                var lr = CalculateVertexLight(sprite, currentLight, VertexLocations.LowerRight, screenOffset);
                var ul = CalculateVertexLight(sprite, currentLight, VertexLocations.UpperLeft, screenOffset);
                var ur = CalculateVertexLight(sprite, currentLight, VertexLocations.UpperRight, screenOffset);
                // Add the squared values to the compiled lightinfo values 
                lightInfo.VertexColLowerLeft += SquareVertexLight(ll);
                lightInfo.VertexColLowerRight += SquareVertexLight(lr);
                lightInfo.VertexColUpperLeft += SquareVertexLight(ul);
                lightInfo.VertexColUpperRight += SquareVertexLight(ur);
                // Add intensity of light to the pile
                LLIntensity += ll.Length;
                LRIntensity += lr.Length;
                ULIntensity += ul.Length;
                URIntensity += ur.Length;
            }

            //Munge intensity down
            LLIntensity = (float)(LLIntensity / 1.73205081); // ALGEBRA - REPLACES (float)Math.Sqrt(Math.Pow(LLIntensity, 2) / 3);
            LRIntensity = (float)(LRIntensity / 1.73205081);
            ULIntensity = (float)(ULIntensity / 1.73205081);
            URIntensity = (float)(URIntensity / 1.73205081);
            lightInfo.VertexColLowerLeft = SqrtVertexLight(lightInfo.VertexColLowerLeft);
            lightInfo.VertexColLowerRight = SqrtVertexLight(lightInfo.VertexColLowerRight);
            lightInfo.VertexColUpperLeft = SqrtVertexLight(lightInfo.VertexColUpperLeft);
            lightInfo.VertexColUpperRight = SqrtVertexLight(lightInfo.VertexColUpperRight);
            lightInfo.VertexColLowerLeft = NormalizeLight(lightInfo.VertexColLowerLeft, LLIntensity);
            lightInfo.VertexColLowerRight = NormalizeLight(lightInfo.VertexColLowerRight, LRIntensity);
            lightInfo.VertexColUpperLeft = NormalizeLight(lightInfo.VertexColUpperLeft, ULIntensity);
            lightInfo.VertexColUpperRight = NormalizeLight(lightInfo.VertexColUpperRight, URIntensity);


            //lightInfo.ClampByAmbient(ambientBrightness);
            ApplyDefinitionToSprite(sprite, lightInfo);
        }

        public Vector3D MaxLight(Vector3D lightColor1, Vector3D lightColor2)
        {
            Vector3D max = new Vector3D();
            max.X = Math.Max(lightColor1.X, lightColor2.X);
            max.Y = Math.Max(lightColor1.Y, lightColor2.Y);
            max.Z = Math.Max(lightColor1.Z, lightColor2.Z);
            return max;
        }

        public Vector3D SquareVertexLight(Vector3D lightColor)
        {
            lightColor.X *= lightColor.X;
            lightColor.Y *= lightColor.Y;
            lightColor.Z *= lightColor.Z;
            return lightColor;
        }

        public Vector3D SqrtVertexLight(Vector3D lightColor)
        {
            return new Vector3D((float)Math.Sqrt(lightColor.X), (float)Math.Sqrt(lightColor.Y), (float)Math.Sqrt(lightColor.Z));
        }

        public Vector3D NormalizeLight(Vector3D lightColor, float intensity)
        {
            float normalizeto = Math.Min(intensity, 254);
            double maxComponent = Math.Max(lightColor.X, Math.Max(lightColor.Y, lightColor.Z));
            if (maxComponent == 0 || maxComponent <= 254)
                return lightColor;
            lightColor.X = lightColor.X / (float)(maxComponent / normalizeto);
            lightColor.Y = lightColor.Y / (float)(maxComponent / normalizeto);
            lightColor.Z = lightColor.Z / (float)(maxComponent / normalizeto);
            return lightColor;
        }

        public ILight CreateLight(IMapManager mapManager, Color color, int range, LightState lightState, Vector2D position)
        {
            return new Light(mapManager, color, range, lightState, position);
        }
    }

}