extends Node

enum CLIENT_VIEWS {
	ABOUT,
	ACHIEVEMENTS,
	CREDITS,
	GAME_OVER,
	MAIN_MENU,
	SPLASH,
	NONE,
}

enum TRANSITION_TYPES {
	FADE,
	SLIDE,
}

var view_instances: Dictionary = {}
var view_transitions: Dictionary = {}

@onready var _ui_root: CanvasLayer = get_tree().get_root().find_child("UIRoot", true, false)


func register_view(
	view: CLIENT_VIEWS, instance: Control, transition_type: TRANSITION_TYPES
) -> void:
	view_instances[view] = instance
	view_transitions[view] = transition_type
	view_transitions[instance] = transition_type


func set_client_view(view: CLIENT_VIEWS) -> Tween:
	var _view_tween: Tween = get_tree().create_tween().set_trans(Tween.TRANS_QUAD)
	if GDUtil.reference_safe(_ui_root):
		var _old_instance: CLIENT_VIEWS = Store.state.client_view

		if _old_instance != CLIENT_VIEWS.NONE:
			match view_transitions[_old_instance]:
				TRANSITION_TYPES.SLIDE:
					_view_tween.tween_property(
						view_instances[_old_instance],
						"position",
						Vector2(0, -get_viewport().get_visible_rect().size.y),
						0.5
					)

				TRANSITION_TYPES.FADE:
					_view_tween.tween_property(
						view_instances[_old_instance], "modulate", Color.TRANSPARENT, 0.5
					)

			_view_tween.tween_callback(func(): view_instances[_old_instance].visible = false)

		Store.set_state("client_view", view)

		if view != CLIENT_VIEWS.NONE:
			view_instances[view].visible = true

			match view_transitions[view]:
				TRANSITION_TYPES.SLIDE:
					view_instances[view].position = Vector2(
						0, -get_viewport().get_visible_rect().size.y
					)
					_view_tween.tween_property(view_instances[view], "position", Vector2(0, 0), 0.5)

				TRANSITION_TYPES.FADE:
					view_instances[view].modulate = Color.TRANSPARENT
					_view_tween.tween_property(view_instances[view], "modulate", Color.WHITE, 0.5)

	return _view_tween
