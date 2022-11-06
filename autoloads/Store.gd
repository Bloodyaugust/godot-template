extends Node

signal state_changed(state_key, substate)

var persistent_store:PersistentStore
var state: Dictionary = {
  "client_view": "",
  "game": "",
  "tower_selection": null,
  "tower_build_selection": null,
  "money": 0,
 }

func start_game() -> void:
  set_state("client_view", ClientConstants.CLIENT_VIEW_NONE)
  set_state("game", GameConstants.GAME_STARTING)
  set_state("tower_selection", null)
  set_state("tower_build_selection", null)
  set_state("money", 0)

func save_persistent_store() -> void:
  if ResourceSaver.save(persistent_store, ClientConstants.CLIENT_PERSISTENT_STORE_PATH) != OK:
    print("Failed to save persistent store")

func set_state(state_key: String, new_state) -> void:
  state[state_key] = new_state
  emit_signal("state_changed", state_key, state[state_key])
  print("State changed: ", state_key, " -> ", state[state_key])
  
func _initialize():
  set_state("client_view", ClientConstants.CLIENT_VIEW_SPLASH)
  set_state("game", GameConstants.GAME_OVER)
  set_state("tower_selection", null)
  set_state("tower_build_selection", null)
  set_state("money", 0)

func _ready():
  if FileAccess.file_exists(ClientConstants.CLIENT_PERSISTENT_STORE_PATH):
    persistent_store = load(ClientConstants.CLIENT_PERSISTENT_STORE_PATH)

  if !persistent_store:
    persistent_store = PersistentStore.new()
    save_persistent_store()

  call_deferred("_initialize")
