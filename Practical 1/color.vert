#version 330 core

uniform mat4 _ProjectionViewMatrix;


layout(location = 0) in vec3 _position;
layout(location = 1) in vec3 _normal;

out vec3 _VertexNormal;

void main() {
    gl_Position = _ProjectionViewMatrix * vec4(_position, 1);
    _VertexNormal = _normal;
}
