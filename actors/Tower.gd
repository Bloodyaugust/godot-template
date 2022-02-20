extends Node2D

const SELECTED_MODULATE:Color = Color(0.88627451658249, 1, 0.5137255191803)
const IDLE_MODULATE:Color = Color.WHITE

var data:Dictionary

@onready var _name:Label = find_node("Name")
@onready var _area2d:Area2D = find_node("Area2D")

func _on_area2d_input_event(_viewport:Node, event:InputEvent, _shape_index:int) -> void:
  if event is InputEventMouseButton && event.button_index == MOUSE_BUTTON_LEFT && event.is_pressed():
    Store.set_state("tower_selection", self)

func _on_store_state_changed(state_key:String, substate) -> void:
  match state_key:
    "tower_selection":
      if self == substate:
        modulate = SELECTED_MODULATE
      else:
        modulate = IDLE_MODULATE

func _ready():
  Store.state_changed.connect(_on_store_state_changed)
  (func(parent): parent._area2d.input_event.connect(parent._on_area2d_input_event)).call_deferred(self)
  _name.text = data.id
