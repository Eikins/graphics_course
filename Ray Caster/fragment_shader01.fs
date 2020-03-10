#version 330 core

uniform vec2 mousePos;
uniform float time;
uniform float aspectRatio;

in vec2 fragCoord;

out vec4 outColor;

#define DIST_MIN 1e-5 // minimum distance to objects
#define DIST_MAX 1e+5 // maximum distance to objects 

// ray structure
struct Ray 
{
  vec3 ro; // origin
  vec3 rd; // direction
};

// plane structure
struct Plane 
{
  vec3 n; // normal
  float d; // offset
};

// sphere structure
struct Sphere 
{
  vec3 c; // center
  float r; // radius
};

// struct for remembering intersected object
struct ISObj 
{
  float d; // distance to the object
  int t; // type (-1=nothing,0=plane, 1=sphere)
  int i; // object ID
};

const float DP = 10.0;


// intersection test with one plane 
ISObj intersectPlane(in Plane p,in Ray r,in int id) 
{
	//TODO
    return ISObj(DIST_MAX,-1,-1);
}


// intersection test with one sphere 
ISObj intersectSphere(in Sphere s,in Ray r,in int id) 
{
	//TODO
    return ISObj(DIST_MAX,-1,-1);
}

// intersection test for all objects in the scene
ISObj intersectObjects(in Ray r) 
{
	  //TODO
    return ISObj(DIST_MAX,-1,-1);
}

Ray generatePerspectiveRay(in vec2 p) 
{
  // p is the current pixel coord, in [-1,1]
  //TODO
  return  Ray(vec3(0,0,0),vec3(0,0,0));
}

vec3 computeNormal(in ISObj is,in Ray r) 
{
	  //TODO
    return vec3(0,0,0);
}


void main() 
{
	//TODO
  outColor = vec4(vec3(sin(0.2*time)),1);

}
