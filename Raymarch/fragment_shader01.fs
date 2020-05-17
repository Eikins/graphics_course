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

#define OBJECT_TYPE_NONE -1
#define OBJECT_TYPE_PLANE 0
#define OBJECT_TYPE_SPHERE 1
#define OBJECT_TYPE_BOX 2
#define OBJECT_TYPE_SHOP 3

struct Plane {
  vec3 n; // normal
  float d; // offset
};

// box structure
struct Box 
{
  vec3 b; // up_right_corner_length
  vec3 c; // center
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

struct Shape {
  int type;
  int id;
};

#define SDF_OP_UNION 0
#define SDF_OP_SUBTRACT 1
#define SDF_OP_INTERSECTION 2
#define SDF_OP_BLEND 3

struct ShapeOp {
  Shape shape;
  int op;
  int next;
};

// SCENE
// #define SHOP_LEN 1
// ShapeOp shops[SHOP_LEN] = ShapeOp[](
//   ShapeOp(Shape(OBJECT_TYPE_SPHERE, 0), SDF_OP_UNION, -1)
// );

#define SHOP_LEN 10
ShapeOp shops[SHOP_LEN] = ShapeOp[](
  ShapeOp(Shape(OBJECT_TYPE_SPHERE, 0), SDF_OP_BLEND, 1),
  ShapeOp(Shape(OBJECT_TYPE_SPHERE, 1), SDF_OP_BLEND, 2),
  ShapeOp(Shape(OBJECT_TYPE_SPHERE, 2), SDF_OP_BLEND, 3),
  ShapeOp(Shape(OBJECT_TYPE_SPHERE, 3), SDF_OP_INTERSECTION, 4),
  ShapeOp(Shape(OBJECT_TYPE_SPHERE, 4), SDF_OP_BLEND, 5),
  ShapeOp(Shape(OBJECT_TYPE_SPHERE, 5), SDF_OP_BLEND, 6),
  ShapeOp(Shape(OBJECT_TYPE_BOX, 0), SDF_OP_BLEND, 7),
  ShapeOp(Shape(OBJECT_TYPE_BOX, 1), SDF_OP_BLEND, 8),
  ShapeOp(Shape(OBJECT_TYPE_BOX, 2), SDF_OP_SUBTRACT, -1),
  ShapeOp(Shape(OBJECT_TYPE_PLANE, 0), SDF_OP_UNION, -1)
);


#define ROOT_LEN 2
int roots[ROOT_LEN] = int[](
  0,
  9
);

#define SPHERE_LEN 6

Sphere spheres[SPHERE_LEN] = Sphere[](
  Sphere(vec3(0.5, -2.5 * cos(time * 0.1), 0), 0.5),
  Sphere(vec3(0.7, -2.5 * cos(time * 0.2), 0), 0.5),
  Sphere(vec3(0.9, -2.5 * cos(time * 0.3), 0), 0.5),
  Sphere(vec3(-0.9, 2.5 * cos(time * 0.4), 0), 0.5),
  Sphere(vec3(-0.7, 2.5 * cos(time * 0.5), 0), 0.5),
  Sphere(vec3(-0.5, 2.5 * cos(time * 0.6), 0), 0.5)
);

#define BOX_LEN 3
Box boxes[BOX_LEN] = Box[](
  Box(vec3(1, 1, 1), vec3(0, -3, 0)),
  Box(vec3(1, 1, 1), vec3(0, 3, 0)),
  Box(vec3(0.5, 0.5, 0.5), vec3(0, 0, 0))
);

#define PLANE_LEN 6
Plane planes[PLANE_LEN] = Plane[](
  Plane(vec3(0, 1, 0), 5.0),
  Plane(vec3(0, -1, 0), 5.0),
  Plane(vec3(0, 0, 1), 5.0),
  Plane(vec3(0, 0, -1), 15.0),
  Plane(vec3(1, 0, 0), 5.0),
  Plane(vec3(-1, 0, 0), 5.0)
);

#define LIGHT_TYPE_DIRECTIONAL 0
#define LIGHT_TYPE_POINT 1

struct Light {
  int type;
  vec3 dir;
  vec3 center;
  float intensity;
  vec3 color;
};


const PBRMat[7] materials = PBRMat[](
  PBRMat(vec3(1.0, 0.0, 0.0), 1.0, 0.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 0.1, 1.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 0.3, 1.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 0.5, 1.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 0.7, 1.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 0.9, 1.0, 1.0),
  PBRMat(vec3(0.1, 0.2, 0.8), 1.0, 1.0, 1.0)
);

#define LIGHT_LEN 2
const Light[LIGHT_LEN] lights = Light[](
  Light(LIGHT_TYPE_POINT, vec3(1, 1, -1), vec3(-3, 1, -3), 80.0, vec3(1.0)),
  Light(LIGHT_TYPE_POINT, vec3(-1, 1, 1), vec3(3, 1, 3), 80.0, vec3(1.0))
);


// SHAPE SDF
float SDFPlane(in Plane plane, in vec3 point) {
  return plane.d + dot(normalize(plane.n), point);
}

float SDFSphere(in Sphere sphere, in vec3 point) {
  return length(sphere.c - point) - sphere.r;
}

float SDFBox(in Box box, in vec3 point) {
  vec3 d = abs(point - box.c) - box.b;
  return length(max(d, 0)) + min(max(max(d.x, d.y), d.z), 0);
}

// OPERATIONS
float opSDF(in float d1, in float d2, int op) {
  if (op == SDF_OP_UNION) {
    return min(d1, d2);
  } else if (op == SDF_OP_SUBTRACT) {
    return max(d1, -d2);
  } else if (op == SDF_OP_INTERSECTION) {
    return max(d1, d2);
  } else if (op == SDF_OP_BLEND) {
    float k = 1.0;
    float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return mix(d2, d1, h) - k * h * (1.0 - h);
  }
}


float getBasicSDF(int type, int id, in vec3 point) {
  if (type == OBJECT_TYPE_PLANE) {
    return SDFPlane(planes[id], point);
  } else if (type == OBJECT_TYPE_SPHERE) {
    return SDFSphere(spheres[id], point);
  } else if (type == OBJECT_TYPE_BOX) {
    return SDFBox(boxes[id], point);
  }
  return 0.0;
}

float shapeSDF(int shop_id, vec3 point) {
  int id = shop_id;
  float sdf = getBasicSDF(
    shops[id].shape.type,
    shops[id].shape.id,
    point);

  int next = shops[id].next;

  while (next != -1) {
    float sdfNext = getBasicSDF(
      shops[next].shape.type,
      shops[next].shape.id,
      point);
    sdf = opSDF(sdf, sdfNext, shops[id].op);
    id = next;
    next = shops[id].next;
  }
  return sdf;
}

float getSDF(int type, int id, in vec3 point) {
  if (type != OBJECT_TYPE_SHOP) {
    return getBasicSDF(type, id, point);
  } else {
    return shapeSDF(id, point);
  }
}

#define EPS_GRAD 0.001
vec3 computeSDFGrad(in ISObj io, in vec3 point) {
    vec3 p_x_p = point + vec3(EPS_GRAD, 0, 0);
    vec3 p_x_m = point - vec3(EPS_GRAD, 0, 0);
    vec3 p_y_p = point + vec3(0, EPS_GRAD, 0);
    vec3 p_y_m = point - vec3(0, EPS_GRAD, 0);
    vec3 p_z_p = point + vec3(0, 0, EPS_GRAD);
    vec3 p_z_m = point - vec3(0, 0, EPS_GRAD);

    float sdf_x_p = getSDF(io.t, io.i, p_x_p);
    float sdf_x_m = getSDF(io.t, io.i, p_x_m);
    float sdf_y_p = getSDF(io.t, io.i, p_y_p);
    float sdf_y_m = getSDF(io.t, io.i, p_y_m);
    float sdf_z_p = getSDF(io.t, io.i, p_z_p);
    float sdf_z_m = getSDF(io.t, io.i, p_z_m);

    return vec3(
      sdf_x_p - sdf_x_m,
      sdf_y_p - sdf_y_m,
      sdf_z_p - sdf_z_m
    ) / (2.0 * EPS_GRAD);
}

ISObj sceneSDF(in vec3 point) {
  ISObj nearest = ISObj(DIST_MAX, OBJECT_TYPE_NONE, -1);
  for (int i = 0; i < ROOT_LEN; i++) {
    float dist = shapeSDF(roots[i], point);
    if (dist < nearest.d) {
      nearest = ISObj(dist, OBJECT_TYPE_SHOP, roots[i]);
    }
  }
  return nearest;
}

ISObj rayMarch(in Ray r) {
  int nb_step = 300;

  float depth = 0.0;
  float march_accuracy = 0.001;
  float march_step_fact = 0.01;

  for (int i = 0; i < nb_step; i++) {
    ISObj io = sceneSDF(r.ro + depth * r.rd);
    if (io.d <= march_accuracy) {
      return ISObj(depth, io.t, io.i);
    }
  
    // Spherical Marching
    // Take the closest distance...
    depth += io.d;
    // Step by Step MArching
    // depth += march_step_fact;

    if (depth >= DIST_MAX) {
      return ISObj(DIST_MAX, -1, -1);
    }
  }
  return ISObj(DIST_MAX, -1, -1);
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
    io = rayMarch(lr);

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

float ambientOcclusion(vec3 point, vec3 normal, float dist, float steps) {
  float ao = 1.0;
  while (steps > 0.0) {
    float sdf = sceneSDF(point + normal * steps * dist).d;
    ao -= pow(steps * dist - sdf, 2.0) / steps;
    steps--;
  }

  return ao;
}

vec3 march(in Ray r) {
  vec3 accum = vec3(0.0);
  vec3 mask = vec3(1.0);
  int nb_refl = 1;
  float c_refl = 0.3;
  Ray ray = r;
  for (int i = 0; i <= nb_refl; i++) {
    ISObj io = rayMarch(ray);
    if (io.t != OBJECT_TYPE_NONE) {
      PBRMat mat = PBRMat(vec3(.9,.1,.1),0.4,0.9,0.3);
      vec3 N = normalize(computeSDFGrad(io, ray.ro + ray.rd * io.d));
      HitSurface hs = HitSurface(ray.ro + ray.rd * io.d, N, mat);
      vec3 color = directIllumination(hs, ray, c_refl);
      
      float ao = pow(ambientOcclusion(hs.point, hs.normal, 0.015, 20), 40);
      
      accum += mask * color * ao;
      mask *= c_refl;
      ray = Ray(hs.point + 0.001 * hs.normal, reflect(ray.rd, N));
    } else {
      break;
    }
  }

  //HDR
  accum = accum / (accum+ vec3(1.0));
  //Gamma
  float gamma = 1.4;
  accum = pow(accum, vec3(1.0/gamma));

  return accum;
}

void main() 
{
	Ray ray = generatePerspectiveRay(fragCoord);
  outColor = vec4(march(ray), 1.0);

}
