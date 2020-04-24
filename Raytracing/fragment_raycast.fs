#version 330 core

uniform vec2 _MousePos;
uniform float _Time;
uniform float _AspectRatio;
uniform vec2 _ScreenSize;

in vec2 fragCoord;

out vec4 outColor;

#define DIST_MIN 1e-5
#define DIST_MAX 1e+5 

// ====================== FUNCTIONS ======================
#define OBJECT_TYPE_NONE -1
#define OBJECT_TYPE_PLANE 0
#define OBJECT_TYPE_SPHERE 1

struct Frame {
    vec3 X, Y, Z;
};

struct Ray {
    vec3 origin;
    vec3 dir;
};

struct Sphere 
{
	vec3 center;
	float radius;
};
    
struct Plane {
    vec3 normal;
    float offset;
};
    
// struct for remembering intersected object
struct ISObj 
{
	float dist;  // distance to the object
	int type;    // type (-1=nothing,0=plane, 1=sphere)
	int id;      // object ID
};
    
ISObj intersectPlane(in Plane p, in Ray r, in int id) {
    float t = - (p.offset + dot(r.origin, p.normal)) / dot(r.dir, p.normal);
    
    if (t < 0.0) {
        return ISObj(DIST_MAX, OBJECT_TYPE_NONE, -1);
    } else {
    	return ISObj(t, OBJECT_TYPE_PLANE, id);
    }

}
    
ISObj intersectSphere(in Sphere s, in Ray r, in int id) {
    vec3 offset = (r.origin - s.center);
	float a = dot(r.dir, r.dir);
    float b = 2.0 * dot(offset, r.dir);
    float c = dot(offset, offset) - s.radius * s.radius;
    
    float det = sqrt(b*b - 4.0*a*c);
    
    if (det < 0.0) {
        return ISObj(DIST_MAX, OBJECT_TYPE_NONE, -1);
    } else {
        float t = min(- b - det, - b + det) / (2.0 * a);
    	return ISObj(t, OBJECT_TYPE_SPHERE, id);
    }

}

vec3 computeSphereNormal(in Sphere s, in Ray r, in float dist) {
    return normalize((r.origin + r.dir * dist) - s.center);
}

// ====================== SCENE ======================
#define NEAR_DISTANCE 2.0
#define FAR_DISTANCE 40.0
Frame CAMERA = Frame(vec3(1, 0, 0), vec3(0, 1, 0), vec3(0, 0, 1));
vec3 EYE_POS = vec3(0.0, 1.0, -8.0);


const vec4 DEFAULT_COLOR = vec4(.45, .85, .92, 1);
const vec3 LIGHT_DIRECTION = normalize(vec3(1.0, 1.0, 1.0));

#define PLANE_LEN 6
#define SPHERE_LEN 3
const Plane[PLANE_LEN] planes = Plane[](
    Plane(vec3(0.0, 1.0, 0.0), 2.0),
	Plane(vec3(0.0, 0.0, -1.0), 10.0),
	Plane(vec3(-1.0, 0.0, 0.0), 10.0),
	Plane(vec3(1.0, 0.0, 0.0), 10.0),
	Plane(vec3(0.0, -1.0, 0.0), 10.0),
	Plane(vec3(0.0, 0.0, 1.0), 10.0)
);

const Sphere[SPHERE_LEN] spheres = Sphere[](
    Sphere(vec3(0.0, 4.0, 7.0), 1.0),
	Sphere(vec3(7.0, -1, 4.0), 2.0),
    Sphere(vec3(-6.0, 0.0, 6.0), 3.0));

ISObj raycast(in Ray ray) {
    ISObj nearestObj = ISObj(FAR_DISTANCE, -1, -1);
    
    for (int i = 0; i < PLANE_LEN; i++) {
   		ISObj raycast = intersectPlane(planes[i], ray, i);
        if(raycast.dist < nearestObj.dist) {
            nearestObj = raycast;
        }
    }
    for (int i = 0; i < SPHERE_LEN; i++) {
   		ISObj raycast = intersectSphere(spheres[i], ray, i);
        if(raycast.dist < nearestObj.dist) {
            nearestObj = raycast;
        }
    }
    return nearestObj;
}

vec3 paint(vec3 col, vec3 paintColor, float dist, float thickness) {
    return col = mix(col, paintColor, 1.0 - pow(smoothstep(0.0, thickness / _ScreenSize.x, dist), 1.0));
}

float circle(vec2 center, float radius, vec2 point) {
	return length(point - center) - radius;   
}

float segment(vec2 start, vec2 end, vec2 point) {
    vec2 offset = point - start;
    vec2 seg = end - start;
    float t = clamp(dot(offset, seg) / dot(seg, seg), 0.0, 1.0);
    return length(offset - seg * t);
}

void main()
{
    
    EYE_POS = EYE_POS + vec3(3.0 * cos(_Time), 1.0 * sin(_Time), 0.0);
      
    // Pixel coordinates mapped to [-aspectRatio, aspectRatio] x [-1, 1]
    vec2 uv = fragCoord * vec2(1.0, _AspectRatio);

    Ray ray = Ray(EYE_POS, normalize(vec3(uv.x * CAMERA.X + uv.y * CAMERA.Y + NEAR_DISTANCE * CAMERA.Z)));

    ISObj nearestObj = raycast(ray);
    
    if (_MousePos.x < fragCoord.x) {
      // Depth
      outColor = vec4(nearestObj.dist, nearestObj.dist, nearestObj.dist, FAR_DISTANCE) / FAR_DISTANCE;
    } else {
      // Normal
      vec3 normal;

      if (nearestObj.type == OBJECT_TYPE_PLANE) {
          normal = planes[nearestObj.id].normal;
      } else if (nearestObj.type == OBJECT_TYPE_SPHERE) {
          normal = computeSphereNormal(spheres[nearestObj.id], ray, nearestObj.dist);
      } else {
          normal = vec3(0.0, 0.0, 0.0);
      }

      outColor = vec4(normal, 1.0);
    }

    outColor = vec4(paint(outColor.xyz, vec3(1.0), abs(_MousePos.x - fragCoord.x), 8.0), 1.0);
    outColor = vec4(paint(outColor.xyz, vec3(1.0), circle(vec2(_MousePos.x, 0), 0.03, uv), 8.0), 1.0);
    outColor = vec4(paint(outColor.xyz, vec3(0.2, 0.16, 0.16), segment(vec2(_MousePos.x - 0.01, 0.01), vec2(_MousePos.x - 0.02, 0), uv), 4.0), 1.0);
    outColor = vec4(paint(outColor.xyz, vec3(0.2, 0.16, 0.16), segment(vec2(_MousePos.x - 0.01, -0.01), vec2(_MousePos.x - 0.02, 0), uv), 4.0), 1.0);
    outColor = vec4(paint(outColor.xyz, vec3(0.2, 0.16, 0.16), segment(vec2(_MousePos.x + 0.01, 0.01), vec2(_MousePos.x + 0.02, 0), uv), 4.0), 1.0);
    outColor = vec4(paint(outColor.xyz, vec3(0.2, 0.16, 0.16), segment(vec2(_MousePos.x + 0.01, -0.01), vec2(_MousePos.x + 0.02, 0), uv), 4.0), 1.0);
    
}