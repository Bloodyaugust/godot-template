extends Node

static func centroid(points:Array) -> Vector2:
  var centroid:Vector2 = Vector2.ZERO

  if points.size() == 0:
    return centroid

  for _point in points:
    centroid += _point

  centroid = centroid / float(points.size())

  return centroid

static func get_files_in_directory(directory_path: String) -> Array:
  var _directory: Directory = Directory.new()
  var _files: Array = []

  if _directory.open(directory_path) == OK:
    _directory.list_dir_begin(true)
    var _file_name: String = _directory.get_next()

    while _file_name != "":
      if !_directory.current_is_dir():
        _files.append(_file_name)

      _file_name = _directory.get_next()

    _directory.list_dir_end()

  return _files

static func free_children(node):
  for n in node.get_children():
      n.free()

static func queue_free_children(node):
  for n in node.get_children():
      n.queue_free()

static func reference_safe(node:Node) -> bool:
  return node != null && !node.is_queued_for_deletion() && is_instance_valid(node)
