shader_type canvas_item;
render_mode blend_mix;

uniform float amount : hint_range(0, 1);
uniform vec4 color : hint_color;

void fragment() {
  COLOR = texture(TEXTURE, UV);
  
  if (amount > 0.0) {
    vec3 mixed_colors = mix(vec3(1, 1, 1), vec3(color.r, color.g, color.b), amount);
    COLOR = COLOR * vec4(mixed_colors.r, mixed_colors.g, mixed_colors.b, 1);
  }
}

void vertex() {
  float sin_time = sin(TIME * (64.0 * amount));
  
  VERTEX = VERTEX + vec2(sin_time * amount * 3.0, (sin_time * amount) / 2.0);
}