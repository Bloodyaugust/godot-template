extends Control

@onready var _animation_player:AnimationPlayer = %MainMenuAnimationPlayer
@onready var _play_button: Button = %Play

func _on_play_button_pressed() -> void:
  Store.start_game()

func _on_state_changed(state_key: String, substate):
  match state_key:
    "client_view":
      match substate:
        ClientConstants.CLIENT_VIEW_MAIN_MENU:
          visible = true
          _animation_player.play("ui_show")
        _:
          _animation_player.play_backwards("ui_show")
          await _animation_player.animation_finished
          visible = false

func _ready():
  _play_button.connect("pressed", self._on_play_button_pressed)

  Store.state_changed.connect(self._on_state_changed)
