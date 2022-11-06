extends Camera2D

@export var camera_follow_speed: float
@export var camera_target_speed: float
@export var camera_zoom_speed: float
@export var debug: bool
@export var key_movement: bool
@export var follow_curve: Curve
@export var follow_delta: float
@export var mouse_drag: bool
@export var zoom_follow_scalar: float
@export var zoom_min: float
@export var zoom_max: float

var _camera_last_position: Vector2
var _camera_last_zoom: Vector2
var _drag_relative: Vector2
var _dragging: bool
var _position_tween: float
var _target: Node2D
var _target_last_position: Vector2
var _target_zoom: Vector2

func set_target_position(new_target_position: Vector2) -> void:
  _target.global_position = new_target_position

func _exit_tree():
  _target.queue_free()

func _handle_keys() -> void:
  if key_movement:
    if Input.is_action_pressed("ui_left"):
      _target.translate(Vector2.LEFT * camera_target_speed * get_process_delta_time())
    if Input.is_action_pressed("ui_right"):
      _target.translate(Vector2.RIGHT * camera_target_speed * get_process_delta_time())
    if Input.is_action_pressed("ui_up"):
      _target.translate(Vector2.UP * camera_target_speed * get_process_delta_time())
    if Input.is_action_pressed("ui_down"):
      _target.translate(Vector2.DOWN * camera_target_speed * get_process_delta_time())

func _on_target_draw():
  _target.draw_arc(_target.global_position, 100, 0, PI * 2, 32, Color.GREEN)

func _reset_follow() -> void:
  _camera_last_position = global_position
  _position_tween = clamp(camera_follow_speed, 0, 1)
  _target_last_position = _target.global_position
  _camera_last_zoom = Vector2(zoom)

func _process(delta):
  _handle_keys()

  if _target_last_position.distance_to(_target.global_position) >= follow_delta:
    _reset_follow()

  if mouse_drag && _dragging:
    _target.global_position += _drag_relative * delta * zoom.length()

  var _interpolated_position_tween: float = follow_curve.sample(_position_tween)

  global_position = global_position.lerp(_target_last_position, _interpolated_position_tween)
  zoom = zoom.lerp(_target_zoom, _interpolated_position_tween)

  if debug:
    queue_redraw()

func _ready():
  _target = Node2D.new()

  _target.name = "BetterCam Target"
  _target.global_position = global_position
  _target.z_index = 999
  if debug:
    _target.connect("draw", self._on_target_draw)

  get_tree().get_root().call_deferred("add_child", _target)

  _target_zoom = Vector2(zoom)

func _unhandled_input(event):
  if event is InputEventMouseButton:
    if event.button_index == MOUSE_BUTTON_WHEEL_UP:
      _target_zoom = _target_zoom - Vector2(camera_zoom_speed, camera_zoom_speed)
    if event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
      _target_zoom = _target_zoom + Vector2(camera_zoom_speed, camera_zoom_speed)

    if event.button_index == MOUSE_BUTTON_WHEEL_UP || event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
      _target.global_position = _target.global_position.lerp(get_global_mouse_position(), (1 - (zoom.x / zoom_max)) * zoom_follow_scalar)
      _target_zoom = Vector2(clamp(_target_zoom.x, zoom_min, zoom_max), clamp(_target_zoom.y, zoom_min, zoom_max))

    if mouse_drag:
      if event.button_index == MOUSE_BUTTON_RIGHT:
        if event.pressed:
          _dragging = true
          _drag_relative = Vector2()
        else:
          _dragging = false

  if mouse_drag && _dragging && event is InputEventMouseMotion:
    _drag_relative = _drag_relative + event.relative
