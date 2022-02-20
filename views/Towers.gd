extends Control

const _tower_component:PackedScene = preload("res://views/components/Tower.tscn")

@onready var _tower_list:VBoxContainer = find_node("TowerList")
@onready var _towers:Array = Depot.get_lines("towers")

func _on_store_state_changed(state_key:String, substate) -> void:
  match state_key:
    "tower_selection":
      match substate:
        null:
          visible = true
        _:
          visible = false

func _ready():
  Store.state_changed.connect(_on_store_state_changed)
  
  for _tower in _towers:
    var _new_tower_component:Control = _tower_component.instantiate()
    
    _new_tower_component.data = _tower
    _tower_list.add_child(_new_tower_component)
