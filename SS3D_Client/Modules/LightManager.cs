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
        //Do range check on lights before applying them to sprite. if light range > some verts pos + 64(this would be the tile size) then its definitely out of range.

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

            //lightColor.X.Clamp(0, 255);
            //lightColor.Y.Clamp(0, 255); //Might want to remove those when clamping is done on the total of all lights.
            //lightColor.Z.Clamp(0, 255);

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

            foreach (Light currentLight in lights)
            {
                lightInfo.VertexColLowerLeft += CalculateVertexLight(sprite, currentLight, VertexLocations.LowerLeft, screenOffset);
                lightInfo.VertexColLowerRight += CalculateVertexLight(sprite, currentLight, VertexLocations.LowerRight, screenOffset);
                lightInfo.VertexColUpperLeft += CalculateVertexLight(sprite, currentLight, VertexLocations.UpperLeft, screenOffset);
                lightInfo.VertexColUpperRight += CalculateVertexLight(sprite, currentLight, VertexLocations.UpperRight, screenOffset);
            }

            lightInfo.ClampByAmbient(ambientBrightness);
            ApplyDefinitionToSprite(sprite, lightInfo);
        }
    }

    public class Light
    {
        public System.Drawing.Color color = System.Drawing.Color.PapayaWhip;
        public int range = 150;
        public int brightness = 25;

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