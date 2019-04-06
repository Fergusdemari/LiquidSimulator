#version 410
precision highp float;
uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
in vec3 in_position;
in vec3 in_normal;
out vec3 normal;
void main(void)
{
    normal = in_normal;
    gl_Position = projection_matrix * modelview_matrix * vec4(in_position, 1);
}