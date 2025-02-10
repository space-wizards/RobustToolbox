using System.Drawing;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

[NetworkedComponent]
public abstract partial class SharedDynamicParticlesComponent : Component {
	//The width and height of the drawing surface for the particles - this is the size of the texture that the particles are drawn on.
    [ViewVariables] public int width = 640;
    [ViewVariables] public int height = 480;
	//Maximum number of particles in this system
    [ViewVariables] public int count = 1000;
	//Maximum number of particles spawned per second
    [ViewVariables] public float spawning = 100;
	//Texture that the particles have. This is a list of paths to textures, and the particle system will randomly choose one of them for each particle. If blank, the particle will be a 1px white dot.
    [ViewVariables] public ResPath[] texture = [];
	//Maximum lifespan of the partcles in seconds
    [ViewVariables] public float lifespan;
    [ViewVariables] public int fadein = 0;
    [ViewVariables] public int fadeout = 0;
	//Color of the particles. This can either be a list of a colours, or a gradient that the particles will interpolate between over their lifespan.
    [ViewVariables] public Color[] color = [];
	//Starting position of the particles. X,Y,Z. The Z co-ordinate determines layering order.
    [ViewVariables] public Vector3 spawn_position;
	//Starting velocity of the particles
    [ViewVariables] public Vector3 spawn_velocity;
	//Acceleration applied to the particles per second
    [ViewVariables] public Vector3 acceleration;
	//Scaling applied to the particles in (x,y)
    [ViewVariables] public Vector2 scale;
	//Rotation applied to the particles in degrees
    [ViewVariables] public float rotation = 0;
	//Increase in scale per second
    [ViewVariables] public Vector2 growth;
	//Change in rotation per second
    [ViewVariables] public float spin = 0;
}
