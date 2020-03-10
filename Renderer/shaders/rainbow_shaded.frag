#version 330 core

in vec3 _VertexPosition;
in vec3 _PerVertexColor;
in vec3 _VertexNormal;

uniform vec3 _LightDirection;

out vec4 outColor;

void main() {
    outColor = vec4(_PerVertexColor, 1.0) * dot(_VertexNormal, - _LightDirection);
}
