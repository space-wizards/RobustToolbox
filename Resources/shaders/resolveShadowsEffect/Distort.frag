
uniform sampler2D inputSampler;


vec4 DistortPS()
{
	  //translate u and v into [-1 , 1] domain
	  float u0 = gl_TexCoord[0].x * 2 - 1;
	  float v0 = gl_TexCoord[0].y * 2 - 1;
	  
	  //then, as u0 approaches 0 (the center), v should also approach 0 
	  v0 = v0 * abs(u0);

      //convert back from [-1,1] domain to [0,1] domain
	  v0 = (v0 + 1) / 2;

	  //we now have the coordinates for reading from the initial image
	  vec2 newCoords = vec2(gl_TexCoord[0].x, v0);

	  //read for both horizontal and vertical direction and store them in separate channels
	  float horizontal = texture2D(inputSampler, newCoords).r;
	  float vertical = texture2D(inputSampler, newCoords.yx).r;
      return vec4(horizontal,vertical ,0,1);
}
void main()
{
 gl_FragColor = DistortPS();
}