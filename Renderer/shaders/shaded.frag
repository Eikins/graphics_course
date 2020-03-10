#version 330 core

uniform vec3 _CameraPos;
uniform vec3 _LightDirection;

in vec3 _VertexNormal;
in vec3 _VertexPosition;

out vec4 outColor;

void main() {
    float fresnel = clamp(1.0 - dot((_CameraPos - _VertexPosition), _VertexNormal), 0.0, 1.0);
    fresnel = pow(fresnel, 4.0);
    outColor = vec4(0.6, 0, 0, 1) * (dot(_VertexNormal, - _LightDirection)) + fresnel * vec4(0.0, 0.0, 0.2, 1.0);
}
