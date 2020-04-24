#version 330 core

uniform vec2 mousePos;
uniform float time;
uniform float aspectRatio;

uniform mat4 _View;

in vec2 fragCoord;

out vec4 outColor;

#define DIST_MIN 1e-5 // minimum distance to objects
#define DIST_MAX 1e+5 // maximum distance to objects 
#define PI 3.14159


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

#define OBJECT_TYPE_NONE -1
#define OBJECT_TYPE_PLANE 0
#define OBJECT_TYPE_SPHERE 1

// struct for remembering intersected object
struct ISObj 
{
  float d; // distance to the object
  int t; // type (-1=nothing,0=plane, 1=sphere)
  int i; // object ID
};

struct PBRMat {
  vec3 color;
  float roughness;
  float metallic;
  float ao;
};

struct HitSurface {
  vec3 point;
  vec3 normal;
  PBRMat material;
};


#define LIGHT_TYPE_DIRECTIONAL 0
#define LIGHT_TYPE_POINT 1

struct Light {
  int type;
  vec3 dir;
  vec3 center;
  float intensity;
  vec3 color;
};

const float DP = 10.0;

#define PLANE_LEN 1
#define SPHERE_LEN 7
const Plane[PLANE_LEN] planes = Plane[](
  Plane(vec3(0.0, 0.0, 1.0), 5.0)
);

const Sphere[SPHERE_LEN] spheres = Sphere[](
  Sphere(vec3(0, 0.0, 100.0), 0.0),
  Sphere(vec3(2.5, 0.0, -2.0), 0.3),
  Sphere(vec3(1.5, 0.0, -2.0), 0.3),
  Sphere(vec3(0.5, 0.0, -2.0), 0.3),
  Sphere(vec3(-0.5, 0.0, -2.0), 0.3),
  Sphere(vec3(-1.5, 0.0, -2.0), 0.3),
  Sphere(vec3(-2.5, 0.0, -2.0), 0.3)
);

const PBRMat[7] materials = PBRMat[](
  PBRMat(vec3(1.0, 0.0, 0.0), 1.0, 0.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 0.1, 1.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 0.3, 1.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 0.5, 1.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 0.7, 1.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 0.9, 1.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 1.0, 1.0, 1.0)
);

#define LIGHT_LEN 1
const Light[LIGHT_LEN] lights = Light[](
  Light(LIGHT_TYPE_POINT, vec3(0.0), vec3(0.0), 40.0, vec3(1.0))
);


// intersection test with one plane 
ISObj intersectPlane(in Plane p,in Ray r,in int id) 
{
  float t = - (p.d + dot(r.ro, p.n)) / dot(r.rd, p.n);
  
  if (t < 0.0) {
    return ISObj(DIST_MAX, OBJECT_TYPE_NONE, -1);
  } else {
    return ISObj(t, OBJECT_TYPE_PLANE, id);
  }
}


// intersection test with one sphere 
ISObj intersectSphere(in Sphere s,in Ray r,in int id) 
{
  vec3 offset = (r.ro - s.c);
  float a = dot(r.rd, r.rd);
  float b = 2.0 * dot(offset, r.rd);
  float c = dot(offset, offset) - s.r * s.r;
  
  float det = sqrt(b*b - 4.0*a*c);
  
  if (det < 0.0) {
    return ISObj(DIST_MAX, OBJECT_TYPE_NONE, -1);
  } else {
    float t = min(- b - det, - b + det) / (2.0 * a);
    if (t > 0.0)
      return ISObj(t, OBJECT_TYPE_SPHERE, id);
    else 
      return ISObj(DIST_MAX, OBJECT_TYPE_NONE, -1);
  }
}

// intersection test for all objects in the scene
ISObj intersectObjects(in Ray r) 
{
  ISObj nearestObj = ISObj(DIST_MAX, -1, -1);
  
  for (int i = 0; i < PLANE_LEN; i++) {
    ISObj raycast = intersectPlane(planes[i], r, i);
    if(raycast.d < nearestObj.d) {
      nearestObj = raycast;
    }
  }

  for (int i = 0; i < SPHERE_LEN; i++) {
    ISObj raycast = intersectSphere(spheres[i], r, i);
    if(raycast.d < nearestObj.d) {
      nearestObj = raycast;
    }
  }

  return nearestObj;
}

Ray generatePerspectiveRay(in vec2 p) 
{
  // p is the current pixel coord, in [-1,1]
  float fov = 30.0;
  float D = 1.0 / tan(radians(fov));
  mat4 invView = inverse(_View);

  vec3 up = vec3(0.0, 1.0, 0.0);
  vec3 front = vec3(0.0, 0.0, -1.0);
  vec3 right = vec3(-1.0, 0.0, 0.0);

  return Ray((invView * vec4(0.0, 0.0, -D, 1.0)).xyz,
              mat3(invView) * normalize(p.x * right + p.y * up * aspectRatio + D * front));
}

