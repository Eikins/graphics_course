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

#define FLOAT_MAX 1e+5

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
    
struct PointLight {
	vec3 position;
    vec3 color;
    float intensity;
};
    
// Intersected Object
struct ISObj 
{
	float dist;
	int type;
	int id;
    vec3 normal;
};
    
struct PhongMat {
    vec3 diffuse;
    float ambient;
    vec3 specular;
    float shininess;
    float reflexivity;
};
    
vec3 computeSphereNormal(in Sphere s, in Ray r, in float dist) {
    return normalize((r.origin + r.dir * dist) - s.center);
}
    
ISObj intersectPlane(in Plane p, in Ray r, in int id) {
    float t = - (p.offset + dot(r.origin, p.normal)) / dot(r.dir, p.normal);
    
    if (t > 0.0) {
        return ISObj(t, OBJECT_TYPE_PLANE, id, p.normal);
    } else {
        return ISObj(FLOAT_MAX, OBJECT_TYPE_NONE, -1, vec3(0.0, 0.0, 0.0));
    }

}
    
ISObj intersectSphere(in Sphere s, in Ray r, in int id) {
    vec3 offset = (r.origin - s.center);
	float a = dot(r.dir, r.dir);
    float b = 2.0 * dot(offset, r.dir);
    float c = dot(offset, offset) - s.radius * s.radius;
    
    float det = sqrt(b*b - 4.0*a*c);
    
    if (det < 0.0) {
        return ISObj(FLOAT_MAX, OBJECT_TYPE_NONE, -1, vec3(0.0, 0.0, 0.0));
    } else {
        float t = min(- b - det, - b + det) / (2.0 * a);
        if (t > 0.0) {
        	return ISObj(t, OBJECT_TYPE_SPHERE, id, computeSphereNormal(s, r, t));  
        } else {
            return ISObj(FLOAT_MAX, OBJECT_TYPE_NONE, -1, vec3(0.0, 0.0, 0.0));        
        }

    }

}

// ====================== SCENE ======================
// ===== PARAM =====
#define NB_STEPS 5

// ===== SCENE =====
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
const PhongMat[PLANE_LEN] planeMats = PhongMat[](
    PhongMat(vec3(0.1, 0.1, 0.1), 0.5, vec3(1.0, 1.0, 1.0) * 0.2, 5.0, 0.1),
    PhongMat(vec3(0.75, 0.31, 0.31), 0.5, vec3(1.0, 1.0, 1.0) * 0.2, 4.0, 0.1),
    PhongMat(vec3(0.0, 0.0, 0.0), 0.5, vec3(1.0, 1.0, 1.0) * 0.2, 4.0, 1.0),
    PhongMat(vec3(0.31, 0.31, 0.75), 0.5, vec3(1.0, 1.0, 1.0) * 0.2, 4.0, 0.1),
    PhongMat(vec3(0.55, 0.61, 0.71), 0.5, vec3(1.0, 1.0, 1.0) * 0.2, 4.0, 0.1),
    PhongMat(vec3(0.31, 0.55, 0.31), 0.5, vec3(1.0, 1.0, 1.0) * 0.0, 4.0, 0.1)
);


const Sphere[SPHERE_LEN] spheres = Sphere[](
    Sphere(vec3(0.0, 4.0, 7.0), 1.0),
	Sphere(vec3(7.0, -1, 4.0), 2.0),
    Sphere(vec3(-6.0, 0.0, 6.0), 3.0));
const PhongMat[SPHERE_LEN] sphereMats = PhongMat[](
    PhongMat(vec3(0.0, 0.6, 0.86), 0.2, vec3(1.0, 1.0, 1.0) * 0.2, 3.0, 0.25),
    PhongMat(vec3(0.24, 0.54, 0.28), 0.4, vec3(1.0, 1.0, 1.0) * 0.2, 2.0, 0.1),
    PhongMat(vec3(0, 0, 0), 0.2, vec3(0.0, 0.0, 0.0), 3.0, 1.0));

const PointLight[1] pointLights = PointLight[](
    PointLight(vec3(-3.0, 2.0, 2.0), vec3(1.0, 0.0, 1.0), 1.0));

const vec3 LIGHT_DIRECTION = normalize(vec3(1.0, 1.0, 1.0));

// ===== CAMERA =====
#define NEAR_DISTANCE 2.0
#define FAR_DISTANCE 40.0

Frame CAMERA = Frame(vec3(1, 0, 0), vec3(0, 1, 0), vec3(0, 0, 1));
vec3 EYE_POS = vec3(0.0, 1.0, -8.0);
const vec4 BACKGROUND_COLOR = vec4(.45, .85, .92, 1);

