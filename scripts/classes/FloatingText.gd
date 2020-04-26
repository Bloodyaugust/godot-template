extends Node2D
class_name FloatingText

export var float_speed: float
export var float_margin: float
export var time_to_live: float

onready var label: Label = get_node("Label")

onready var _opacity_tween: Label = get_node("OpacityTween")
onready var _starting_x: float = position.x
onready var _time_left: float = time_to_live

func _process(delta):
  _time_left -= delta
  
  if _time_left <= 0:
    queue_free()
  else:
    position = Vector2(_starting_x + (sin(OS.get_ticks_msec() / 100.0) * float_margin), position.y - float_speed * delta)

func _ready():
  _opacity_tween.interpolate_property(label, "modulate", Color(1, 1, 1, 1), Color(1, 1, 1, 0), time_to_live, Tween.TRANS_LINEAR, Tween.EASE_IN)
  _opacity_tween.start()
