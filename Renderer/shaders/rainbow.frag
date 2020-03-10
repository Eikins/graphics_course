#version 330 core

in vec3 _VertexPosition;
in vec3 _PerVertexColor;

out vec4 outColor;

void main() {
    outColor = vec4(_PerVertexColor, 1.0);
}
