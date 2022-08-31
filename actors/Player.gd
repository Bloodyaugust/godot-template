extends Sprite

export var data: Resource

func _process(delta):
  var _movement: Vector2 = Vector2.ZERO
  
  _movement += Vector2(Input.get_axis("move_left", "move_right"), Input.get_axis("move_up", "move_down")).normalized() * data.speed * delta
  
  global_position += _movement
