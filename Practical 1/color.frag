#version 330 core

uniform vec3 _LightDirection;

in vec3 _VertexNormal;

out vec4 outColor;

void main() {
    outColor = vec4(1, 0, 0, 1) * dot(_VertexNormal, - _LightDirection);
}
