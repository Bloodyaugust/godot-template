extends Node
class_name GDUtil
## A collection of common operations on nodes, such as queue_free()-ing all children of a node,
## checking if a reference is safe to use, and various math/tile utilities.
##
## All methods of this class are static, so it should never be instantiated. This allows you to use
## its functionality anywhere, without autoloads.


static func centroid(points: Array) -> Vector2:
	var _centroid: Vector2 = Vector2.ZERO

	if points.size() == 0:
		return _centroid

	for _point in points:
		_centroid += _point

	_centroid = _centroid / float(points.size())

	return _centroid


## free() all children of the passed node.
static func free_children(node):
	for n in node.get_children():
		n.free()


## queue_free() all children of the passed node.
static func queue_free_children(node):
	for n in node.get_children():
		n.queue_free()


## Checks if the argument is null, queued for deletion, and a valid instance.
##
## Use this function when you want to do work with a reference that may at some point be freed, or
## may be initialized to null. The argument is typed as Variant to avoid errors when passing null,
## but this function should only be used for types that inherit from Object (which is everything
## that can be freed).
static func reference_safe(object: Variant) -> bool:
	return object != null && !object.is_queued_for_deletion() && is_instance_valid(object)


static func tilemap_global_cell_position(tilemap: TileMapLayer, position: Vector2) -> Vector2:
	return tilemap.to_global(tilemap.map_to_world(tilemap.world_to_map(tilemap.to_local(position))))


## Returns a dictionary where the key is a Resource, and the value is the path it was loaded from.
##
## Recursively loads folders in the path. Useful for data-driven games.
static func load_resources_directory(directory_path: String) -> Dictionary[Resource, String]:
	var directory: DirAccess = DirAccess.open(directory_path)
	var resources: Dictionary[Resource, String] = {}

	if directory:
		directory.list_dir_begin()
		var file_name: String = directory.get_next()

		while file_name != "":
			if !directory.current_is_dir():
				if file_name.ends_with(".tres"):
					var resource_path: String = directory_path + file_name
					var new_resource: Resource = ResourceLoader.load(resource_path)
					
					resources[new_resource] = resource_path
			else:
				var inner_resources: Dictionary[Resource, String] = load_resources_directory(directory_path + file_name + "/")
				
				for inner_resource in inner_resources:
					resources[inner_resource] = inner_resources[inner_resource]

			file_name = directory.get_next()

	return resources


static func get_camera_zoom_contain_sprite(camera: Camera2D, sprite: Sprite2D, padding: float) -> Vector2:
	var bounding_rect: Rect2 = sprite.get_rect() * sprite.global_transform
	
	var viewport_size: Vector2 = camera.get_viewport().size
	var scale_x: float = (viewport_size.x - (padding * 2)) / bounding_rect.size.x
	var scale_y: float = (viewport_size.y - (padding * 2)) / bounding_rect.size.y
	var needed_scale: float = min(scale_x, scale_y)
	
	return Vector2(needed_scale, needed_scale)


static func get_tile_from_global_position(position: Vector2, tilemap: TileMapLayer) -> Vector2i:
	return tilemap.local_to_map(tilemap.to_local(position))


static func get_tile_from_offset_global(
	position: Vector2, offset: Vector2, tilemap: TileMapLayer
) -> Vector2i:
	return get_tile_from_global_position(
		position + (offset * Vector2(tilemap.tile_set.tile_size)), tilemap
	)


static func get_global_position_from_tile(tile: Vector2i, tilemap: TileMapLayer) -> Vector2:
	return tilemap.to_global(tilemap.map_to_local(tile))


static func rotate_towards(node: Node2D, target_angle: float, step: float) -> void:
	var difference: float = angle_difference(node.rotation, target_angle)

	if abs(difference) <= step:
		node.rotate(difference)
	else:
		node.rotate(sign(difference) * step)
