#version 410
precision highp float;
uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
in vec3 in_position;
in vec3 in_normal;
in vec3 in_colour;
out vec3 normal;
out vec3 colour;
void main(void)
{
    normal = in_normal;
    colour = in_colour;
    gl_Position = projection_matrix * modelview_matrix * vec4(in_position, 1);
}