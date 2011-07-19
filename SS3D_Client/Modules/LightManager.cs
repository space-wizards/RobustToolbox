using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS3D;

namespace SS3D.Modules
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

    public class LightManager
    {
        private static LightManager singleton;

        public int ambientBrightness = 15;

        private const int errorTolerance = 64; //Keep this around the size of tiles. Used to find out if a light is out of range.

        private LightManager() { }

        public static LightManager Singleton
        {
            get 
            {
                if (singleton == null)
                {
                    singleton = new LightManager();
                }
                return singleton;
            }
        }

        /// <summary>
        ///  <para>Applies Color Definitions to the Vertices of a Sprite.</para>
        /// </summary>
        private void ApplyDefinitionToSprite(Sprite sprite, SpriteLightDefinition lightInfo)
        {
            sprite.SetSpriteVertexColor(VertexLocations.LowerLeft,
                System.Drawing.Color.FromArgb(
                (int)lightInfo.VertexColLowerLeft.X,
                (int)lightInfo.VertexColLowerLeft.Y,
                (int)lightInfo.VertexColLowerLeft.Z));

            sprite.SetSpriteVertexColor(VertexLocations.LowerRight,
                System.Drawing.Color.FromArgb(
                (int)lightInfo.VertexColLowerRight.X,
                (int)lightInfo.VertexColLowerRight.Y,
                (int)lightInfo.VertexColLowerRight.Z));

            sprite.SetSpriteVertexColor(VertexLocations.UpperLeft,
                System.Drawing.Color.FromArgb(
                (int)lightInfo.VertexColUpperLeft.X,
                (int)lightInfo.VertexColUpperLeft.Y,
                (int)lightInfo.VertexColUpperLeft.Z));

            sprite.SetSpriteVertexColor(VertexLocations.UpperRight,
                System.Drawing.Color.FromArgb(
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

            float distance = (vertexPos - lightPos).Length;
            float lightIntensity = Math.Max(light.range - distance, 0);

            if (lightIntensity == 0) return Vector3D.Zero; //Must be zero or the ambient light would increase with the number of lights. (Thats bad)

            Vector3D lightColor =
                new Vector3D(
                    light.color.R,
                    light.color.G,
                    light.color.B);

            lightColor.Normalize();

            lightColor *= lightIntensity;
            lightColor += new Vector3D(light.brightness, light.brightness, light.brightness);

            return lightColor;
        }

        /// <summary>
        ///  <para>Applies List of lights to given sprite.</para>
        /// </summary>
        public void ApplyLightsToSprite(List<Light> lights, Sprite sprite, Vector2D screenOffset)
        {
            sprite.UpdateAABB(); //Just to be safe that the verts are in the right pos. Might want to remove this when its handled reliably by the objects.

            var lightsInRange = from Light l in lights where (l.position - sprite.Position).Length <= (l.range + errorTolerance) select l;

            SpriteLightDefinition lightInfo = new SpriteLightDefinition();
            float LLIntensity = 0;
            float LRIntensity = 0;
            float ULIntensity = 0;
            float URIntensity = 0;

            foreach (Light currentLight in lights)
            {
                var ll = CalculateVertexLight(sprite, currentLight, VertexLocations.LowerLeft, screenOffset);
                LLIntensity += ll.Length;
                lightInfo.VertexColLowerLeft += SquareVertexLight(ll);
                var lr = CalculateVertexLight(sprite, currentLight, VertexLocations.LowerRight, screenOffset);
                LRIntensity += lr.Length;
                lightInfo.VertexColLowerRight += SquareVertexLight(lr);
                var ul = CalculateVertexLight(sprite, currentLight, VertexLocations.UpperLeft, screenOffset);
                ULIntensity += ul.Length;
                lightInfo.VertexColUpperLeft += SquareVertexLight(ul);
                var ur = CalculateVertexLight(sprite, currentLight, VertexLocations.UpperRight, screenOffset);
                URIntensity += ur.Length;
                lightInfo.VertexColUpperRight += SquareVertexLight(ur);
            }
            LLIntensity = (float)Math.Sqrt(Math.Pow(LLIntensity, 2) / 3);
            LRIntensity = (float)Math.Sqrt(Math.Pow(LRIntensity, 2) / 3);
            ULIntensity = (float)Math.Sqrt(Math.Pow(ULIntensity, 2) / 3);
            URIntensity = (float)Math.Sqrt(Math.Pow(URIntensity, 2) / 3);
            lightInfo.VertexColLowerLeft = SqrtVertexLight(lightInfo.VertexColLowerLeft); 
            lightInfo.VertexColLowerRight = SqrtVertexLight(lightInfo.VertexColLowerRight); 
            lightInfo.VertexColUpperLeft = SqrtVertexLight(lightInfo.VertexColUpperLeft);
            lightInfo.VertexColUpperRight = SqrtVertexLight(lightInfo.VertexColUpperRight);
            lightInfo.VertexColLowerLeft = NormalizeLight(lightInfo.VertexColLowerLeft, LLIntensity);
            lightInfo.VertexColLowerRight = NormalizeLight(lightInfo.VertexColLowerRight, LRIntensity);
            lightInfo.VertexColUpperLeft = NormalizeLight(lightInfo.VertexColUpperLeft, ULIntensity);
            lightInfo.VertexColUpperRight = NormalizeLight(lightInfo.VertexColUpperRight, URIntensity);


            lightInfo.ClampByAmbient(ambientBrightness);
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
    }

    public class Light
    {
        public System.Drawing.Color color = System.Drawing.Color.PapayaWhip;
        public int range = 150;
        public int brightness = 35;

        public LightState state;
        public Vector2D position;
        public Vector2D lastPosition;
        public Modules.Map.Map map;
        public List<LightDirection> direction;

        public Light(Modules.Map.Map _map, System.Drawing.Color _color, int _range, LightState _state, System.Drawing.Point _position, LightDirection _direction)
        {
            map = _map;
            color = _color;
            range = _range;
            state = _state;
            lastPosition = _position;
            direction = new List<LightDirection>();
            direction.Add(_direction);
            UpdateLight();
        }

        public void UpdateLight()
        {
        }

        public void UpdatePosition(Vector2D newPosition)
        {
            lastPosition = position;
            position = newPosition;
            if (position != lastPosition)
            {
                UpdateLight();
            }
        }

    }
}