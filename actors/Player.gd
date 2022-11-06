extends Sprite2D

@export var data: PlayerData

func _process(delta):
  var _movement_input: Vector2 = Vector2(Input.get_axis("move_left", "move_right"), Input.get_axis("move_up", "move_down")).normalized()

  var _movement: Vector2 = _movement_input * delta * data.move_speed

  global_position = global_position + _movement
