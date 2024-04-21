extends Node
## The store is a central place to store data, and emit a signal when that data changes.
##
## This autoload implements a pattern similar to Redux from the React world. It makes it easy to
## store global state, and respond to changes in that state. Using set_state will emit the
## state_changed signal, allowing other dependents to know when the relevant key changes.
## Out of the box, ViewController uses this to switch between the primary views that are children
## of UIRoot in the main scene.

const PersistentStore := preload("res://scripts/classes/PersistentStore.gd")

## Emitted when set_state is used to change the value of a key, passing the key changed and the new
## state. In most cases, should be used with a match statement on the state_key.
signal state_changed(state_key, substate)

var persistent_store: Resource
## The dictionary that stores the state manipulated by set_state.
##
## This state is public because there are cases where you want to read some subset of the state
## outside of the state_changed signal. In the _vast_ majority of cases, this should not be
## manipulated directly. Use set_state instead. For complex operations, prepare your new state
## locally, and then use set_state. If you must manipulate directly, ensure you call set_state
## for each key mutated, and pass the current value for that key.
var state: Dictionary

## state is initialized with this value at _init by default.
##
## Useful if you need to reset_store, or reset some part of your state to an initial value. In those
## cases, never pass a reference to _initial_state or some subset, _always_ use duplicate.
var _initial_state: Dictionary = {
	"client_view": ViewController.CLIENT_VIEWS.NONE,
	GameConstants.STORE_KEYS.GAME_STATE: GameConstants.GAME_STATES.GAME_OVER,
	"debug": OS.has_feature("editor")  # Debug will be true when running from editor, false in builds
}


func reset_store() -> void:
	state = _initial_state.duplicate(true)


func start_game() -> void:
	var _view_tween: Tween = ViewController.set_client_view(ViewController.CLIENT_VIEWS.NONE)

	await _view_tween.finished
	set_state(GameConstants.STORE_KEYS.GAME_STATE, GameConstants.GAME_STATES.GAME_STARTING)


func save_persistent_store() -> void:
	if ResourceSaver.save(persistent_store, ClientConstants.CLIENT_PERSISTENT_STORE_PATH) != OK:
		print("Failed to save persistent store")


## The main way to mutate the state of the store. By passing a state key and the new value, you set
## not only set that key to a new value, but state_changed is emitted, so dependents of that key
## can respond to changes.
func set_state(state_key: String, new_state) -> void:
	print("State changing: ", state_key, " - ", state[state_key], " -> ", new_state)
	state[state_key] = new_state
	state_changed.emit(state_key, state[state_key])


func _init():
	reset_store()


func _initialize():
	(
		(func(): ViewController.set_client_view(
			(
				ViewController.CLIENT_VIEWS.SPLASH
				if !state.debug
				else ViewController.CLIENT_VIEWS.MAIN_MENU
			)
		))
		. call_deferred()
	)


func _ready():
	if FileAccess.file_exists(ClientConstants.CLIENT_PERSISTENT_STORE_PATH):
		persistent_store = load(ClientConstants.CLIENT_PERSISTENT_STORE_PATH)

	if !persistent_store:
		persistent_store = PersistentStore.new()
		save_persistent_store()

	call_deferred("_initialize")
