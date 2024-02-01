extends Node


static func centroid(points: Array) -> Vector2:
	var _centroid: Vector2 = Vector2.ZERO

	if points.size() == 0:
		return _centroid

	for _point in points:
		_centroid += _point

	_centroid = _centroid / float(points.size())

	return _centroid


static func free_children(node):
	for n in node.get_children():
		n.free()


static func queue_free_children(node):
	for n in node.get_children():
		n.queue_free()


static func reference_safe(node: Node) -> bool:
	return node != null && !node.is_queued_for_deletion() && is_instance_valid(node)


static func tilemap_global_cell_position(tilemap: TileMap, position: Vector2) -> Vector2:
	return tilemap.to_global(tilemap.map_to_world(tilemap.world_to_map(tilemap.to_local(position))))


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
