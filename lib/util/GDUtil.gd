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


static func tilemap_global_cell_position(tilemap: TileMap, position: Vector2) -> Vector2:
	return tilemap.to_global(tilemap.map_to_world(tilemap.world_to_map(tilemap.to_local(position))))


## Returns all files in a directory. Useful for data-driven games.
static func load_directory(directory_path: String) -> Array[Variant]:
	var _directory: DirAccess = DirAccess.open(directory_path)
	var _paths: Array[String] = []
	var _loads: Array = []

	if _directory:
		_directory.list_dir_begin()
		var _file_name: String = _directory.get_next()

		while _file_name != "":
			if !_directory.current_is_dir():
				_paths.append(directory_path + _file_name)

			_file_name = _directory.get_next()

		for _path in _paths:
			_loads.append(load(_path))

	return _loads


static func get_tile_from_global_position(position: Vector2, tilemap: TileMap) -> Vector2i:
	return tilemap.local_to_map(tilemap.to_local(position))


static func get_tile_from_offset_global(
	position: Vector2, offset: Vector2, tilemap: TileMap
) -> Vector2i:
	return get_tile_from_global_position(
		position + (offset * Vector2(tilemap.tile_set.tile_size)), tilemap
	)


static func get_global_position_from_tile(tile: Vector2i, tilemap: TileMap) -> Vector2:
	return tilemap.to_global(tilemap.map_to_local(tile))