ISObj raycast(in Ray ray) {
    ISObj nearestObj = ISObj(FAR_DISTANCE, -1, -1, vec3(0.0, 0.0, 0.0));
    
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

PhongMat getMat(in ISObj hit) {
     if (hit.type == OBJECT_TYPE_SPHERE) {
   		return sphereMats[hit.id];
    } else if (hit.type == OBJECT_TYPE_PLANE) {
        return planeMats[hit.id];
    }   
    
}

vec3 phong(in ISObj hit, in PhongMat mat, in vec3 lightDir, in vec3 viewDir, in vec3 reflection) {
    return mat.ambient * mat.diffuse
        + mat.diffuse * max(0.0, dot(hit.normal, lightDir))
        + mat.specular * pow(max(0.0, dot(reflection, viewDir)), mat.shininess);
}

vec3 globalIllumination(in Ray ray, in ISObj hit, in PhongMat mat, out vec3 hitPos, out vec3 reflection) {
    for (int i = 0; i < 1; i++) {
        // Intersection pos
        vec3 inPos = vec3(hit.dist * ray.dir + ray.origin);
        vec3 toLight = pointLights[i].position - inPos;
        vec3 lightDir = normalize(toLight);
        
        vec3 viewDir = normalize(ray.origin - inPos);
        reflection = -reflect(viewDir, hit.normal);
        hitPos = inPos + hit.normal * 1e-5;
        
        Ray shadowRay = Ray(hitPos, lightDir);
        ISObj shadowHit = raycast(shadowRay);
        
        if(shadowHit.dist > length(toLight)) {
        	return phong(hit, mat, lightDir, viewDir, -reflect(lightDir, hit.normal));
        } else {
        	return mat.diffuse * mat.ambient;   
        }
    }

}

vec4 trace(in Ray ray) {

    vec3 accum = vec3(0.0, 0.0, 0.0);
    vec3 mask = vec3(1.0, 1.0, 1.0);
    
    for (int i = 0; i < NB_STEPS; i++) {
    	ISObj hit = raycast(ray); 
        
    	if (hit.type == OBJECT_TYPE_NONE) {
    		return vec4(accum, 1.0);
    	} else {
            
            PhongMat mat = getMat(hit);
            
            vec3 reflection, hitPos;
			vec3 color = globalIllumination(ray, hit, mat, hitPos, reflection);
            accum = accum + mask * color;
            mask = mask * mat.reflexivity;
            ray = Ray(hitPos, reflection);
    	}
         
    }
    
    return vec4(accum, 1.0);

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
    // Move Camera
    EYE_POS = EYE_POS + vec3(3.0 * cos(_Time), 1.0 * sin(_Time), 0.0);
    
    // Setup Viewport
    // Pixel coordinates mapped to [-1, 1] x [-aspectRatio, aspectRatio]
    vec2 uv = fragCoord * vec2(1.0, _AspectRatio);

    if (_MousePos.x > fragCoord.x) {
        // No AA
	    Ray ray = Ray(EYE_POS, normalize(vec3(uv.x * CAMERA.X + uv.y * CAMERA.Y + NEAR_DISTANCE * CAMERA.Z)));
        outColor = trace(ray);
    } else {
        // MSAA
        vec2 pixel = vec2(2.0, 2.0 * _AspectRatio) / _ScreenSize;
	    Ray ray00 = Ray(EYE_POS, normalize(vec3((uv.x - pixel.x / 4.0) * CAMERA.X + (uv.y - pixel.y / 4.0) * CAMERA.Y + NEAR_DISTANCE * CAMERA.Z)));
	    Ray ray01 = Ray(EYE_POS, normalize(vec3((uv.x + pixel.x / 4.0) * CAMERA.X + (uv.y - pixel.y / 4.0) * CAMERA.Y + NEAR_DISTANCE * CAMERA.Z)));
	    Ray ray10 = Ray(EYE_POS, normalize(vec3((uv.x - pixel.x / 4.0) * CAMERA.X + (uv.y + pixel.y / 4.0) * CAMERA.Y + NEAR_DISTANCE * CAMERA.Z)));
	    Ray ray11 = Ray(EYE_POS, normalize(vec3((uv.x + pixel.x / 4.0) * CAMERA.X + (uv.y + pixel.y / 4.0) * CAMERA.Y + NEAR_DISTANCE * CAMERA.Z)));
        outColor = 0.25 * (trace(ray00) + trace(ray01) + trace(ray10) + trace(ray11));
    }

    outColor = vec4(paint(outColor.xyz, vec3(1.0), abs(_MousePos.x - fragCoord.x), 8.0), 1.0);
    outColor = vec4(paint(outColor.xyz, vec3(1.0), circle(vec2(_MousePos.x, 0), 0.03, uv), 8.0), 1.0);
    outColor = vec4(paint(outColor.xyz, vec3(0.2, 0.16, 0.16), segment(vec2(_MousePos.x - 0.01, 0.01), vec2(_MousePos.x - 0.02, 0), uv), 4.0), 1.0);
    outColor = vec4(paint(outColor.xyz, vec3(0.2, 0.16, 0.16), segment(vec2(_MousePos.x - 0.01, -0.01), vec2(_MousePos.x - 0.02, 0), uv), 4.0), 1.0);
    outColor = vec4(paint(outColor.xyz, vec3(0.2, 0.16, 0.16), segment(vec2(_MousePos.x + 0.01, 0.01), vec2(_MousePos.x + 0.02, 0), uv), 4.0), 1.0);
    outColor = vec4(paint(outColor.xyz, vec3(0.2, 0.16, 0.16), segment(vec2(_MousePos.x + 0.01, -0.01), vec2(_MousePos.x + 0.02, 0), uv), 4.0), 1.0);
}

