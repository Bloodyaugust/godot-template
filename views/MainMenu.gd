extends Control

onready var _animation_player:AnimationPlayer = find_node("AnimationPlayer")
onready var _play_button: Button = find_node("Play")

func _on_play_button_pressed() -> void:
  Store.set_state("client_view", ClientConstants.CLIENT_VIEW_NONE)

func _on_state_changed(state_key: String, substate):
  match state_key:
    "client_view":
      match substate:
        ClientConstants.CLIENT_VIEW_MAIN_MENU:
          visible = true
          _animation_player.play("ui_show")
        _:
          _animation_player.play_backwards("ui_show")
          yield(_animation_player, "animation_finished")
          visible = false

func _ready():
  _play_button.connect("pressed", self, "_on_play_button_pressed")

  Store.connect("state_changed", self, "_on_state_changed")
