#version 330 core

in vec2 pos;

layout(location = 0) out vec4 depth;

void main()
{
    vec2 adjustedPos = pos;
    if (!gl_FrontFacing)
    {
        // Slightly bias back faces inwards.
        // This fixes being able to see a few no-lighting pixels (like space)
        // "behind" walls at certain angles/positions.
        //adjustedPos -= sign(adjustedPos) * 1.5/32.0;
    }
    float dist = length(adjustedPos);
    float dx = dFdx(dist);
    float dy = dFdy(dist); // I'm aware derivative of y makes no sense here but oh well.
    depth = vec4(dist, dist * dist + 0.25 * (dx*dx + dy*dy), 0, 1);
}

