
// Amount to blur.
float blurAmount = 0.0135;

// Our texture sampler.
uniform sampler2D sourceSampler;


// Function to perform the sampling for the blur.
vec4 psBlurSample(vec2 Tex , vec4 baseColor, float offX, float offY)
{
	vec4 Color;				    // Output.
  	float scaler = 0;			// Scale of the sample.
  	  
  	// Calculate sample.
  	scaler = (1 + (offY * offX));
	Tex.x = Tex.x + offX;
	Tex.y = Tex.y + offY;
	
   	Color = baseColor + texture2D(sourceSampler, Tex / scaler);   	
   	return Color;
}


vec4 Blur()
{
  	vec4 Color = 0;				// Output.
  	float Alpha = 0;				// Alpha component.
  	float blurValue = 0;				// Blur value.
  	
  	blurValue = blurAmount / 1000.0f;
  	
  	if (blurAmount < 0)
  	   blurValue = 0;
  		
  	if (blurAmount > 10)
  	   blurValue = 0.01;


	Color = texture2D(sourceSampler, gl_TexCoord[0]);	

	// Store the alpha for later, we don't want to blur that.
	Alpha = Color.a;
	
	// Sample eight directions + the center.
<<<<<<< HEAD
  	Color = psBlurSample(gl_TexCoord[0], Color, -blurValue, -blurValue);
  	Color = psBlurSample(gl_TexCoord[0], Color, 0, -blurValue);
  	Color = psBlurSample(gl_TexCoord[0], Color, blurValue, -blurValue);  	
  	Color = psBlurSample(gl_TexCoord[0], Color, -blurValue, blurValue);
  	Color = psBlurSample(gl_TexCoord[0], Color, 0, blurValue);
  	Color = psBlurSample(gl_TexCoord[0], Color, blurValue, blurValue);  	
  	Color = psBlurSample(gl_TexCoord[0], Color, -blurValue, 0);
  	Color = psBlurSample(gl_TexCoord[0], Color, blurValue, 0);
  	
  	// Calculate final color.
   	Color.rgb = clamp((Color.rgb / 9) * vec3(.8,.8,.8),0,1);
   	// Restore and combine the alpha.
   	Color.a = Alpha * float(1);
   	
    return Color;
}

void main()
{
	gl_FragColor = Blur();

}


