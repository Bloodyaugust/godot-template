shader_type canvas_item;
render_mode blend_mix;

uniform bool use_mult;
uniform float key_distance : hint_range(0, 1);
uniform vec4 chroma_key : hint_color;
uniform vec4 color : hint_color;

void fragment() {
  COLOR = texture(TEXTURE, UV);
  
  if (distance(COLOR, chroma_key) < key_distance) {
    if (use_mult) {
      COLOR = COLOR * color;      
     }
    else {
      COLOR = color;
     }
   }
}