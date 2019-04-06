#version 410
precision highp float;
const vec3 ambient = vec3(0.1, 0.1, 0.1);
const vec3 lightVecNormalized = normalize(vec3(0.5, -0.5, 2.0));
const vec3 lightColor = vec3(0.9, 0.9, 0.7);
in vec3 normal;
out vec4 out_frag_color;
void main(void)
{
    if(normal[0] == 0 && normal[1] == 0 && normal[2] == 0){
         out_frag_color = vec4(0.87, 0.25, 0.96, 1.0);
    }else{
        float diffuse = clamp(dot(lightVecNormalized, normalize(normal)), 0.0, 1.0);
        out_frag_color = vec4(ambient + diffuse * lightColor, 1.0);
    }
}