vec3 computeNormal(in ISObj is,in Ray r) 
{
    if (is.t == OBJECT_TYPE_PLANE) {
      return planes[is.i].n;
    } else if(is.t == OBJECT_TYPE_SPHERE) {
      vec3 center = spheres[is.i].c;
      return normalize((r.ro + r.rd * is.d) - center);
    } else {
      return vec3(0.0, 1.0, 0.0);
    }
}

Ray lightRay(in vec3 ro, in Light l) {
  if (l.type == LIGHT_TYPE_DIRECTIONAL)
    return Ray(ro, normalize(-l.dir));
  else if (l.type == LIGHT_TYPE_POINT)
    return Ray(ro, normalize(l.center - ro));

  return Ray(ro, vec3(1.0));
}

float lightDist(in vec3 ro, in Light l) {
  if (l.type == LIGHT_TYPE_POINT)
    return length(l.center - ro);

  return DIST_MAX;
}


float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a      = roughness*roughness;
    float a2     = a*a;
    float NdotH  = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;

    float nom   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return nom / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float nom   = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return nom / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2  = GeometrySchlickGGX(NdotV, roughness);
    float ggx1  = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0)*pow((1.0 + 0.000001/*avoid negative approximation when cosTheta = 1*/) - cosTheta, 5.0);
}

vec3 computeReflectance(vec3 N, vec3 Ve, vec3 F0, vec3 albedo, vec3 L, vec3 H, vec3 light_col, float intensity, float metallic, float roughness)
{
    vec3 radiance =  light_col * intensity; //Incoming Radiance

    // cook-torrance brdf
    float NDF = DistributionGGX(N, H, roughness);
    float G   = GeometrySmith(N, Ve, L,roughness);
    vec3 F    = fresnelSchlick(max(dot(H, Ve), 0.0), F0);

    vec3 kS = F;
    vec3 kD = vec3(1.0) - kS;
    kD *= 1.0 - metallic;

    vec3 nominator    = NDF * G * F;
    float denominator = 4 * max(dot(N, Ve), 0.0) * max(dot(N, L), 0.0) + 0.00001/* avoid divide by zero*/;
    vec3 specular     = nominator / denominator;


    // add to outgoing radiance Lo
    float NdotL = max(dot(N, L), 0.0);
    vec3 diffuse_radiance = kD * (albedo)/ PI;

    return (diffuse_radiance + specular) * radiance * NdotL;
}

vec3 PBR(in HitSurface hit, in Ray r, in Light l) {
  vec3 ambient = vec3(0.03) * hit.material.color * hit.material.ao;

  vec3 F0 = vec3(0.04);
  F0 = mix(F0, hit.material.color, hit.material.metallic);
  vec3 N = normalize(hit.normal);
  vec3 Ve = normalize(r.ro - hit.point);

  float intensity = l.intensity;
  if (l.type == LIGHT_TYPE_POINT) {
    float ld = lightDist(hit.point, l);
    intensity = intensity / (ld * ld);
  }

  vec3 ldir = lightRay(hit.point, l).rd;
  vec3 H = normalize(Ve + ldir);
  return ambient + computeReflectance(N, Ve, F0, hit.material.color, ldir, H, l.color, intensity, hit.material.metallic, hit.material.roughness);
}


vec3 directIllumination(in HitSurface hit, in Ray r, inout float refl) {
  vec3 color = vec3(0.0);

  for (int i = 0; i < LIGHT_LEN; i++) {
    Ray lr = lightRay(hit.point, lights[i]);
    float ld = lightDist(hit.point, lights[i]);

    lr.ro = hit.point + 0.001 * hit.normal;

    ISObj io;
    io = intersectObjects(lr);

    if (io.t == OBJECT_TYPE_NONE || (io.d >= ld)) {
      color += PBR(hit, r, lights[i]);
    } else {
      color += vec3(0.03) * hit.material.color * hit.material.ao;
    }

    vec3 Ve = normalize(r.ro - hit.point);
    vec3 H = normalize(Ve + lr.rd);
    refl = length(fresnelSchlick(max(dot(H, Ve), 0.0), mix(vec3(0.04), hit.material.color, hit.material.metallic)));

  }
  return color;
}

vec3 trace(in Ray r) {
  vec3 accum = vec3(0.0);
  vec3 mask = vec3(1.0);
  int nb_refl = 2;
  float c_refl = 1.0;
  Ray ray = r;
  for (int i = 0; i <= nb_refl; i++) {
    ISObj io = intersectObjects(ray);
    if (io.t != OBJECT_TYPE_NONE) {
      PBRMat mat = materials[io.i];

      HitSurface hs = HitSurface(ray.ro + io.d * ray.rd, computeNormal(io, ray), mat);

      vec3 color = directIllumination(hs, ray, c_refl);
      accum = accum + mask * color;
      mask = mask * c_refl;
      ray = Ray(hs.point + 0.001 * hs.normal, reflect(ray.rd, hs.normal));
    } else {
      break;
    }
  }

  return accum;
}

void main() 
{
	Ray ray = generatePerspectiveRay(fragCoord);
  outColor = vec4(trace(ray), 1.0);

}
