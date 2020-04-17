extends Node

static func free_children(node):
  for n in node.get_children():
      n.free()

static func queue_free_children(node):
  for n in node.get_children():
      n.queue_free()
